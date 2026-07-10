using DailySavingV.API.Data;
using DailySavingV.API.DTOs;
using DailySavingV.API.Entities;
using DailySavingV.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DailySavingV.API.Controllers;

[ApiController]
[Route("api/cashsession")]
[Authorize]
public class CashSessionController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    // Variance beyond this threshold requires supervisor approval before the
    // session is considered fully closed. Kept as a constant for now — could
    // become an admin-configurable System Parameter later.
    private const decimal VarianceApprovalThreshold = 5000m;

    public CashSessionController(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    // ---- Business Calendar --------------------------------------------

    [HttpGet("business-calendar")]
    public async Task<ActionResult<BusinessCalendarDto>> GetCalendar([FromQuery] int? agenceId)
    {
        var targetAgency = agenceId ?? _currentUser.AgenceID;
        var cal = await _db.BusinessCalendars.FirstOrDefaultAsync(c => c.AgenceID == targetAgency);
        if (cal == null)
        {
            // Sensible default so the system works even before an admin configures it.
            return Ok(new BusinessCalendarDto(targetAgency ?? 0, "1,2,3,4,5,6", new TimeSpan(8, 0, 0), new TimeSpan(17, 0, 0), 15));
        }
        return Ok(new BusinessCalendarDto(cal.AgenceID, cal.WorkingDays, cal.OpeningTime, cal.ClosingTime, cal.GracePeriodMinutes));
    }

    [HttpPut("business-calendar")]
    [Authorize(Policy = "SupervisorOrAdmin")]
    public async Task<ActionResult> SaveCalendar([FromQuery] int agenceId, UpdateBusinessCalendarRequest request)
    {
        var cal = await _db.BusinessCalendars.FirstOrDefaultAsync(c => c.AgenceID == agenceId);
        if (cal == null)
        {
            cal = new BusinessCalendar { AgenceID = agenceId };
            _db.BusinessCalendars.Add(cal);
        }
        cal.WorkingDays = request.WorkingDays;
        cal.OpeningTime = request.OpeningTime;
        cal.ClosingTime = request.ClosingTime;
        cal.GracePeriodMinutes = request.GracePeriodMinutes;
        cal.UpdatedBy = _currentUser.CodeUser;
        cal.UpdatedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Calendrier métier mis à jour." });
    }

    // ---- Current session -----------------------------------------------

    [HttpGet("current")]
    public async Task<ActionResult<CashSessionDto?>> GetCurrent()
    {
        var session = await _db.CashSessions
            .FirstOrDefaultAsync(s => s.CodeUser == _currentUser.CodeUser && s.Status == "OPEN");
        if (session == null) return Ok(null);
        return Ok(await ToDto(session));
    }

    [HttpGet("history")]
    public async Task<ActionResult<IEnumerable<CashSessionDto>>> GetHistory(
        [FromQuery] string? codeUser, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var query = _db.CashSessions.AsQueryable();
        if (!_currentUser.IsHeadOffice) query = query.Where(s => s.AgenceID == _currentUser.AgenceID);
        if (!string.IsNullOrWhiteSpace(codeUser)) query = query.Where(s => s.CodeUser == codeUser);
        if (from.HasValue) query = query.Where(s => s.OpeningDate >= from.Value);
        if (to.HasValue) query = query.Where(s => s.OpeningDate <= to.Value);

        var sessions = await query.OrderByDescending(s => s.OpeningDate).Take(200).ToListAsync();
        var result = new List<CashSessionDto>();
        foreach (var s in sessions) result.Add(await ToDto(s));
        return Ok(result);
    }

    // ---- Open / Close ----------------------------------------------------

    [HttpPost("open")]
    public async Task<ActionResult<CashSessionDto>> Open(OpenSessionRequest request)
    {
        var codeUser = _currentUser.CodeUser!;
        var agenceId = _currentUser.AgenceID ?? 0;

        if (await _db.CashSessions.AnyAsync(s => s.CodeUser == codeUser && s.Status == "OPEN"))
            return BadRequest(new { message = "Vous avez déjà une session de caisse ouverte." });

        // Business rule: previous day's session must be closed first.
        var lastSession = await _db.CashSessions
            .Where(s => s.CodeUser == codeUser)
            .OrderByDescending(s => s.OpeningDate)
            .FirstOrDefaultAsync();
        if (lastSession != null && lastSession.Status == "OPEN")
            return BadRequest(new { message = "Vous ne pouvez pas ouvrir une nouvelle session tant que la précédente n'est pas clôturée." });

        // Business-hours check — TEMPORARILY DISABLED at the user's request so
        // sessions can be opened at any time during testing. Re-enable by
        // uncommenting the block below once the Business Calendar is configured
        // and testing is complete.
        var calendar = await _db.BusinessCalendars.FirstOrDefaultAsync(c => c.AgenceID == agenceId);
        // var workingDays = (calendar?.WorkingDays ?? "1,2,3,4,5,6").Split(',').Select(int.Parse).ToHashSet();
        // var opening = calendar?.OpeningTime ?? new TimeSpan(8, 0, 0);
        // var closing = calendar?.ClosingTime ?? new TimeSpan(17, 0, 0);
        // var grace = calendar?.GracePeriodMinutes ?? 15;
        // var now = DateTime.Now;
        // var isoDay = (int)now.DayOfWeek == 0 ? 7 : (int)now.DayOfWeek;
        // if (!workingDays.Contains(isoDay) || now.TimeOfDay < opening || now.TimeOfDay > closing.Add(TimeSpan.FromMinutes(grace)))
        //     return BadRequest(new { message = "Vous ne pouvez pas ouvrir une session de caisse en dehors des heures ouvrables officielles." });

        var previousClosingCash = lastSession?.PhysicalCash ?? 0m;
        var count = await _db.CashSessions.CountAsync();

        var session = new CashSession
        {
            SessionNumber = $"CS-{DateTime.UtcNow:yyyyMMdd}-{codeUser}-{(count + 1):D4}",
            CodeUser = codeUser,
            AgenceID = agenceId,
            OpeningDate = DateTime.UtcNow,
            OpeningCash = request.OpeningCashOverride ?? previousClosingCash,
            PreviousClosingCash = previousClosingCash,
            OpeningComment = request.Comment,
            Status = "OPEN",
            CreatedBy = codeUser
        };

        _db.CashSessions.Add(session);
        await _db.SaveChangesAsync();

        return Ok(await ToDto(session));
    }

    [HttpPost("close")]
    public async Task<ActionResult<CashSessionDto>> Close(CloseSessionRequest request)
    {
        var session = await _db.CashSessions
            .FirstOrDefaultAsync(s => s.CodeUser == _currentUser.CodeUser && s.Status == "OPEN")
            ?? throw new InvalidOperationException("Aucune session de caisse ouverte à clôturer.");

        // Real banking end-of-day counting: if a bill/coin breakdown was provided,
        // it must add up to exactly the declared physical cash — no silent mismatch.
        if (request.PhysicalCashBreakdown is { Count: > 0 })
        {
            var breakdownTotal = request.PhysicalCashBreakdown.Sum(kv => (decimal)kv.Key * kv.Value);
            if (breakdownTotal != request.PhysicalCash)
                throw new InvalidOperationException(
                    $"Le détail des coupures ({breakdownTotal:N0}) ne correspond pas au montant physique déclaré ({request.PhysicalCash:N0}).");
        }

        var expectedCash = await ComputeExpectedCash(session);
        var difference = request.PhysicalCash - expectedCash;

        session.ClosingDate = DateTime.UtcNow;
        session.ExpectedCash = expectedCash;
        session.PhysicalCash = request.PhysicalCash;
        session.PhysicalCashBreakdownJson = request.PhysicalCashBreakdown is { Count: > 0 }
            ? System.Text.Json.JsonSerializer.Serialize(request.PhysicalCashBreakdown)
            : null;
        session.CashDifference = difference;
        session.ClosingComment = request.Comment;
        session.ClosedBy = _currentUser.CodeUser;
        session.Status = "CLOSED";

        if (Math.Abs(difference) >= 0.01m)
        {
            var variancePercent = expectedCash != 0 ? (double)(Math.Abs(difference) / expectedCash * 100) : 0;
            session.RequiresApproval = Math.Abs(difference) >= VarianceApprovalThreshold;
            session.ApprovalStatus = session.RequiresApproval ? "PENDING" : "APPROVED";

            _db.CashVariances.Add(new CashVariance
            {
                CashSessionID = session.CashSessionID,
                VarianceAmount = difference,
                VariancePercentage = variancePercent,
                VarianceType = difference < 0 ? "SHORTAGE" : "OVERAGE",
                Reason = request.VarianceReason,
                Comment = request.Comment,
                ApprovalStatus = session.ApprovalStatus
            });
        }

        await _db.SaveChangesAsync();
        return Ok(await ToDto(session));
    }

    // ---- Dashboard -------------------------------------------------------

    [HttpGet("dashboard")]
    public async Task<ActionResult<SessionDashboardDto>> Dashboard()
    {
        var session = await _db.CashSessions
            .FirstOrDefaultAsync(s => s.CodeUser == _currentUser.CodeUser && s.Status == "OPEN");
        if (session == null) return NotFound(new { message = "Aucune session de caisse ouverte." });

        var transactions = await _db.Transactions.IgnoreQueryFilters()
            .Where(t => t.CashSessionID == session.CashSessionID)
            .ToListAsync();

        decimal Sum(TransactionType type) => transactions.Where(t => t.TransactionType == type).Sum(t => t.Montant);
        int Count(TransactionType type) => transactions.Count(t => t.TransactionType == type);

        var collections = Sum(TransactionType.DAILY_COLLECTION);
        var deposits = Sum(TransactionType.DEPOSIT);
        var withdrawals = Sum(TransactionType.WITHDRAWAL);
        var transfers = Sum(TransactionType.TRANSFER);
        var expected = session.OpeningCash + collections + deposits - withdrawals - transfers;

        return Ok(new SessionDashboardDto(
            session.OpeningCash, expected, expected,
            collections, deposits, withdrawals, transfers,
            Count(TransactionType.DAILY_COLLECTION), Count(TransactionType.DEPOSIT), Count(TransactionType.WITHDRAWAL), Count(TransactionType.TRANSFER),
            transactions.Count(t => t.Statut == "PENDING"), transactions.Count(t => t.Statut == "VALIDATED"), transactions.Count(t => t.Statut is "REJECTED" or "CANCELLED"),
            transactions.Sum(t => t.MontantCommission), 0m, session.Status
        ));
    }

    // ---- helpers -----------------------------------------------------

    private async Task<decimal> ComputeExpectedCash(CashSession session)
    {
        var transactions = await _db.Transactions.IgnoreQueryFilters()
            .Where(t => t.CashSessionID == session.CashSessionID)
            .ToListAsync();

        var collections = transactions.Where(t => t.TransactionType == TransactionType.DAILY_COLLECTION).Sum(t => t.Montant);
        var deposits = transactions.Where(t => t.TransactionType == TransactionType.DEPOSIT).Sum(t => t.Montant);
        var withdrawals = transactions.Where(t => t.TransactionType == TransactionType.WITHDRAWAL).Sum(t => t.Montant);
        var transfers = transactions.Where(t => t.TransactionType == TransactionType.TRANSFER).Sum(t => t.Montant);

        return session.OpeningCash + collections + deposits - withdrawals - transfers;
    }

    private async Task<CashSessionDto> ToDto(CashSession s)
    {
        var user = await _db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.CodeUser == s.CodeUser);
        var fullName = user != null ? $"{user.FirstName} {user.LastName}".Trim() : s.CodeUser;

        return new CashSessionDto(
            s.CashSessionID, s.SessionNumber, s.CodeUser, fullName, s.AgenceID,
            s.OpeningDate, s.OpeningCash, s.PreviousClosingCash,
            s.ClosingDate, s.ExpectedCash, s.PhysicalCash, s.CashDifference,
            s.PhysicalCashBreakdownJson,
            s.Status, s.RequiresApproval, s.ApprovalStatus
        );
    }
}
