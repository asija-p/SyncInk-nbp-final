using Backend.Cassandra;
using Backend.Redis;
using Backend.Models.Entities;
using Backend.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace Backend.Controllers
{
    [ApiController]
    [Route("cassandra")]
    public class CassandraController : ControllerBase
    {
        private readonly CassandraService _cassandra;
        private readonly RedisService _redis;

        private readonly AppDbContext _db;

        public CassandraController(
            CassandraService cassandra,
            RedisService redis,
            AppDbContext db
        )
        {
            _cassandra = cassandra;
            _redis = redis;
            _db = db;
        }

        [HttpPost("save")]
        public async Task<IActionResult> SaveCurrentRoomAsync([FromQuery] string roomName)
        {
            var jwtCookie = Request.Cookies["auth_token"];
            if (string.IsNullOrEmpty(jwtCookie))
                return Unauthorized("Cookie not found");

            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadToken(jwtCookie) as JwtSecurityToken;
            var sub = jsonToken?.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;

            if (string.IsNullOrEmpty(sub))
                return BadRequest("Sub claim not found in token");

            var userId = Guid.Parse(sub);

            var redisStrokes = await _redis.GetAllStrokes(roomName);

            if (redisStrokes == null || redisStrokes.Count == 0)
                return BadRequest("No strokes found for this room.");

            var saveId = Guid.NewGuid();
            var savedAt = DateTime.UtcNow;

            await _cassandra.SaveSnapshotMetadataAsync(
                userId,
                roomName,
                saveId,
                savedAt
            );

            await _cassandra.SaveStrokesSnapshotAsync(
                userId,
                roomName,
                saveId,
                redisStrokes
            );

            return Ok(new
            {
                message = "Strokes saved to Cassandra successfully.",
                saveId,
                savedAt
            });
        }


        [HttpGet("replay-timeline")]
        public async Task<IActionResult> GetReplayTimeline([FromQuery] string roomName)
        {
            if (string.IsNullOrEmpty(roomName))
                return BadRequest("Room name is required.");

            var counters = await _cassandra.GetActivityCountersAsync(roomName);

            var activeStates = await _cassandra.GetActiveUsersAsync(roomName);

            var allUserIds = activeStates.SelectMany(a => a.ActiveUsers).Distinct().ToList();
            var usernamesMap = await GetUsernamesFromPostgresAsync(allUserIds); // returns Dictionary<Guid, string>

            var timeline = counters.Select(c =>
            {
                var activeGuids = activeStates.FirstOrDefault(a => a.MinuteBucket == c.MinuteBucket)?.ActiveUsers ?? new List<Guid>();
                var activeUsernames = activeGuids
                    .Where(id => usernamesMap.ContainsKey(id))
                    .Select(id => usernamesMap[id])
                    .ToList();

                return new
                {
                    minuteBucket = c.MinuteBucket,
                    strokesCompleted = c.StrokesCompleted,
                    undos = c.Undos,
                    redos = c.Redos,
                    activeUsers = activeUsernames
                };
            }).OrderBy(t => t.minuteBucket).ToList();

            return Ok(timeline);
        }

        private async Task<Dictionary<Guid, string>> GetUsernamesFromPostgresAsync(List<Guid> userIds)
        {
            // Assume you have some DbContext injected
            var users = await _db.Users
                .Where(u => userIds.Contains(u.Id))
                .Select(u => new { u.Id, u.Username })
                .ToListAsync();

            return users.ToDictionary(u => u.Id, u => u.Username);
        }



    }
}
