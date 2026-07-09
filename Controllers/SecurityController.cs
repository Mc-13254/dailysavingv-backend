using DailySavingV.API.Data;
using DailySavingV.API.DTOs;
using DailySavingV.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DailySavingV.API.Controllers;

[ApiController]
[Route("api/security")]
[Authorize]
public class SecurityController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public SecurityController(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    // ---- Failed Login Attempts --------------------------------------------

    [HttpGet("failed-logins")]
    [Authorize(Policy = "SupervisorOrAdmin")]
    public async Task<ActionResult<IEnumerable<FailedLoginRowDto>>> FailedLogins(
        [FromQuery] string? username, [FromQuery] string? riskLevel, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var query = _db.FailedLoginAttempts.AsQueryable();
        if (!string.IsNullOrWhiteSpace(username)) query = query.Where(a => a.Username.Contains(username));
        if (!string.IsNullOrWhiteSpace(riskLevel)) query = query.Where(a => a.RiskLevel == riskLevel);
        if (from.HasValue) query = query.Where(a => a.AttemptDate >= from.Value);
        if (to.HasValue) query = query.Where(a => a.AttemptDate <= to.Value.AddDays(1).AddTicks(-1));

        var attempts = await query.OrderByDescending(a => a.AttemptDate).Take(300).ToListAsync();
        var codeUsers = attempts.Where(a => a.CodeUser != null).Select(a => a.CodeUser!).Distinct().ToList();
        var users = await _db.Users.IgnoreQueryFilters().Where(u => codeUsers.Contains(u.CodeUser)).ToListAsync();

        var result = attempts.Select(a =>
        {
            var user = users.FirstOrDefault(u => u.CodeUser == a.CodeUser);
            return new FailedLoginRowDto(
                a.AttemptID, a.Username, a.CodeUser, user != null ? $"{user.FirstName} {user.LastName}".Trim() : null,
                a.FailureReason, a.RiskLevel, a.IPAddress, a.UserAgent, a.AttemptDate
            );
        });

        return Ok(result);
    }

    [HttpGet("failed-logins/stats")]
    [Authorize(Policy = "SupervisorOrAdmin")]
    public async Task<ActionResult<FailedLoginStatsDto>> FailedLoginStats()
    {
        var today = DateTime.UtcNow.Date;
        var weekStart = today.AddDays(-(int)today.DayOfWeek);
        var monthStart = new DateTime(today.Year, today.Month, 1);

        var all = await _db.FailedLoginAttempts.ToListAsync();
        var lockedAccounts = await _db.Users.IgnoreQueryFilters().CountAsync(u => u.AccountLocked);

        return Ok(new FailedLoginStatsDto(
            all.Count(a => a.AttemptDate >= today),
            all.Count(a => a.AttemptDate >= weekStart),
            all.Count(a => a.AttemptDate >= monthStart),
            lockedAccounts,
            all.Count(a => a.RiskLevel is "HIGH" or "CRITICAL")
        ));
    }

    // ---- Account Lockout ---------------------------------------------------

    [HttpGet("locked-accounts")]
    [Authorize(Policy = "SupervisorOrAdmin")]
    public async Task<ActionResult<IEnumerable<LockedAccountRowDto>>> LockedAccounts()
    {
        var users = await _db.Users.IgnoreQueryFilters()
            .Include(u => u.Role).Include(u => u.Agence)
            .Where(u => u.AccountLocked)
            .ToListAsync();

        return Ok(users.Select(u => new LockedAccountRowDto(
            u.CodeUser, u.Username, $"{u.FirstName} {u.LastName}".Trim(), u.Role?.Code, u.Agence?.Nom,
            u.FailedLoginAttempts, u.LockReason, u.LockedDate, u.LockedBy
        )));
    }

    [HttpPost("lock-account/{codeUser}")]
    [Authorize(Policy = "SupervisorOrAdmin")]
    public async Task<ActionResult> LockAccount(string codeUser, LockAccountRequest request)
    {
        var user = await _db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.CodeUser == codeUser)
            ?? throw new KeyNotFoundException("Utilisateur introuvable.");

        user.AccountLocked = true;
        user.LockReason = request.Reason;
        user.LockedDate = DateTime.UtcNow;
        user.LockedBy = _currentUser.CodeUser;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Compte verrouillé." });
    }

    [HttpPost("unlock-account/{codeUser}")]
    [Authorize(Policy = "SupervisorOrAdmin")]
    public async Task<ActionResult> UnlockAccount(string codeUser)
    {
        var user = await _db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.CodeUser == codeUser)
            ?? throw new KeyNotFoundException("Utilisateur introuvable.");

        user.AccountLocked = false;
        user.FailedLoginAttempts = 0;
        user.LockReason = null;
        user.LockedDate = null;
        user.LockedBy = null;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Compte déverrouillé." });
    }

    // ---- Active Sessions ----------------------------------------------------

    [HttpGet("sessions")]
    [Authorize(Policy = "SupervisorOrAdmin")]
    public async Task<ActionResult<IEnumerable<ActiveSessionRowDto>>> ActiveSessions()
    {
        var sessions = await _db.RefreshTokens
            .Where(t => t.IsActive && t.ExpiryDate > DateTime.UtcNow)
            .OrderByDescending(t => t.CreatedDate)
            .ToListAsync();

        var codeUsers = sessions.Select(s => s.CodeUser).Distinct().ToList();
        var users = await _db.Users.IgnoreQueryFilters().Include(u => u.Role).Include(u => u.Agence)
            .Where(u => codeUsers.Contains(u.CodeUser)).ToListAsync();

        var result = sessions.Select(s =>
        {
            var user = users.FirstOrDefault(u => u.CodeUser == s.CodeUser);
            return new ActiveSessionRowDto(
                s.TokenID, s.CodeUser, user?.Username ?? s.CodeUser, user != null ? $"{user.FirstName} {user.LastName}".Trim() : null,
                user?.Role?.Code, user?.Agence?.Nom, s.IPAddress, s.UserAgent, s.CreatedDate, s.ExpiryDate, s.IsActive
            );
        });

        return Ok(result);
    }

    [HttpGet("sessions/stats")]
    [Authorize(Policy = "SupervisorOrAdmin")]
    public async Task<ActionResult<SessionStatsDto>> SessionStats()
    {
        var sessions = await _db.RefreshTokens.Where(t => t.IsActive && t.ExpiryDate > DateTime.UtcNow).ToListAsync();
        var codeUsers = sessions.Select(s => s.CodeUser).Distinct().ToList();
        var users = await _db.Users.IgnoreQueryFilters().Include(u => u.Role).Where(u => codeUsers.Contains(u.CodeUser)).ToListAsync();

        int CountByRole(string keyword) => sessions.Count(s =>
        {
            var role = users.FirstOrDefault(u => u.CodeUser == s.CodeUser)?.Role?.Code ?? "";
            return role.Contains(keyword, StringComparison.OrdinalIgnoreCase);
        });

        return Ok(new SessionStatsDto(
            sessions.Count, CountByRole("COLLECTOR"), CountByRole("CASHIER"), CountByRole("MANAGER"), CountByRole("ADMIN")
        ));
    }

    [HttpPost("sessions/{tokenId:int}/terminate")]
    [Authorize(Policy = "SupervisorOrAdmin")]
    public async Task<ActionResult> TerminateSession(int tokenId, TerminateSessionRequest request)
    {
        var session = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenID == tokenId)
            ?? throw new KeyNotFoundException("Session introuvable.");

        session.IsActive = false;
        session.RevokedDate = DateTime.UtcNow;
        session.TerminationReason = request.Reason;
        session.TerminatedBy = _currentUser.CodeUser;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Session terminée." });
    }

    // ---- Password Policy ---------------------------------------------------

    private async Task<Entities.PasswordPolicy> GetOrCreatePolicyAsync()
    {
        var policy = await _db.PasswordPolicies.FirstOrDefaultAsync();
        if (policy == null)
        {
            policy = new Entities.PasswordPolicy();
            _db.PasswordPolicies.Add(policy);
            await _db.SaveChangesAsync();
        }
        return policy;
    }

    [HttpGet("password-policy")]
    public async Task<ActionResult<PasswordPolicyDto>> GetPasswordPolicy()
    {
        var p = await GetOrCreatePolicyAsync();
        return Ok(new PasswordPolicyDto(
            p.MinimumLength, p.MaximumLength, p.RequireUppercase, p.RequireLowercase,
            p.RequireNumber, p.RequireSpecialCharacter, p.PasswordExpirationDays, p.PasswordHistoryCount
        ));
    }

    [HttpPut("password-policy")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult> SavePasswordPolicy(PasswordPolicyDto request)
    {
        var p = await GetOrCreatePolicyAsync();
        p.MinimumLength = request.MinimumLength;
        p.MaximumLength = request.MaximumLength;
        p.RequireUppercase = request.RequireUppercase;
        p.RequireLowercase = request.RequireLowercase;
        p.RequireNumber = request.RequireNumber;
        p.RequireSpecialCharacter = request.RequireSpecialCharacter;
        p.PasswordExpirationDays = request.PasswordExpirationDays;
        p.PasswordHistoryCount = request.PasswordHistoryCount;
        p.UpdatedBy = _currentUser.CodeUser;
        p.UpdatedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Politique de mot de passe mise à jour." });
    }

    [HttpPost("change-password")]
    public async Task<ActionResult> ChangePassword(ChangePasswordRequest request)
    {
        var user = await _db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.CodeUser == _currentUser.CodeUser)
            ?? throw new KeyNotFoundException("Utilisateur introuvable.");

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            throw new InvalidOperationException("Mot de passe actuel incorrect.");
        if (request.NewPassword != request.ConfirmPassword)
            throw new InvalidOperationException("La confirmation ne correspond pas au nouveau mot de passe.");
        if (BCrypt.Net.BCrypt.Verify(request.NewPassword, user.PasswordHash))
            throw new InvalidOperationException("Le nouveau mot de passe doit être différent de l'actuel.");

        var policy = await GetOrCreatePolicyAsync();
        var errors = Services.PasswordPolicyValidator.Validate(request.NewPassword, policy, user.Username);
        if (errors.Count > 0) throw new InvalidOperationException(string.Join(" ", errors));

        // Reject reuse of the last N passwords per policy.
        var recentHashes = await _db.PasswordHistories
            .Where(h => h.CodeUser == user.CodeUser)
            .OrderByDescending(h => h.ChangedDate)
            .Take(policy.PasswordHistoryCount)
            .ToListAsync();
        if (recentHashes.Any(h => BCrypt.Net.BCrypt.Verify(request.NewPassword, h.PasswordHash)))
            throw new InvalidOperationException($"Ce mot de passe a déjà été utilisé récemment (les {policy.PasswordHistoryCount} derniers sont bloqués).");

        _db.PasswordHistories.Add(new Entities.PasswordHistory { CodeUser = user.CodeUser, PasswordHash = user.PasswordHash, ChangedBy = user.CodeUser });
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.MustChangePassword = false;
        user.PasswordChangedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Mot de passe modifié avec succès." });
    }

    [HttpPost("password-strength")]
    public ActionResult<int> PasswordStrength([FromBody] string password) => Ok(Services.PasswordPolicyValidator.Score(password ?? ""));

    // ---- API Management (key issuance/revocation) --------------------------

    [HttpGet("api-keys")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<IEnumerable<ApiKeyRowDto>>> ApiKeys()
    {
        var keys = await _db.ApiKeys.OrderByDescending(k => k.CreatedDate).ToListAsync();
        return Ok(keys.Select(k => new ApiKeyRowDto(k.ApiKeyID, k.Name, k.KeyPrefix, k.Description, k.ExpiryDate, k.IsActive, k.LastUsedDate, k.CreatedBy, k.CreatedDate)));
    }

    [HttpPost("api-keys")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<CreateApiKeyResponseDto>> CreateApiKey(CreateApiKeyRequest request)
    {
        var rawKey = $"ac_{Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32))}".Replace("+", "").Replace("/", "").Replace("=", "");
        var entity = new Entities.ApiKey
        {
            Name = request.Name,
            Description = request.Description,
            ExpiryDate = request.ExpiryDate,
            KeyHash = BCrypt.Net.BCrypt.HashPassword(rawKey),
            KeyPrefix = rawKey[..Math.Min(10, rawKey.Length)] + "…",
            CreatedBy = _currentUser.CodeUser!
        };
        _db.ApiKeys.Add(entity);
        await _db.SaveChangesAsync();

        // The raw key is only ever shown here, once. Only the hash is kept afterwards.
        return Ok(new CreateApiKeyResponseDto(entity.ApiKeyID, rawKey));
    }

    [HttpPost("api-keys/{id:int}/revoke")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult> RevokeApiKey(int id)
    {
        var key = await _db.ApiKeys.FirstOrDefaultAsync(k => k.ApiKeyID == id)
            ?? throw new KeyNotFoundException("Clé API introuvable.");
        key.IsActive = false;
        key.RevokedBy = _currentUser.CodeUser;
        key.RevokedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Clé API révoquée." });
    }

    // ---- Error Logs ---------------------------------------------------------

    [HttpGet("error-logs")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<IEnumerable<ErrorLogRowDto>>> ErrorLogs([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var query = _db.ErrorLogs.AsQueryable();
        if (from.HasValue) query = query.Where(e => e.OccurredDate >= from.Value);
        if (to.HasValue) query = query.Where(e => e.OccurredDate <= to.Value.AddDays(1).AddTicks(-1));

        var logs = await query.OrderByDescending(e => e.OccurredDate).Take(300)
            .Select(e => new ErrorLogRowDto(e.ErrorLogID, e.Message, e.ExceptionType, e.RequestPath, e.RequestMethod, e.CodeUser, e.OccurredDate))
            .ToListAsync();

        return Ok(logs);
    }

    // ---- System Health --------------------------------------------------

    private static readonly DateTime ProcessStart = DateTime.UtcNow;

    [HttpGet("system-health")]
    public async Task<ActionResult<SystemHealthDto>> SystemHealth()
    {
        var dbOk = await _db.Database.CanConnectAsync();
        var since24h = DateTime.UtcNow.AddHours(-24);

        return Ok(new SystemHealthDto(
            "OK", dbOk ? "OK" : "UNREACHABLE", DateTime.UtcNow, DateTime.UtcNow - ProcessStart,
            await _db.ErrorLogs.CountAsync(e => e.OccurredDate >= since24h),
            await _db.RefreshTokens.CountAsync(t => t.IsActive && t.ExpiryDate > DateTime.UtcNow)
        ));
    }
}
