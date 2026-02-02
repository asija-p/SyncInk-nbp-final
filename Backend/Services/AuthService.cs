using Backend.Data;
using Backend.Models.DTOs;
using Backend.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace Backend.Services;

public class AuthService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly JwtService _jwtService;

    public AuthService(AppDbContext db, IConfiguration config, JwtService jwtService)
    {
        _db = db;
        _config = config;
        _jwtService = jwtService;
    }
    private string HashPassword(string password)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }
    public async Task<string> Register(RegisterDto dto)
    {
        if (await _db.Users.AnyAsync(u => u.Username == dto.Username))
            throw new Exception("Email already in use");

        var user = new User
        {
            Username = dto.Username,
            PasswordHash = HashPassword(dto.Password)
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var token = _jwtService.GenerateToken(user.Username, user.Id);
        return token;
    }
    public async Task<string> Login(LoginDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == dto.Username);
        if (user == null || user.PasswordHash != HashPassword(dto.Password))
            throw new Exception("Invalid credentials");

        // Generate JWT token
        var token = _jwtService.GenerateToken(user.Username, user.Id);
        return token;
    }
    public async Task<UserProfile> GetProfile(Guid userId)
    {
        var user = await _db.Users.FindAsync(userId)
                   ?? throw new Exception("User not found");

        return new UserProfile(user.Username);
    }
}