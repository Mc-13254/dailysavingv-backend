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

    [HttpPost("types")]
    [Authorize(Policy = "SupervisorOrAdmin")]
    public async Task<ActionResult> CreateType(CreateCommissionTypeRequest request)
    {
        // Commission Type is a low-risk business parameter (unlike Commission Range,
        // which affects live commission calculations) - saved directly, no Maker-Checker.
        var codeExists = await _db.CommissionTypes.AnyAsync(t => t.Code == request.Code);
        if (codeExists)
            return BadRequest(new { message = "Ce code de type de commission existe déjà." });

        _db.CommissionTypes.Add(new CommissionType
        {
            Code = request.Code,
            Name = request.Name,
            Description = request.Description,
            Statut = "ACTIVE",
            CreatedBy = _currentUser.CodeUser
        });
        await _db.SaveChangesAsync();
        return Ok(new { message = "Commission Type créé." });
    }

    [HttpPut("types/{id:int}")]
    [Authorize(Policy = "SupervisorOrAdmin")]
    public async Task<ActionResult> UpdateType(int id, CreateCommissionTypeRequest request)
    {
        var existing = await _db.CommissionTypes.FirstOrDefaultAsync(t => t.CommissionTypeID == id)
            ?? throw new KeyNotFoundException("Commission type not found.");

        var codeTaken = await _db.CommissionTypes.AnyAsync(t => t.Code == request.Code && t.CommissionTypeID != id);
        if (codeTaken)
            return BadRequest(new { message = "Ce code de type de commission existe déjà." });

        existing.Code = request.Code;
        existing.Name = request.Name;
        existing.Description = request.Description;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Commission Type modifié." });
    }

    [HttpDelete("types/{id:int}")]
    [Authorize(Policy = "SupervisorOrAdmin")]
    public async Task<ActionResult> DeleteType(int id)
    {
        var existing = await _db.CommissionTypes.FirstOrDefaultAsync(t => t.CommissionTypeID == id)
            ?? throw new KeyNotFoundException("Commission type not found.");

        var isUsed = await _db.CommissionRanges.AnyAsync(r => r.CommissionTypeID == id);
        if (isUsed)
            return BadRequest(new { message = "Impossible de supprimer : ce type de commission possède des tranches définies." });

        existing.Statut = "INACTIVE"; // soft delete
        await _db.SaveChangesAsync();
        return Ok(new { message = "Commission Type désactivé." });
    }

    // ---- Commission Ranges ----

    [HttpGet("ranges")]
    public async Task<ActionResult<IEnumerable<CommissionRangeDto>>> GetRanges([FromQuery] int? commissionTypeId)
    {
        var query = _db.CommissionRanges.Include(r => r.CommissionType).AsQueryable();
        if (commissionTypeId.HasValue)
            query = query.Where(r => r.CommissionTypeID == commissionTypeId.Value);

        var ranges = await query
            .OrderBy(r => r.CommissionTypeID).ThenBy(r => r.Inf)
            .Select(r => new CommissionRangeDto(
                r.CommissionRangeID, r.Description, r.CommissionTypeID, r.CommissionType!.Name, r.CommissionType!.Code,
                r.Inf, r.Sup, r.CalculationMethod.ToString(),
                r.Fixe, r.TAUX, r.Minimum, r.Maximum, r.CodeU, r.Statut,
                r.UserCreate, r.CreateDate, r.UserVal, r.DateValidation,
                r.LastUserModif, r.DateModification))
            .ToListAsync();

        return Ok(ranges);
    }

    // POST api/commission/ranges -> submitted as PENDING; requires Maker-Checker approval
    // before it becomes ACTIVE and can be used by the automatic commission engine.
    [HttpPost("ranges")]
    public async Task<ActionResult> CreateRange(CreateCommissionRangeRequest request)
    {
        if (request.Inf >= request.Sup)
            return BadRequest(new { message = "Inf (borne inférieure) doit être inférieur à Sup (borne supérieure)." });

        if (request.CalculationMethod == "FIXED" && (request.Fixe is null || request.TAUX is not null))
            return BadRequest(new { message = "Fixe doit être renseigné et TAUX doit être vide." });

        if (request.CalculationMethod == "PERCENTAGE" && (request.TAUX is null || request.Fixe is not null))
            return BadRequest(new { message = "TAUX doit être renseigné et Fixe doit être vide." });

        // Overlap check against currently ACTIVE ranges of the same type
        var overlaps = await _db.CommissionRanges.AnyAsync(r =>
            r.CommissionTypeID == request.CommissionTypeID &&
            r.Statut == "ACTIVE" &&
            request.Inf <= r.Sup &&
            request.Sup >= r.Inf);

        if (overlaps)
            return BadRequest(new { message = "This range overlaps an existing active range for the same Commission Type." });

        var draft = new CommissionRangeTmp
        {
            ActionType = PendingActionType.CREATE,
            CommissionTypeID = request.CommissionTypeID,
            Description = request.Description,
            Inf = request.Inf,
            Sup = request.Sup,
            CalculationMethod = request.CalculationMethod,
            Fixe = request.Fixe,
            TAUX = request.TAUX,
            Minimum = request.Minimum,
            Maximum = request.Maximum,
            CodeU = request.CodeU,
            RequestUser = _currentUser.CodeUser!,
            NewData = JsonSerializer.Serialize(request)
        };

        _db.CommissionRangeTmps.Add(draft);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Commission Range soumise pour validation.", pendingId = draft.PendingID });
    }

    [HttpPut("ranges/{id:int}")]
    public async Task<ActionResult> UpdateRange(int id, CreateCommissionRangeRequest request)
    {
        var existing = await _db.CommissionRanges.FirstOrDefaultAsync(r => r.CommissionRangeID == id)
            ?? throw new KeyNotFoundException("Commission range not found.");

        var draft = new CommissionRangeTmp
        {
            ActionType = PendingActionType.UPDATE,
            TargetCommissionRangeID = id,
            CommissionTypeID = request.CommissionTypeID,
            Description = request.Description,
            Inf = request.Inf,
            Sup = request.Sup,
            CalculationMethod = request.CalculationMethod,
            Fixe = request.Fixe,
            TAUX = request.TAUX,
            Minimum = request.Minimum,
            Maximum = request.Maximum,
            CodeU = request.CodeU,
            RequestUser = _currentUser.CodeUser!,
            PreviousData = JsonSerializer.Serialize(existing),
            NewData = JsonSerializer.Serialize(request)
        };

        _db.CommissionRangeTmps.Add(draft);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Modification soumise pour validation.", pendingId = draft.PendingID });
    }

    [HttpDelete("ranges/{id:int}")]
    public async Task<ActionResult> DeleteRange(int id)
    {
        var existing = await _db.CommissionRanges.FirstOrDefaultAsync(r => r.CommissionRangeID == id)
            ?? throw new KeyNotFoundException("Commission range not found.");

        var draft = new CommissionRangeTmp
        {
            ActionType = PendingActionType.DELETE,
            TargetCommissionRangeID = id,
            RequestUser = _currentUser.CodeUser!,
            PreviousData = JsonSerializer.Serialize(existing)
        };

        _db.CommissionRangeTmps.Add(draft);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Suppression soumise pour validation.", pendingId = draft.PendingID });
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

        if (draft.ActionType == PendingActionType.CREATE)
        {
            var method = Enum.Parse<CalculationMethod>(draft.CalculationMethod!);
            _db.CommissionRanges.Add(new CommissionRange
            {
                CommissionTypeID = draft.CommissionTypeID!.Value,
                Description = draft.Description,
                Inf = draft.Inf!.Value,
                Sup = draft.Sup!.Value,
                CalculationMethod = method,
                Fixe = draft.Fixe,
                TAUX = draft.TAUX,
                Minimum = draft.Minimum,
                Maximum = draft.Maximum,
                CodeU = draft.CodeU ?? "XAF",
                Statut = "ACTIVE",
                UserCreate = draft.RequestUser,
                UserVal = _currentUser.CodeUser,
                DateValidation = DateTime.UtcNow
            });
        }
        else if (draft.ActionType == PendingActionType.UPDATE && draft.TargetCommissionRangeID.HasValue)
        {
            var method = Enum.Parse<CalculationMethod>(draft.CalculationMethod!);
            var existing = await _db.CommissionRanges.FirstOrDefaultAsync(r => r.CommissionRangeID == draft.TargetCommissionRangeID.Value)
                ?? throw new KeyNotFoundException("Target range no longer exists.");
            existing.Description = draft.Description;
            existing.Inf = draft.Inf ?? existing.Inf;
            existing.Sup = draft.Sup ?? existing.Sup;
            existing.CalculationMethod = method;
            existing.Fixe = draft.Fixe;
            existing.TAUX = draft.TAUX;
            existing.Minimum = draft.Minimum;
            existing.Maximum = draft.Maximum;
            if (draft.CodeU != null) existing.CodeU = draft.CodeU;
            existing.LastUserModif = _currentUser.CodeUser;
            existing.DateModification = DateTime.UtcNow;
        }
        else if (draft.ActionType == PendingActionType.DELETE && draft.TargetCommissionRangeID.HasValue)
        {
            var existing = await _db.CommissionRanges.FirstOrDefaultAsync(r => r.CommissionRangeID == draft.TargetCommissionRangeID.Value)
                ?? throw new KeyNotFoundException("Target range no longer exists.");
            existing.Statut = "INACTIVE";
            existing.LastUserModif = _currentUser.CodeUser;
            existing.DateModification = DateTime.UtcNow;
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
