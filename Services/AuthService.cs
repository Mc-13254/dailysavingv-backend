using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using DailySavingV.API.Data;
using DailySavingV.API.DTOs;
using DailySavingV.API.Entities;
using DailySavingV.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace DailySavingV.API.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public AuthService(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<LoginResponse?> LoginAsync(string username, string password)
    {
        // NOTE: Users DbSet has a global query filter based on the CURRENT user's
        // agency. At login time there is no current user yet, so we query
        // IgnoreQueryFilters() here deliberately - login must be able to find
        // any active user regardless of agency.
        var user = await _db.Users
            .IgnoreQueryFilters()
            .Include(u => u.Role)
            .Include(u => u.Agence)
            .FirstOrDefaultAsync(u => u.Username == username && u.Statut == "ACTIVE");

        if (user == null) return null;
        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash)) return null;
        if (user.ValidationStatus != "VALIDATED") return null; // account itself pending Maker-Checker approval

        user.LastLogin = DateTime.UtcNow;

        var (accessToken, expiresAt) = GenerateAccessToken(user);
        var refreshToken = GenerateRefreshToken();

        _db.RefreshTokens.Add(new RefreshToken
        {
            CodeUser = user.CodeUser,
            Token = refreshToken,
            ExpiryDate = DateTime.UtcNow.AddDays(_config.GetValue<int>("Jwt:RefreshTokenDays", 7)),
            IsActive = true
        });

        _db.Activites.Add(new Activite
        {
            CodeUser = user.CodeUser,
            Action = "LOGIN",
            Module = "AUTH",
            Description = $"User {user.Username} logged in"
        });

        await _db.SaveChangesAsync();

        return new LoginResponse(
            user.CodeUser, user.Username, user.Role!.Code, user.AgenceID, user.Agence?.Nom,
            accessToken, refreshToken, expiresAt
        );
    }

    public async Task<LoginResponse?> RefreshAsync(string refreshToken)
    {
        var stored = await _db.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == refreshToken && t.IsActive && t.ExpiryDate > DateTime.UtcNow);

        if (stored == null) return null;

        var user = await _db.Users
            .IgnoreQueryFilters()
            .Include(u => u.Role)
            .Include(u => u.Agence)
            .FirstOrDefaultAsync(u => u.CodeUser == stored.CodeUser);

        if (user == null) return null;

        // Rotate: revoke old, issue new
        stored.IsActive = false;
        stored.RevokedDate = DateTime.UtcNow;

        var (accessToken, expiresAt) = GenerateAccessToken(user);
        var newRefreshToken = GenerateRefreshToken();

        _db.RefreshTokens.Add(new RefreshToken
        {
            CodeUser = user.CodeUser,
            Token = newRefreshToken,
            ExpiryDate = DateTime.UtcNow.AddDays(_config.GetValue<int>("Jwt:RefreshTokenDays", 7)),
            IsActive = true
        });

        await _db.SaveChangesAsync();

        return new LoginResponse(
            user.CodeUser, user.Username, user.Role!.Code, user.AgenceID, user.Agence?.Nom,
            accessToken, newRefreshToken, expiresAt
        );
    }

    public async Task LogoutAsync(string refreshToken)
    {
        var stored = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.Token == refreshToken);
        if (stored != null)
        {
            stored.IsActive = false;
            stored.RevokedDate = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }

    private (string token, DateTime expiresAt) GenerateAccessToken(Users user)
    {
        var minutes = _config.GetValue<int>("Jwt:AccessTokenMinutes", 30);
        var expiresAt = DateTime.UtcNow.AddMinutes(minutes);

        var claims = new List<Claim>
        {
            new("codeUser", user.CodeUser),
            new("username", user.Username),
            new("role", user.Role!.Code),
        };

        // Only add the agenceId claim when the user actually belongs to one.
        // Its ABSENCE + role != ADMIN is what the DbContext filter treats as "no access".
        if (user.AgenceID.HasValue)
            claims.Add(new Claim("agenceId", user.AgenceID.Value.ToString()));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: expiresAt,
            signingCredentials: creds
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    private static string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }
}
