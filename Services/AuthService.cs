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

    // Accounts lock automatically after this many consecutive failed attempts.
    // Simplification: no rolling time-window (e.g. "5 in 10 minutes") — a
    // straight consecutive-attempt counter that resets on success. Good enough
    // to stop brute-force guessing without needing a background job.
    private const int MaxFailedAttempts = 5;

    public async Task<LoginResponse?> LoginAsync(string username, string password, string? ipAddress, string? userAgent)
    {
        // NOTE: Users DbSet has a global query filter based on the CURRENT user's
        // agency. At login time there is no current user yet, so we query
        // IgnoreQueryFilters() here deliberately - login must be able to find
        // any active user regardless of agency.
        var user = await _db.Users
            .IgnoreQueryFilters()
            .Include(u => u.Role)
            .Include(u => u.Agence)
            .FirstOrDefaultAsync(u => u.Username == username);

        async Task RecordFailureAsync(string reason, string? codeUser)
        {
            var recentCount = await _db.FailedLoginAttempts
                .CountAsync(a => a.Username == username && a.AttemptDate >= DateTime.UtcNow.AddMinutes(-15));
            var riskLevel = recentCount >= 5 ? "CRITICAL" : recentCount >= 3 ? "HIGH" : recentCount >= 1 ? "MEDIUM" : "LOW";

            _db.FailedLoginAttempts.Add(new FailedLoginAttempt
            {
                Username = username,
                CodeUser = codeUser,
                FailureReason = reason,
                RiskLevel = riskLevel,
                IPAddress = ipAddress,
                UserAgent = userAgent
            });
        }

        if (user == null)
        {
            await RecordFailureAsync("UNKNOWN_USERNAME", null);
            await _db.SaveChangesAsync();
            return null;
        }

        if (user.AccountLocked)
        {
            await RecordFailureAsync("LOCKED_ACCOUNT", user.CodeUser);
            await _db.SaveChangesAsync();
            throw new InvalidOperationException($"Compte verrouillé{(user.LockReason != null ? $" : {user.LockReason}" : "")}. Contactez un administrateur.");
        }

        if (user.Statut != "ACTIVE")
        {
            await RecordFailureAsync("INACTIVE_ACCOUNT", user.CodeUser);
            await _db.SaveChangesAsync();
            return null;
        }

        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            await RecordFailureAsync("WRONG_PASSWORD", user.CodeUser);
            user.FailedLoginAttempts += 1;
            if (user.FailedLoginAttempts >= MaxFailedAttempts)
            {
                user.AccountLocked = true;
                user.LockReason = "Trop de tentatives de connexion échouées";
                user.LockedDate = DateTime.UtcNow;
                user.LockedBy = "SYSTEM";
            }
            await _db.SaveChangesAsync();
            return null;
        }

        if (user.ValidationStatus != "VALIDATED") return null; // account itself pending Maker-Checker approval

        user.LastLogin = DateTime.UtcNow;
        user.FailedLoginAttempts = 0; // reset the streak on a successful login

        var (accessToken, expiresAt) = GenerateAccessToken(user);
        var refreshToken = GenerateRefreshToken();

        _db.RefreshTokens.Add(new RefreshToken
        {
            CodeUser = user.CodeUser,
            Token = refreshToken,
            ExpiryDate = DateTime.UtcNow.AddDays(_config.GetValue<int>("Jwt:RefreshTokenDays", 7)),
            IsActive = true,
            IPAddress = ipAddress,
            UserAgent = userAgent
        });

        _db.Activites.Add(new Activite
        {
            CodeUser = user.CodeUser,
            Action = "LOGIN",
            Module = "AUTH",
            Description = $"User {user.Username} logged in",
            AdresseIP = ipAddress
        });

        await _db.SaveChangesAsync();

        return new LoginResponse(
            user.CodeUser, user.Username, user.Role!.Code, user.AgenceID, user.Agence?.Nom, user.Agence?.CodeAgence,
            accessToken, refreshToken, expiresAt, user.MustChangePassword
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
            user.CodeUser, user.Username, user.Role!.Code, user.AgenceID, user.Agence?.Nom, user.Agence?.CodeAgence,
            accessToken, newRefreshToken, expiresAt, user.MustChangePassword
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
