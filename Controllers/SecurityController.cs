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
}
