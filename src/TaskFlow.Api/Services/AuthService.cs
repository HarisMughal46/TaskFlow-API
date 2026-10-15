using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TaskFlow.Api.Data;
using TaskFlow.Api.DTOs;
using TaskFlow.Api.Models;

namespace TaskFlow.Api.Services;

public class JwtOptions
{
    public string Key { get; set; } = "";
    public string Issuer { get; set; } = "TaskFlow";
    public string Audience { get; set; } = "TaskFlow";
    public int ExpiryMinutes { get; set; } = 60;
}

public interface IAuthService
{
    Task<AuthResponse?> RegisterAsync(RegisterRequest req, CancellationToken ct);
    Task<AuthResponse?> LoginAsync(LoginRequest req, CancellationToken ct);
}

public class AuthService(AppDbContext db, JwtOptions jwt, ILogger<AuthService> logger) : IAuthService
{
    private readonly PasswordHasher<User> _hasher = new();

    public async Task<AuthResponse?> RegisterAsync(RegisterRequest req, CancellationToken ct)
    {
        var email = req.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Email == email, ct)) return null;
        var user = new User { Email = email, DisplayName = req.DisplayName.Trim() };
        user.PasswordHash = _hasher.HashPassword(user, req.Password);
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("User {UserId} registered", user.Id);
        return CreateToken(user);
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest req, CancellationToken ct)
    {
        var email = req.Email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        if (user is null || _hasher.VerifyHashedPassword(user, user.PasswordHash, req.Password) == PasswordVerificationResult.Failed)
        {
            logger.LogWarning("Failed login for {Email}", email);
            return null;
        }
        return CreateToken(user);
    }

    private AuthResponse CreateToken(User user)
    {
        var expires = DateTime.UtcNow.AddMinutes(jwt.ExpiryMinutes);
        var creds = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(jwt.Issuer, jwt.Audience,
            [new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()), new Claim(JwtRegisteredClaimNames.Email, user.Email), new Claim("name", user.DisplayName)],
            expires: expires, signingCredentials: creds);
        return new AuthResponse(new JwtSecurityTokenHandler().WriteToken(token), expires, user.Email, user.DisplayName);
    }
}
