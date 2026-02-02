using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Backend.Models.DTOs;
using Backend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _auth;
    private readonly JwtService _jwtService;

    public AuthController(AuthService auth, JwtService jwtService)
    {
        _auth = auth;
        _jwtService = jwtService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var token = await _auth.Login(dto);
        var cookieOptions = new CookieOptions
        {
            HttpOnly = false,
            Secure = false,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddHours(1)
        };
        Response.Cookies.Append("auth_token", token, cookieOptions);
        return Ok(new { message = "Logged in successfully: " + token });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        var token = await _auth.Register(dto);

        var cookieOptions = new CookieOptions
        {
            HttpOnly = false,
            Secure = false,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddHours(1)
        };
        Response.Cookies.Append("auth_token", token, cookieOptions);
        return Ok(new { message = "Registration in successfully" + token });
    }

    [HttpGet("check/me")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<IActionResult> GetProfile()
    {
        var jwtCookie = Request.Cookies["auth_token"];
        if (string.IsNullOrEmpty(jwtCookie))
        {
            return Unauthorized("Cookie not found");
        }
        var handler = new JwtSecurityTokenHandler();
        var jsonToken = handler.ReadToken(jwtCookie) as JwtSecurityToken;
        var sub = jsonToken?.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
        if (string.IsNullOrEmpty(sub))
        {
            return BadRequest("Sub claim not found in token");
        }
        var profile = await _auth.GetProfile(Guid.Parse(sub));
        return Ok(profile);
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Append("auth_token", "", new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddDays(-1),
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Path = "/"
        });

        Console.WriteLine("LOGOUT USER");
        return Ok();
    }
}