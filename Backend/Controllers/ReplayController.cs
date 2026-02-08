using System.IdentityModel.Tokens.Jwt;
using Backend.Cassandra;
using Backend.Data;
using Backend.Models.Entities;
using Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
[Route("replay")]
public class ReplayController : ControllerBase
{
    private readonly ReplayService _replayService;
    private readonly CassandraService _cassandraService;

    public ReplayController(ReplayService replayService, CassandraService cassandraService)
    {
        _replayService = replayService;
        _cassandraService = cassandraService;
    }


    [HttpGet("list")]
    public async Task<IActionResult> GetUserReplays()
    {
        try
        {
            var userId = GetUserIdFromToken();
            if (userId == Guid.Empty)
                return Unauthorized("Invalid Token");

            var snapshots = await _cassandraService.GetUserSnapshotsAsync(userId);
            return Ok(snapshots);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }


    [HttpPost("start")]
    public async Task<IActionResult> StartReplay([FromBody] ReplayRequestDto request)
    {
        Guid sub = GetUserIdFromToken();
        try
        {
            var strokeCount = await _replayService.StartReplayAsync(
                sub,
                request.RoomName,
                request.SaveId
            );
            return Ok(new { message = "Replay started successfully", strokeCount });

        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private Guid GetUserIdFromToken()
    {
        var jwtCookie = Request.Cookies["auth_token"];
        if (string.IsNullOrEmpty(jwtCookie)) return Guid.Empty;

        var handler = new JwtSecurityTokenHandler();
        var jsonToken = handler.ReadToken(jwtCookie) as JwtSecurityToken;
        var sub = jsonToken?.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;

        return Guid.TryParse(sub, out var guid) ? guid : Guid.Empty;
    }
}


public class ReplayRequestDto
{
    public string RoomName { get; set; }
    public Guid SaveId { get; set; }
}