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

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ClientController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ClientController(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    // Auto agency-scoped via the Client global query filter in AppDbContext
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ClientDto>>> GetAll([FromQuery] string? search)
    {
        var query = _db.Clients.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c => c.Nom.Contains(search) || c.ClientID.Contains(search));

        var result = await query
            .Select(c => new ClientDto(c.ClientID, c.Nom, c.Prenom, c.PhoneNumber, c.Email, c.ClientType, c.AgenceID, c.ValidationStatus))
            .ToListAsync();

        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult> Create(CreateClientRequest request)
    {
        var draft = new ClientTmp
        {
            ActionType = PendingActionType.CREATE,
            Nom = request.Nom,
            Prenom = request.Prenom,
            Sexe = request.Sexe,
            PhoneNumber = request.PhoneNumber,
            Address = request.Address,
            Email = request.Email,
            CompanyName = request.CompanyName,
            ClientType = request.ClientType,
            TypeCNIID = request.TypeCNIID,
            NumeroCNI = request.NumeroCNI,
            CollectorID = request.CollectorID,
            AgenceID = _currentUser.AgenceID,
            RequestUser = _currentUser.CodeUser!,
            NewData = JsonSerializer.Serialize(request)
        };

        _db.ClientTmps.Add(draft);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Client soumis pour validation.", pendingId = draft.PendingID });
    }

    [HttpGet("pending")]
    [Authorize(Policy = "SupervisorOrAdmin")]
    public async Task<ActionResult<IEnumerable<ClientTmp>>> GetPending()
    {
        var pending = await _db.ClientTmps
            .Where(t => t.PendingStatus == PendingStatusEnum.PENDING
                        && (_currentUser.IsHeadOffice || t.AgenceID == _currentUser.AgenceID))
            .OrderBy(t => t.RequestDate)
            .ToListAsync();
        return Ok(pending);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(string id, UpdateClientRequest request)
    {
        var existing = await _db.Clients.FirstOrDefaultAsync(c => c.ClientID == id)
            ?? throw new KeyNotFoundException("Client not found in your agency.");

        var draft = new ClientTmp
        {
            ActionType = PendingActionType.UPDATE,
            TargetClientID = id,
            Nom = request.Nom,
            Prenom = request.Prenom,
            Sexe = request.Sexe,
            PhoneNumber = request.PhoneNumber,
            Address = request.Address,
            Email = request.Email,
            CompanyName = request.CompanyName,
            ClientType = request.ClientType,
            NumeroCNI = request.NumeroCNI,
            CollectorID = request.CollectorID,
            AgenceID = existing.AgenceID,
            RequestUser = _currentUser.CodeUser!,
            PreviousData = JsonSerializer.Serialize(existing),
            NewData = JsonSerializer.Serialize(request)
        };

        _db.ClientTmps.Add(draft);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Modification soumise pour validation.", pendingId = draft.PendingID });
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(string id)
    {
        var existing = await _db.Clients.FirstOrDefaultAsync(c => c.ClientID == id)
            ?? throw new KeyNotFoundException("Client not found in your agency.");

        var draft = new ClientTmp
        {
            ActionType = PendingActionType.DELETE,
            TargetClientID = id,
            AgenceID = existing.AgenceID,
            RequestUser = _currentUser.CodeUser!,
            PreviousData = JsonSerializer.Serialize(existing)
        };

        _db.ClientTmps.Add(draft);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Suppression soumise pour validation.", pendingId = draft.PendingID });
    }

    [HttpPost("pending/{pendingId:int}/approve")]
    [Authorize(Policy = "SupervisorOrAdmin")]
    public async Task<ActionResult> Approve(int pendingId)
    {
        var draft = await _db.ClientTmps.FirstOrDefaultAsync(t => t.PendingID == pendingId)
            ?? throw new KeyNotFoundException("Pending record not found.");

        if (draft.PendingStatus != PendingStatusEnum.PENDING)
            return BadRequest(new { message = "This request has already been processed." });

        if (draft.ActionType == PendingActionType.CREATE)
        {
            var statusId = await _db.ClientStatuses.Where(s => s.Code == "VALIDATED").Select(s => s.ClientStatusID).FirstAsync();
            var count = await _db.Clients.IgnoreQueryFilters().CountAsync();
            var newId = $"CL-{(count + 1):D5}";

            _db.Clients.Add(new Entities.Client
            {
                ClientID = newId,
                Nom = draft.Nom!,
                Prenom = draft.Prenom,
                Sexe = draft.Sexe,
                PhoneNumber = draft.PhoneNumber,
                Address = draft.Address,
                Email = draft.Email,
                CompanyName = draft.CompanyName,
                ClientType = draft.ClientType ?? "INDIVIDUAL",
                ClientStatusID = statusId,
                TypeCNIID = draft.TypeCNIID,
                NumeroCNI = draft.NumeroCNI,
                CollectorID = draft.CollectorID,
                AgenceID = draft.AgenceID!.Value,
                ValidationStatus = "VALIDATED",
                CreatedBy = draft.RequestUser
            });
        }
        else if (draft.ActionType == PendingActionType.UPDATE && draft.TargetClientID != null)
        {
            var existing = await _db.Clients.FirstOrDefaultAsync(c => c.ClientID == draft.TargetClientID)
                ?? throw new KeyNotFoundException("Target client no longer exists.");
            if (draft.Nom != null) existing.Nom = draft.Nom;
            if (draft.Prenom != null) existing.Prenom = draft.Prenom;
            if (draft.Sexe != null) existing.Sexe = draft.Sexe;
            if (draft.PhoneNumber != null) existing.PhoneNumber = draft.PhoneNumber;
            if (draft.Address != null) existing.Address = draft.Address;
            if (draft.Email != null) existing.Email = draft.Email;
            if (draft.CompanyName != null) existing.CompanyName = draft.CompanyName;
            if (draft.ClientType != null) existing.ClientType = draft.ClientType;
            if (draft.NumeroCNI != null) existing.NumeroCNI = draft.NumeroCNI;
            if (draft.CollectorID != null) existing.CollectorID = draft.CollectorID;
        }
        else if (draft.ActionType == PendingActionType.DELETE && draft.TargetClientID != null)
        {
            // Soft-delete: Client is referenced by Accounts/Transactions, so we
            // deactivate rather than physically remove the row.
            var existing = await _db.Clients.FirstOrDefaultAsync(c => c.ClientID == draft.TargetClientID)
                ?? throw new KeyNotFoundException("Target client no longer exists.");
            var blockedStatusId = await _db.ClientStatuses.Where(s => s.Code == "BLOCKED").Select(s => s.ClientStatusID).FirstOrDefaultAsync();
            if (blockedStatusId != 0) existing.ClientStatusID = blockedStatusId;
            existing.ValidationStatus = "BLOCKED";
        }

        draft.PendingStatus = PendingStatusEnum.APPROVED;
        draft.ValidationUser = _currentUser.CodeUser;
        draft.ValidationDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Client validé et créé en production." });
    }

    [HttpPost("pending/{pendingId:int}/reject")]
    [Authorize(Policy = "SupervisorOrAdmin")]
    public async Task<ActionResult> Reject(int pendingId, RejectRequest request)
    {
        var draft = await _db.ClientTmps.FirstOrDefaultAsync(t => t.PendingID == pendingId)
            ?? throw new KeyNotFoundException("Pending record not found.");

        draft.PendingStatus = PendingStatusEnum.REJECTED;
        draft.ValidationUser = _currentUser.CodeUser;
        draft.ValidationDate = DateTime.UtcNow;
        draft.RejectionReason = request.Reason;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Client rejeté." });
    }
}
