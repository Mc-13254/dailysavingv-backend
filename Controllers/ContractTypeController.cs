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
public class ContractTypeController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ContractTypeController(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    private static ContractTypeDto ToDto(Entities.ContractType c) => new(
        c.ContractTypeID, c.ContractCode, c.ContractName, c.ShortName, c.Description,
        c.AllowDailyCollection, c.AllowWeeklyCollection, c.AllowMonthlyCollection,
        c.MinimumCollectionAmount, c.MaximumCollectionAmount, c.DefaultCollectionAmount,
        c.MinimumOpeningBalance, c.MaximumBalance, c.InterestRate,
        c.ContractDuration, c.DurationUnit, c.PenaltyAmount, c.GracePeriod,
        c.Statut, c.CreatedBy, c.CreatedDate, c.UpdatedBy, c.UpdatedDate
    );

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ContractTypeDto>>> GetAll()
    {
        var result = await _db.ContractTypes.ToListAsync();
        return Ok(result.Select(ToDto));
    }

    // Only Active Contract Types can be selected when creating Clients.
    [HttpGet("active")]
    public async Task<ActionResult> GetActive()
    {
        var result = await _db.ContractTypes.Where(c => c.Statut == "ACTIVE")
            .Select(c => new { c.ContractTypeID, c.ContractCode, c.ContractName })
            .ToListAsync();
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult> Create(CreateContractTypeRequest request)
    {
        var nameExists = await _db.ContractTypes.AnyAsync(c => c.ContractName == request.ContractName);
        if (nameExists)
            return BadRequest(new { message = "A contract type with this name already exists." });

        var draft = new ContractTypeTmp
        {
            ActionType = PendingActionType.CREATE,
            ContractName = request.ContractName,
            ShortName = request.ShortName,
            Description = request.Description,
            AllowDailyCollection = request.AllowDailyCollection,
            AllowWeeklyCollection = request.AllowWeeklyCollection,
            AllowMonthlyCollection = request.AllowMonthlyCollection,
            MinimumCollectionAmount = request.MinimumCollectionAmount,
            MaximumCollectionAmount = request.MaximumCollectionAmount,
            DefaultCollectionAmount = request.DefaultCollectionAmount,
            MinimumOpeningBalance = request.MinimumOpeningBalance,
            MaximumBalance = request.MaximumBalance,
            InterestRate = request.InterestRate,
            ContractDuration = request.ContractDuration,
            DurationUnit = request.DurationUnit,
            PenaltyAmount = request.PenaltyAmount,
            GracePeriod = request.GracePeriod,
            Statut = "ACTIVE",
            RequestUser = _currentUser.CodeUser!,
            NewData = JsonSerializer.Serialize(request)
        };

        _db.ContractTypeTmps.Add(draft);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Type de contrat soumis pour validation.", pendingId = draft.PendingID });
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult> Update(int id, UpdateContractTypeRequest request)
    {
        var existing = await _db.ContractTypes.FirstOrDefaultAsync(c => c.ContractTypeID == id)
            ?? throw new KeyNotFoundException("Contract type not found.");

        var nameTaken = await _db.ContractTypes.AnyAsync(c => c.ContractName == request.ContractName && c.ContractTypeID != id);
        if (nameTaken)
            return BadRequest(new { message = "A contract type with this name already exists." });

        var draft = new ContractTypeTmp
        {
            ActionType = PendingActionType.UPDATE,
            TargetContractTypeID = id,
            ContractName = request.ContractName,
            ShortName = request.ShortName,
            Description = request.Description,
            AllowDailyCollection = request.AllowDailyCollection,
            AllowWeeklyCollection = request.AllowWeeklyCollection,
            AllowMonthlyCollection = request.AllowMonthlyCollection,
            MinimumCollectionAmount = request.MinimumCollectionAmount,
            MaximumCollectionAmount = request.MaximumCollectionAmount,
            DefaultCollectionAmount = request.DefaultCollectionAmount,
            MinimumOpeningBalance = request.MinimumOpeningBalance,
            MaximumBalance = request.MaximumBalance,
            InterestRate = request.InterestRate,
            ContractDuration = request.ContractDuration,
            DurationUnit = request.DurationUnit,
            PenaltyAmount = request.PenaltyAmount,
            GracePeriod = request.GracePeriod,
            Statut = request.Statut,
            RequestUser = _currentUser.CodeUser!,
            PreviousData = JsonSerializer.Serialize(existing),
            NewData = JsonSerializer.Serialize(request)
        };

        _db.ContractTypeTmps.Add(draft);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Modification soumise pour validation.", pendingId = draft.PendingID });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult> Delete(int id)
    {
        var existing = await _db.ContractTypes.FirstOrDefaultAsync(c => c.ContractTypeID == id)
            ?? throw new KeyNotFoundException("Contract type not found.");

        var isAssigned = await _db.Contracts.AnyAsync(c => c.ContractTypeID == id);
        if (isAssigned)
            return BadRequest(new { message = "This Contract Type is already assigned to one or more clients and cannot be deleted." });

        var draft = new ContractTypeTmp
        {
            ActionType = PendingActionType.DELETE,
            TargetContractTypeID = id,
            RequestUser = _currentUser.CodeUser!,
            PreviousData = JsonSerializer.Serialize(existing)
        };

        _db.ContractTypeTmps.Add(draft);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Suppression soumise pour validation.", pendingId = draft.PendingID });
    }

    [HttpGet("pending")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<IEnumerable<ContractTypeTmp>>> GetPending()
    {
        var pending = await _db.ContractTypeTmps
            .Where(t => t.PendingStatus == PendingStatusEnum.PENDING)
            .OrderBy(t => t.RequestDate)
            .ToListAsync();
        return Ok(pending);
    }

    [HttpPost("pending/{pendingId:int}/approve")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult> Approve(int pendingId)
    {
        var draft = await _db.ContractTypeTmps.FirstOrDefaultAsync(t => t.PendingID == pendingId)
            ?? throw new KeyNotFoundException("Pending record not found.");

        if (draft.PendingStatus != PendingStatusEnum.PENDING)
            return BadRequest(new { message = "This request has already been processed." });

        if (draft.ActionType == PendingActionType.CREATE)
        {
            var nameExists = await _db.ContractTypes.AnyAsync(c => c.ContractName == draft.ContractName);
            if (nameExists)
                return BadRequest(new { message = "A contract type with this name already exists." });

            var count = await _db.ContractTypes.CountAsync();
            var code = $"CT{(count + 1):D3}";

            _db.ContractTypes.Add(new Entities.ContractType
            {
                ContractCode = code,
                ContractName = draft.ContractName!,
                ShortName = draft.ShortName,
                Description = draft.Description,
                AllowDailyCollection = draft.AllowDailyCollection ?? false,
                AllowWeeklyCollection = draft.AllowWeeklyCollection ?? false,
                AllowMonthlyCollection = draft.AllowMonthlyCollection ?? false,
                MinimumCollectionAmount = draft.MinimumCollectionAmount,
                MaximumCollectionAmount = draft.MaximumCollectionAmount,
                DefaultCollectionAmount = draft.DefaultCollectionAmount,
                MinimumOpeningBalance = draft.MinimumOpeningBalance,
                MaximumBalance = draft.MaximumBalance,
                InterestRate = draft.InterestRate,
                ContractDuration = draft.ContractDuration,
                DurationUnit = draft.DurationUnit,
                PenaltyAmount = draft.PenaltyAmount,
                GracePeriod = draft.GracePeriod,
                Statut = "ACTIVE",
                CreatedBy = draft.RequestUser
            });
        }
        else if (draft.ActionType == PendingActionType.UPDATE && draft.TargetContractTypeID.HasValue)
        {
            var existing = await _db.ContractTypes.FirstOrDefaultAsync(c => c.ContractTypeID == draft.TargetContractTypeID.Value)
                ?? throw new KeyNotFoundException("Target contract type no longer exists.");
            if (draft.ContractName != null) existing.ContractName = draft.ContractName;
            existing.ShortName = draft.ShortName;
            existing.Description = draft.Description;
            if (draft.AllowDailyCollection.HasValue) existing.AllowDailyCollection = draft.AllowDailyCollection.Value;
            if (draft.AllowWeeklyCollection.HasValue) existing.AllowWeeklyCollection = draft.AllowWeeklyCollection.Value;
            if (draft.AllowMonthlyCollection.HasValue) existing.AllowMonthlyCollection = draft.AllowMonthlyCollection.Value;
            existing.MinimumCollectionAmount = draft.MinimumCollectionAmount;
            existing.MaximumCollectionAmount = draft.MaximumCollectionAmount;
            existing.DefaultCollectionAmount = draft.DefaultCollectionAmount;
            existing.MinimumOpeningBalance = draft.MinimumOpeningBalance;
            existing.MaximumBalance = draft.MaximumBalance;
            existing.InterestRate = draft.InterestRate;
            existing.ContractDuration = draft.ContractDuration;
            existing.DurationUnit = draft.DurationUnit;
            existing.PenaltyAmount = draft.PenaltyAmount;
            existing.GracePeriod = draft.GracePeriod;
            if (draft.Statut != null) existing.Statut = draft.Statut;
            existing.UpdatedBy = _currentUser.CodeUser;
            existing.UpdatedDate = DateTime.UtcNow;
        }
        else if (draft.ActionType == PendingActionType.DELETE && draft.TargetContractTypeID.HasValue)
        {
            var existing = await _db.ContractTypes.FirstOrDefaultAsync(c => c.ContractTypeID == draft.TargetContractTypeID.Value)
                ?? throw new KeyNotFoundException("Target contract type no longer exists.");

            var isAssigned = await _db.Contracts.AnyAsync(c => c.ContractTypeID == existing.ContractTypeID);
            if (isAssigned)
                return BadRequest(new { message = "This Contract Type is already assigned to one or more clients and cannot be deleted." });

            existing.Statut = "INACTIVE";
            existing.UpdatedBy = _currentUser.CodeUser;
            existing.UpdatedDate = DateTime.UtcNow;
        }

        draft.PendingStatus = PendingStatusEnum.APPROVED;
        draft.ValidationUser = _currentUser.CodeUser;
        draft.ValidationDate = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Type de contrat validé." });
    }

    [HttpPost("pending/{pendingId:int}/reject")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult> Reject(int pendingId, RejectRequest request)
    {
        var draft = await _db.ContractTypeTmps.FirstOrDefaultAsync(t => t.PendingID == pendingId)
            ?? throw new KeyNotFoundException("Pending record not found.");

        draft.PendingStatus = PendingStatusEnum.REJECTED;
        draft.ValidationUser = _currentUser.CodeUser;
        draft.ValidationDate = DateTime.UtcNow;
        draft.RejectionReason = request.Reason;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Type de contrat rejeté." });
    }
}
