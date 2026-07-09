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
        var query = _db.Contracts.Include(c => c.CommissionType).AsQueryable();
        if (!_currentUser.IsHeadOffice)
            query = query.Where(c => c.AgenceID == _currentUser.AgenceID || c.AgenceID == null);

        var contracts = await query.OrderByDescending(c => c.CreatedDate).ToListAsync();

        var clientIds = contracts.Where(c => c.ClientID != null).Select(c => c.ClientID!).Distinct().ToList();
        var clientNames = await _db.Clients.IgnoreQueryFilters().Where(c => clientIds.Contains(c.ClientID))
            .ToDictionaryAsync(c => c.ClientID, c => $"{c.Nom} {c.Prenom}".Trim());

        var collectorIds = contracts.Where(c => c.CollectorID != null).Select(c => c.CollectorID!).Distinct().ToList();
        var collectorNames = await _db.Collectors.IgnoreQueryFilters().Where(c => collectorIds.Contains(c.CollectorID))
            .ToDictionaryAsync(c => c.CollectorID, c => $"{c.Name} {c.Surname}".Trim());

        var result = contracts.Select(c => new ContractFullDto(
            c.ContractID, c.ContractNumber, c.ClientID, c.ClientID != null ? clientNames.GetValueOrDefault(c.ClientID) : null,
            c.AgenceID, c.CollectorID, c.CollectorID != null ? collectorNames.GetValueOrDefault(c.CollectorID) : null,
            c.CommissionTypeID, c.CommissionType?.Name, c.CommissionRangeID,
            c.CollectionFrequency, c.CollectionDay,
            c.OpeningDeposit, c.MinimumBalance, c.MaximumBalance, c.PenaltyRules, c.GracePeriod,
            c.StartDate, c.EndDate, c.ContractType, c.Description, c.Statut,
            c.TerminationReason, c.CustomerSigned, c.OfficerSigned
        ));
        return Ok(result);
    }

    // GET api/contract/eligible-clients -> every validated client. A client can hold
    // several contracts (e.g. one per savings product), so no longer restricted to
    // clients without an existing contract.
    [HttpGet("eligible-clients")]
    public async Task<ActionResult<IEnumerable<ClientDto>>> GetEligibleClients()
    {
        var clients = await _db.Clients
            .Where(c => c.ValidationStatus == "VALIDATED")
            .ToListAsync();

        return Ok(clients.Select(c => new ClientDto(
            c.ClientID, c.Nom, c.Prenom, c.PhoneNumber, c.Email, c.ClientType, c.AgenceID, c.ValidationStatus, 0
        )));
    }

    [HttpPost]
    public async Task<ActionResult> Create(CreateContractRequest request)
    {
        var client = await _db.Clients.FirstOrDefaultAsync(c => c.ClientID == request.ClientID)
            ?? throw new KeyNotFoundException("Client not found in your agency.");

        if (client.ValidationStatus != "VALIDATED")
            return BadRequest(new { message = "Un contrat ne peut être créé que pour un client Actif (validé)." });

        var count = await _db.Contracts.IgnoreQueryFilters().CountAsync();
        var contractNumber = $"CT-{(count + 1):D6}";

        var draft = new ContractTmp
        {
            ActionType = PendingActionType.CREATE,
            ContractNumber = contractNumber,
            ClientID = request.ClientID,
            AgenceID = _currentUser.AgenceID,
            CollectorID = client.CollectorID,
            CommissionTypeID = request.CommissionTypeID,
            CommissionRangeID = request.CommissionRangeID,
            CollectionFrequency = request.CollectionFrequency,
            CollectionDay = request.CollectionDay,
            OpeningDeposit = request.OpeningDeposit,
            MinimumBalance = request.MinimumBalance,
            MaximumBalance = request.MaximumBalance,
            PenaltyRules = request.PenaltyRules,
            GracePeriod = request.GracePeriod,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            ContractType = request.ContractType,
            ContractTypeID = request.ContractTypeID,
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

        return Ok(new { message = "Contrat soumis pour validation.", pendingId = draft.PendingID, contractNumber });
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

    // Only a limited set of fields may be edited; Client can never change (see spec).
    [HttpPut("{id:int}")]
    public async Task<ActionResult> Update(int id, UpdateContractRequest request)
    {
        var existing = await _db.Contracts.FirstOrDefaultAsync(c => c.ContractID == id)
            ?? throw new KeyNotFoundException("Contract not found.");

        var draft = new ContractTmp
        {
            ActionType = PendingActionType.UPDATE,
            TargetContractID = id,
            CollectionFrequency = request.CollectionFrequency,
            CollectionDay = request.CollectionDay,
            EndDate = request.EndDate,
            Statut = request.Statut,
            CommissionTypeID = request.CommissionTypeID,
            CommissionRangeID = request.CommissionRangeID,
            AgenceID = existing.AgenceID,
            RequestUser = _currentUser.CodeUser!,
            PreviousData = JsonSerializer.Serialize(existing),
            NewData = JsonSerializer.Serialize(request)
        };

        _db.ContractTmps.Add(draft);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Modification soumise pour validation.", pendingId = draft.PendingID });
    }

    // Soft close — history is kept forever (Contract row is never removed).
    [HttpPost("{id:int}/terminate")]
    [Authorize(Policy = "SupervisorOrAdmin")]
    public async Task<ActionResult> Terminate(int id, TerminateContractRequest request)
    {
        var contract = await _db.Contracts.FirstOrDefaultAsync(c => c.ContractID == id)
            ?? throw new KeyNotFoundException("Contract not found.");

        contract.Statut = "TERMINATED";
        contract.TerminationReason = request.Reason;
        contract.TerminationDate = DateTime.UtcNow;
        contract.UpdatedBy = _currentUser.CodeUser;
        contract.UpdatedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Contrat clôturé (soft close)." });
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
            _db.Contracts.Add(new Contract
            {
                ContractNumber = draft.ContractNumber!,
                ClientID = draft.ClientID,
                AgenceID = draft.AgenceID,
                CollectorID = draft.CollectorID,
                CommissionTypeID = draft.CommissionTypeID,
                CommissionRangeID = draft.CommissionRangeID,
                CollectionFrequency = draft.CollectionFrequency ?? "DAILY",
                CollectionDay = draft.CollectionDay,
                OpeningDeposit = draft.OpeningDeposit,
                MinimumBalance = draft.MinimumBalance,
                MaximumBalance = draft.MaximumBalance,
                PenaltyRules = draft.PenaltyRules,
                GracePeriod = draft.GracePeriod,
                StartDate = draft.StartDate ?? DateTime.UtcNow,
                EndDate = draft.EndDate,
                ContractType = draft.ContractType,
                ContractTypeID = draft.ContractTypeID,
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
            if (draft.CollectionFrequency != null) existing.CollectionFrequency = draft.CollectionFrequency;
            if (draft.CollectionDay != null) existing.CollectionDay = draft.CollectionDay;
            if (draft.EndDate.HasValue) existing.EndDate = draft.EndDate;
            if (draft.Statut != null) existing.Statut = draft.Statut;
            if (draft.CommissionTypeID.HasValue) existing.CommissionTypeID = draft.CommissionTypeID;
            if (draft.CommissionRangeID.HasValue) existing.CommissionRangeID = draft.CommissionRangeID;
            existing.UpdatedBy = _currentUser.CodeUser;
            existing.UpdatedDate = DateTime.UtcNow;
        }
        else if (draft.ActionType == PendingActionType.DELETE && draft.TargetContractID.HasValue)
        {
            var existing = await _db.Contracts.FirstOrDefaultAsync(c => c.ContractID == draft.TargetContractID.Value)
                ?? throw new KeyNotFoundException("Target contract no longer exists.");
            existing.Statut = "TERMINATED";
            existing.TerminationReason = "Other";
            existing.TerminationDate = DateTime.UtcNow;
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
