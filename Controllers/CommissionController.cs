using System.Text.Json;
using DailySavingV.API.Data;
using DailySavingV.API.DTOs;
using DailySavingV.API.Entities;
using DailySavingV.API.Entities.Pending;
using DailySavingV.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PendingActionType = DailySavingV.API.Entities.Pending.ActionType;
using PendingStatusEnum = DailySavingV.API.Entities.Pending.PendingStatus;

namespace DailySavingV.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CommissionController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CommissionController(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    // ---- Commission Types (reference data, not agency-scoped) ----

    [HttpGet("types")]
    public async Task<ActionResult<IEnumerable<CommissionTypeDto>>> GetTypes()
    {
        var types = await _db.CommissionTypes
            .Select(t => new CommissionTypeDto(t.CommissionTypeID, t.Code, t.Name, t.Description, t.Statut))
            .ToListAsync();
        return Ok(types);
    }

    // ---- Commission Ranges ----

    [HttpGet("ranges")]
    public async Task<ActionResult<IEnumerable<CommissionRangeDto>>> GetRanges([FromQuery] int? commissionTypeId)
    {
        var query = _db.CommissionRanges.Include(r => r.CommissionType).AsQueryable();
        if (commissionTypeId.HasValue)
            query = query.Where(r => r.CommissionTypeID == commissionTypeId.Value);

        var ranges = await query
            .OrderBy(r => r.CommissionTypeID).ThenBy(r => r.MinAmount)
            .Select(r => new CommissionRangeDto(
                r.CommissionRangeID, r.CommissionTypeID, r.CommissionType!.Name,
                r.MinAmount, r.MaxAmount, r.CalculationMethod.ToString(),
                r.FixedAmount, r.PercentageRate, r.Currency, r.Statut))
            .ToListAsync();

        return Ok(ranges);
    }

    // POST api/commission/ranges -> submitted as PENDING; requires Maker-Checker approval
    // before it becomes ACTIVE and can be used by the automatic commission engine.
    [HttpPost("ranges")]
    public async Task<ActionResult> CreateRange(CreateCommissionRangeRequest request)
    {
        if (request.MinAmount >= request.MaxAmount)
            return BadRequest(new { message = "Minimum Amount must be less than Maximum Amount." });

        if (request.CalculationMethod == "FIXED" && (request.FixedAmount is null || request.PercentageRate is not null))
            return BadRequest(new { message = "Fixed Amount must be set and Percentage Rate must be empty." });

        if (request.CalculationMethod == "PERCENTAGE" && (request.PercentageRate is null || request.FixedAmount is not null))
            return BadRequest(new { message = "Percentage Rate must be set and Fixed Amount must be empty." });

        // Overlap check against currently ACTIVE ranges of the same type
        var overlaps = await _db.CommissionRanges.AnyAsync(r =>
            r.CommissionTypeID == request.CommissionTypeID &&
            r.Statut == "ACTIVE" &&
            request.MinAmount <= r.MaxAmount &&
            request.MaxAmount >= r.MinAmount);

        if (overlaps)
            return BadRequest(new { message = "This range overlaps an existing active range for the same Commission Type." });

        var draft = new CommissionRangeTmp
        {
            ActionType = PendingActionType.CREATE,
            CommissionTypeID = request.CommissionTypeID,
            MinAmount = request.MinAmount,
            MaxAmount = request.MaxAmount,
            CalculationMethod = request.CalculationMethod,
            FixedAmount = request.FixedAmount,
            PercentageRate = request.PercentageRate,
            Currency = request.Currency,
            RequestUser = _currentUser.CodeUser!,
            NewData = JsonSerializer.Serialize(request)
        };

        _db.CommissionRangeTmps.Add(draft);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Commission Range soumise pour validation.", pendingId = draft.PendingID });
    }

    [HttpGet("ranges/pending")]
    [Authorize(Policy = "SupervisorOrAdmin")]
    public async Task<ActionResult<IEnumerable<CommissionRangeTmp>>> GetPendingRanges()
    {
        var pending = await _db.CommissionRangeTmps
            .Where(t => t.PendingStatus == PendingStatusEnum.PENDING)
            .OrderBy(t => t.RequestDate)
            .ToListAsync();
        return Ok(pending);
    }

    [HttpPost("ranges/pending/{pendingId:int}/approve")]
    [Authorize(Policy = "SupervisorOrAdmin")]
    public async Task<ActionResult> ApproveRange(int pendingId)
    {
        var draft = await _db.CommissionRangeTmps.FirstOrDefaultAsync(t => t.PendingID == pendingId)
            ?? throw new KeyNotFoundException("Pending record not found.");

        if (draft.PendingStatus != PendingStatusEnum.PENDING)
            return BadRequest(new { message = "This request has already been processed." });

        // Re-check overlap at approval time too (state may have changed since submission)
        var method = Enum.Parse<CalculationMethod>(draft.CalculationMethod!);

        if (draft.ActionType == PendingActionType.CREATE)
        {
            _db.CommissionRanges.Add(new CommissionRange
            {
                CommissionTypeID = draft.CommissionTypeID!.Value,
                MinAmount = draft.MinAmount!.Value,
                MaxAmount = draft.MaxAmount!.Value,
                CalculationMethod = method,
                FixedAmount = draft.FixedAmount,
                PercentageRate = draft.PercentageRate,
                Currency = draft.Currency ?? "XAF",
                Statut = "ACTIVE",
                CreatedBy = draft.RequestUser,
                ValidatedBy = _currentUser.CodeUser,
                ValidationDate = DateTime.UtcNow
            });
        }
        else if (draft.ActionType == PendingActionType.UPDATE && draft.TargetCommissionRangeID.HasValue)
        {
            var existing = await _db.CommissionRanges.FirstOrDefaultAsync(r => r.CommissionRangeID == draft.TargetCommissionRangeID.Value)
                ?? throw new KeyNotFoundException("Target range no longer exists.");
            existing.MinAmount = draft.MinAmount ?? existing.MinAmount;
            existing.MaxAmount = draft.MaxAmount ?? existing.MaxAmount;
            existing.CalculationMethod = method;
            existing.FixedAmount = draft.FixedAmount;
            existing.PercentageRate = draft.PercentageRate;
            existing.ValidatedBy = _currentUser.CodeUser;
            existing.ValidationDate = DateTime.UtcNow;
        }
        else if (draft.ActionType == PendingActionType.DELETE && draft.TargetCommissionRangeID.HasValue)
        {
            var existing = await _db.CommissionRanges.FirstOrDefaultAsync(r => r.CommissionRangeID == draft.TargetCommissionRangeID.Value)
                ?? throw new KeyNotFoundException("Target range no longer exists.");
            existing.Statut = "INACTIVE";
        }

        draft.PendingStatus = PendingStatusEnum.APPROVED;
        draft.ValidationUser = _currentUser.CodeUser;
        draft.ValidationDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Commission Range validée et active." });
    }

    [HttpPost("ranges/pending/{pendingId:int}/reject")]
    [Authorize(Policy = "SupervisorOrAdmin")]
    public async Task<ActionResult> RejectRange(int pendingId, RejectRequest request)
    {
        var draft = await _db.CommissionRangeTmps.FirstOrDefaultAsync(t => t.PendingID == pendingId)
            ?? throw new KeyNotFoundException("Pending record not found.");

        draft.PendingStatus = PendingStatusEnum.REJECTED;
        draft.ValidationUser = _currentUser.CodeUser;
        draft.ValidationDate = DateTime.UtcNow;
        draft.RejectionReason = request.Reason;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Commission Range rejetée." });
    }
}
