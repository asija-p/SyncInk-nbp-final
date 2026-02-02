using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Backend.Models.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Backend.Services;

public class JwtService
{
    //IConfiguration is used to access the appsetttings.json keys and values.
    private readonly IConfiguration _config;

    public JwtService(IConfiguration config)
    {
        //Copy for in class use.
        _config = config;
    }

    public string GenerateToken(string username, Guid id)
    {
        var jwtKey = _config["Jwt:Key"]
            ?? throw new Exception("JWT Key is missing");

        var issuer = _config["Jwt:Issuer"]
            ?? throw new Exception("JWT Issuer is missing");

        var audience = _config["Jwt:Audience"]
            ?? throw new Exception("JWT Audience is missing");

        var expireMinutesStr = _config["Jwt:ExpireMinutes"]
            ?? throw new Exception("JWT ExpireMinutes is missing");

        var expireMinutes = double.Parse(expireMinutesStr);

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtKey)
        );

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
        new Claim(JwtRegisteredClaimNames.Sub, id.ToString()),
        new Claim(JwtRegisteredClaimNames.UniqueName, username),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expireMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

}