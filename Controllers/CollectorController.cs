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

/// <summary>
/// Reference implementation of the Maker-Checker pattern used across the
/// whole system. Every other entity (Client, CommissionRange, Agence, ...)
/// follows this exact shape: GET reads production (auto agency-scoped via
/// the DbContext filter), POST/PUT/DELETE write to the *Tmp table instead
/// of touching production, and a Supervisor/Admin approves or rejects.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CollectorController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CollectorController(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    // GET api/collector
    // Automatically returns ONLY the connected user's agency, thanks to the
    // HasQueryFilter configured on Collector in AppDbContext. No manual
    // ".Where(AgenceID == ...)" needed here or in any other read endpoint.
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CollectorDto>>> GetAll([FromQuery] string? search)
    {
        var query = _db.Collectors.Include(c => c.Agence).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c => c.Name.Contains(search) || c.CollectorID.Contains(search));

        var result = await query
            .Select(c => new CollectorDto(
                c.CollectorID, c.CodeUser, c.Name, c.PhoneNumber,
                c.AgenceID, c.Agence!.Nom, c.IsActive, c.DateEmploi,
                c.ContactType, c.CodeTerminal, c.Plafond))
            .ToListAsync();

        return Ok(result);
    }

    // GET api/collector/pending  (records awaiting validation for this agency's scope)
    [HttpGet("pending")]
    [Authorize(Policy = "SupervisorOrAdmin")]
    public async Task<ActionResult<IEnumerable<CollectorTMP>>> GetPending()
    {
        var pending = await _db.CollectorTMPs
            .Where(t => t.PendingStatus == PendingStatusEnum.PENDING
                        && (_currentUser.IsHeadOffice || t.AgenceID == _currentUser.AgenceID))
            .OrderBy(t => t.RequestDate)
            .ToListAsync();

        return Ok(pending);
    }

    // POST api/collector  -> creates a PENDING record, NOT a production row
    [HttpPost]
    public async Task<ActionResult> Create(CreateCollectorRequest request)
    {
        if (_currentUser.AgenceID is null && !_currentUser.IsHeadOffice)
            return Forbid();

        var draft = new CollectorTMP
        {
            ActionType = PendingActionType.CREATE,
            CodeUser = request.CodeUser,
            Name = request.Name,
            PhoneNumber = request.PhoneNumber,
            AgenceID = _currentUser.AgenceID,
            ZoneCollecteID = request.ZoneCollecteID,
            IsActive = true,
            DateEmploi = request.DateEmploi,
            ContactType = request.ContactType,
            CodeTerminal = request.CodeTerminal,
            Plafond = request.Plafond,
            RequestUser = _currentUser.CodeUser!,
            NewData = JsonSerializer.Serialize(request)
        };

        _db.CollectorTMPs.Add(draft);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Collecteur soumis pour validation (Maker-Checker).", pendingId = draft.PendingID });
    }

    // PUT api/collector/{id}  -> creates a PENDING update record referencing the existing row
    [HttpPut("{id}")]
    public async Task<ActionResult> Update(string id, UpdateCollectorRequest request)
    {
        var existing = await _db.Collectors.FirstOrDefaultAsync(c => c.CollectorID == id)
            ?? throw new KeyNotFoundException("Collector not found in your agency.");

        var draft = new CollectorTMP
        {
            ActionType = PendingActionType.UPDATE,
            TargetCollectorID = id,
            Name = request.Name,
            PhoneNumber = request.PhoneNumber,
            IsActive = request.IsActive,
            ZoneCollecteID = request.ZoneCollecteID,
            ContactType = request.ContactType,
            CodeTerminal = request.CodeTerminal,
            Plafond = request.Plafond,
            AgenceID = existing.AgenceID,
            RequestUser = _currentUser.CodeUser!,
            PreviousData = JsonSerializer.Serialize(existing),
            NewData = JsonSerializer.Serialize(request)
        };

        _db.CollectorTMPs.Add(draft);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Modification soumise pour validation.", pendingId = draft.PendingID });
    }

    // DELETE api/collector/{id} -> creates a PENDING delete record; nothing removed yet
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(string id)
    {
        var existing = await _db.Collectors.FirstOrDefaultAsync(c => c.CollectorID == id)
            ?? throw new KeyNotFoundException("Collector not found in your agency.");

        var draft = new CollectorTMP
        {
            ActionType = PendingActionType.DELETE,
            TargetCollectorID = id,
            AgenceID = existing.AgenceID,
            RequestUser = _currentUser.CodeUser!,
            PreviousData = JsonSerializer.Serialize(existing)
        };

        _db.CollectorTMPs.Add(draft);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Suppression soumise pour validation.", pendingId = draft.PendingID });
    }

    // POST api/collector/pending/{pendingId}/approve
    [HttpPost("pending/{pendingId:int}/approve")]
    [Authorize(Policy = "SupervisorOrAdmin")]
    public async Task<ActionResult> Approve(int pendingId)
    {
        var draft = await _db.CollectorTMPs.FirstOrDefaultAsync(t => t.PendingID == pendingId)
            ?? throw new KeyNotFoundException("Pending record not found.");

        if (draft.PendingStatus != PendingStatusEnum.PENDING)
            return BadRequest(new { message = "This request has already been processed." });

        switch (draft.ActionType)
        {
            case PendingActionType.CREATE:
                var newId = await GenerateNextCollectorId();
                _db.Collectors.Add(new Collector
                {
                    CollectorID = newId,
                    CodeUser = draft.CodeUser!,
                    Name = draft.Name!,
                    PhoneNumber = draft.PhoneNumber,
                    AgenceID = draft.AgenceID!.Value,
                    ZoneCollecteID = draft.ZoneCollecteID,
                    IsActive = draft.IsActive ?? true,
                    DateEmploi = draft.DateEmploi,
                    ContactType = draft.ContactType,
                    CodeTerminal = draft.CodeTerminal,
                    Plafond = draft.Plafond ?? 0,
                    CreatedBy = draft.RequestUser
                });
                break;

            case PendingActionType.UPDATE:
                var toUpdate = await _db.Collectors.FirstOrDefaultAsync(c => c.CollectorID == draft.TargetCollectorID)
                    ?? throw new KeyNotFoundException("Target collector no longer exists.");
                if (draft.Name != null) toUpdate.Name = draft.Name;
                if (draft.PhoneNumber != null) toUpdate.PhoneNumber = draft.PhoneNumber;
                if (draft.IsActive.HasValue) toUpdate.IsActive = draft.IsActive.Value;
                if (draft.ZoneCollecteID.HasValue) toUpdate.ZoneCollecteID = draft.ZoneCollecteID;
                if (draft.ContactType != null) toUpdate.ContactType = draft.ContactType;
                if (draft.CodeTerminal != null) toUpdate.CodeTerminal = draft.CodeTerminal;
                if (draft.Plafond.HasValue) toUpdate.Plafond = draft.Plafond.Value;
                break;

            case PendingActionType.DELETE:
                var toDelete = await _db.Collectors.FirstOrDefaultAsync(c => c.CollectorID == draft.TargetCollectorID)
                    ?? throw new KeyNotFoundException("Target collector no longer exists.");
                _db.Collectors.Remove(toDelete);
                break;
        }

        draft.PendingStatus = PendingStatusEnum.APPROVED;
        draft.ValidationUser = _currentUser.CodeUser;
        draft.ValidationDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Validé et appliqué en production." });
    }

    // POST api/collector/pending/{pendingId}/reject
    [HttpPost("pending/{pendingId:int}/reject")]
    [Authorize(Policy = "SupervisorOrAdmin")]
    public async Task<ActionResult> Reject(int pendingId, RejectRequest request)
    {
        var draft = await _db.CollectorTMPs.FirstOrDefaultAsync(t => t.PendingID == pendingId)
            ?? throw new KeyNotFoundException("Pending record not found.");

        if (draft.PendingStatus != PendingStatusEnum.PENDING)
            return BadRequest(new { message = "This request has already been processed." });

        draft.PendingStatus = PendingStatusEnum.REJECTED;
        draft.ValidationUser = _currentUser.CodeUser;
        draft.ValidationDate = DateTime.UtcNow;
        draft.RejectionReason = request.Reason;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Demande rejetée." });
    }

    private async Task<string> GenerateNextCollectorId()
    {
        var count = await _db.Collectors.IgnoreQueryFilters().CountAsync();
        return $"CO-{(count + 1):D5}";
    }
}
