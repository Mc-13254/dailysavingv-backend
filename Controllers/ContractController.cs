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
public class ContractController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ContractController(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ContractFullDto>>> GetAll()
    {
        var query = _db.Contracts.AsQueryable();
        if (!_currentUser.IsHeadOffice)
            query = query.Where(c => c.AgenceID == _currentUser.AgenceID || c.AgenceID == null);

        var result = await query
            .Select(c => new ContractFullDto(c.ContractID, c.ContractNumber, c.ClientID, c.AgenceID, c.StartDate, c.EndDate, c.ContractType, c.Description, c.Statut))
            .ToListAsync();
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult> Create(CreateContractRequest request)
    {
        var draft = new ContractTmp
        {
            ActionType = PendingActionType.CREATE,
            ContractNumber = request.ContractNumber,
            ClientID = request.ClientID,
            AgenceID = _currentUser.AgenceID,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            ContractType = request.ContractType,
            ContractDetails = request.ContractDetails,
            Description = request.Description,
            Statut = "ACTIVE",
            RenewalTerms = request.RenewalTerms,
            TerminationClause = request.TerminationClause,
            RequestUser = _currentUser.CodeUser!,
            NewData = JsonSerializer.Serialize(request)
        };

        _db.ContractTmps.Add(draft);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Contrat soumis pour validation.", pendingId = draft.PendingID });
    }

    [HttpGet("pending")]
    [Authorize(Policy = "SupervisorOrAdmin")]
    public async Task<ActionResult<IEnumerable<ContractTmp>>> GetPending()
    {
        var pending = await _db.ContractTmps
            .Where(t => t.PendingStatus == PendingStatusEnum.PENDING
                        && (_currentUser.IsHeadOffice || t.AgenceID == _currentUser.AgenceID))
            .OrderBy(t => t.RequestDate)
            .ToListAsync();
        return Ok(pending);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> Update(int id, CreateContractRequest request)
    {
        var existing = await _db.Contracts.FirstOrDefaultAsync(c => c.ContractID == id)
            ?? throw new KeyNotFoundException("Contract not found.");

        var draft = new ContractTmp
        {
            ActionType = PendingActionType.UPDATE,
            TargetContractID = id,
            ContractNumber = request.ContractNumber,
            ClientID = request.ClientID,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            ContractType = request.ContractType,
            ContractDetails = request.ContractDetails,
            Description = request.Description,
            RenewalTerms = request.RenewalTerms,
            TerminationClause = request.TerminationClause,
            AgenceID = existing.AgenceID,
            RequestUser = _currentUser.CodeUser!,
            PreviousData = JsonSerializer.Serialize(existing),
            NewData = JsonSerializer.Serialize(request)
        };

        _db.ContractTmps.Add(draft);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Modification soumise pour validation.", pendingId = draft.PendingID });
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        var existing = await _db.Contracts.FirstOrDefaultAsync(c => c.ContractID == id)
            ?? throw new KeyNotFoundException("Contract not found.");

        var draft = new ContractTmp
        {
            ActionType = PendingActionType.DELETE,
            TargetContractID = id,
            AgenceID = existing.AgenceID,
            RequestUser = _currentUser.CodeUser!,
            PreviousData = JsonSerializer.Serialize(existing)
        };

        _db.ContractTmps.Add(draft);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Suppression soumise pour validation.", pendingId = draft.PendingID });
    }

    [HttpPost("pending/{pendingId:int}/approve")]
    [Authorize(Policy = "SupervisorOrAdmin")]
    public async Task<ActionResult> Approve(int pendingId)
    {
        var draft = await _db.ContractTmps.FirstOrDefaultAsync(t => t.PendingID == pendingId)
            ?? throw new KeyNotFoundException("Pending record not found.");

        if (draft.PendingStatus != PendingStatusEnum.PENDING)
            return BadRequest(new { message = "This request has already been processed." });

        if (draft.ActionType == PendingActionType.CREATE)
        {
            _db.Contracts.Add(new Entities.Contract
            {
                ContractNumber = draft.ContractNumber!,
                ClientID = draft.ClientID,
                AgenceID = draft.AgenceID,
                StartDate = draft.StartDate ?? DateTime.UtcNow,
                EndDate = draft.EndDate,
                ContractType = draft.ContractType,
                ContractDetails = draft.ContractDetails,
                Description = draft.Description,
                Statut = draft.Statut ?? "ACTIVE",
                RenewalTerms = draft.RenewalTerms,
                TerminationClause = draft.TerminationClause,
                CreatedBy = draft.RequestUser
            });
        }
        else if (draft.ActionType == PendingActionType.UPDATE && draft.TargetContractID.HasValue)
        {
            var existing = await _db.Contracts.FirstOrDefaultAsync(c => c.ContractID == draft.TargetContractID.Value)
                ?? throw new KeyNotFoundException("Target contract no longer exists.");
            if (draft.ContractNumber != null) existing.ContractNumber = draft.ContractNumber;
            if (draft.ClientID != null) existing.ClientID = draft.ClientID;
            if (draft.StartDate.HasValue) existing.StartDate = draft.StartDate.Value;
            if (draft.EndDate.HasValue) existing.EndDate = draft.EndDate;
            if (draft.ContractType != null) existing.ContractType = draft.ContractType;
            if (draft.ContractDetails != null) existing.ContractDetails = draft.ContractDetails;
            if (draft.Description != null) existing.Description = draft.Description;
            if (draft.RenewalTerms != null) existing.RenewalTerms = draft.RenewalTerms;
            if (draft.TerminationClause != null) existing.TerminationClause = draft.TerminationClause;
        }
        else if (draft.ActionType == PendingActionType.DELETE && draft.TargetContractID.HasValue)
        {
            var existing = await _db.Contracts.FirstOrDefaultAsync(c => c.ContractID == draft.TargetContractID.Value)
                ?? throw new KeyNotFoundException("Target contract no longer exists.");
            _db.Contracts.Remove(existing);
        }

        draft.PendingStatus = PendingStatusEnum.APPROVED;
        draft.ValidationUser = _currentUser.CodeUser;
        draft.ValidationDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Contrat validé et créé en production." });
    }

    [HttpPost("pending/{pendingId:int}/reject")]
    [Authorize(Policy = "SupervisorOrAdmin")]
    public async Task<ActionResult> Reject(int pendingId, RejectRequest request)
    {
        var draft = await _db.ContractTmps.FirstOrDefaultAsync(t => t.PendingID == pendingId)
            ?? throw new KeyNotFoundException("Pending record not found.");

        draft.PendingStatus = PendingStatusEnum.REJECTED;
        draft.ValidationUser = _currentUser.CodeUser;
        draft.ValidationDate = DateTime.UtcNow;
        draft.RejectionReason = request.Reason;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Contrat rejeté." });
    }
}
