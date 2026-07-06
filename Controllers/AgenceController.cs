using System.Text.Json;
using DailySavingV.API.Data;
using DailySavingV.API.DTOs;
using DailySavingV.API.Entities.Pending;
using DailySavingV.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PendingActionType = DailySavingV.API.Entities.Pending.ActionType;
using PendingStatusEnum = DailySavingV.API.Entities.Pending.PendingStatus;

namespace DailySavingV.API.Controllers;

// Agences are reference/organizational data (not agency-scoped themselves -
// every user needs to see the list of agencies, e.g. to pick one on a form).
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AgenceController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AgenceController(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AgenceDto>>> GetAll()
    {
        var result = await _db.Agences
            .Select(a => new AgenceDto(a.AgenceID, a.CodeAgence, a.Nom, a.Location, a.ContactInfo, a.Statut, a.DateCreated, a.CreatedBy, a.CodeIMF))
            .ToListAsync();
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult> Create(CreateAgenceRequest request)
    {
        var draft = new AgenceTmp
        {
            ActionType = PendingActionType.CREATE,
            CodeAgence = request.CodeAgence,
            Nom = request.Nom,
            Location = request.Location,
            ContactInfo = request.ContactInfo,
            CodeIMF = request.CodeIMF,
            VilleID = request.VilleID,
            Statut = "ACTIVE",
            RequestUser = _currentUser.CodeUser!,
            NewData = JsonSerializer.Serialize(request)
        };

        _db.AgenceTmps.Add(draft);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Agence soumise pour validation.", pendingId = draft.PendingID });
    }

    [HttpGet("pending")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<IEnumerable<AgenceTmp>>> GetPending()
    {
        var pending = await _db.AgenceTmps
            .Where(t => t.PendingStatus == PendingStatusEnum.PENDING)
            .OrderBy(t => t.RequestDate)
            .ToListAsync();
        return Ok(pending);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult> Update(int id, CreateAgenceRequest request)
    {
        var existing = await _db.Agences.FirstOrDefaultAsync(a => a.AgenceID == id)
            ?? throw new KeyNotFoundException("Agence not found.");

        var draft = new AgenceTmp
        {
            ActionType = PendingActionType.UPDATE,
            TargetAgenceID = id,
            CodeAgence = request.CodeAgence,
            Nom = request.Nom,
            Location = request.Location,
            ContactInfo = request.ContactInfo,
            CodeIMF = request.CodeIMF,
            VilleID = request.VilleID,
            RequestUser = _currentUser.CodeUser!,
            PreviousData = JsonSerializer.Serialize(existing),
            NewData = JsonSerializer.Serialize(request)
        };

        _db.AgenceTmps.Add(draft);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Modification soumise pour validation.", pendingId = draft.PendingID });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult> Delete(int id)
    {
        var existing = await _db.Agences.FirstOrDefaultAsync(a => a.AgenceID == id)
            ?? throw new KeyNotFoundException("Agence not found.");

        var draft = new AgenceTmp
        {
            ActionType = PendingActionType.DELETE,
            TargetAgenceID = id,
            RequestUser = _currentUser.CodeUser!,
            PreviousData = JsonSerializer.Serialize(existing)
        };

        _db.AgenceTmps.Add(draft);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Suppression soumise pour validation.", pendingId = draft.PendingID });
    }

    [HttpPost("pending/{pendingId:int}/approve")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult> Approve(int pendingId)
    {
        var draft = await _db.AgenceTmps.FirstOrDefaultAsync(t => t.PendingID == pendingId)
            ?? throw new KeyNotFoundException("Pending record not found.");

        if (draft.PendingStatus != PendingStatusEnum.PENDING)
            return BadRequest(new { message = "This request has already been processed." });

        if (draft.ActionType == PendingActionType.CREATE)
        {
            _db.Agences.Add(new Entities.Agence
            {
                CodeAgence = draft.CodeAgence!,
                Nom = draft.Nom!,
                Location = draft.Location,
                ContactInfo = draft.ContactInfo,
                CodeIMF = draft.CodeIMF!,
                VilleID = draft.VilleID,
                Statut = draft.Statut ?? "ACTIVE",
                CreatedBy = draft.RequestUser
            });
        }
        else if (draft.ActionType == PendingActionType.UPDATE && draft.TargetAgenceID.HasValue)
        {
            var existing = await _db.Agences.FirstOrDefaultAsync(a => a.AgenceID == draft.TargetAgenceID.Value)
                ?? throw new KeyNotFoundException("Target agency no longer exists.");
            if (draft.Nom != null) existing.Nom = draft.Nom;
            if (draft.Location != null) existing.Location = draft.Location;
            if (draft.ContactInfo != null) existing.ContactInfo = draft.ContactInfo;
            if (draft.Statut != null) existing.Statut = draft.Statut;
        }
        else if (draft.ActionType == PendingActionType.DELETE && draft.TargetAgenceID.HasValue)
        {
            var existing = await _db.Agences.FirstOrDefaultAsync(a => a.AgenceID == draft.TargetAgenceID.Value)
                ?? throw new KeyNotFoundException("Target agency no longer exists.");
            existing.Statut = "INACTIVE";
        }

        draft.PendingStatus = PendingStatusEnum.APPROVED;
        draft.ValidationUser = _currentUser.CodeUser;
        draft.ValidationDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Agence validée et créée en production." });
    }

    [HttpPost("pending/{pendingId:int}/reject")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult> Reject(int pendingId, RejectRequest request)
    {
        var draft = await _db.AgenceTmps.FirstOrDefaultAsync(t => t.PendingID == pendingId)
            ?? throw new KeyNotFoundException("Pending record not found.");

        draft.PendingStatus = PendingStatusEnum.REJECTED;
        draft.ValidationUser = _currentUser.CodeUser;
        draft.ValidationDate = DateTime.UtcNow;
        draft.RejectionReason = request.Reason;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Agence rejetée." });
    }
}
