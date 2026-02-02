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

        public CassandraController(
            CassandraService cassandra,
            RedisService redis,
            AppDbContext db
        )
        {
            _cassandra = cassandra;
            _redis = redis;
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

    }
}
