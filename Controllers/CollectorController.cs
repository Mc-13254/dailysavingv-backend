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
public class CollectorController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CollectorController(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    private static CollectorDto ToDto(Collector c) => new(
        c.CollectorID, c.CodeUser, c.Name, c.Surname, c.PhoneNumber,
        c.AgenceID, c.Agence?.Nom, c.DepartmentID, c.Department?.DepartmentName,
        c.IsActive, c.CDETAT, c.DateEmploi, c.ContactType, c.CodeTerminal,
        c.Plafond, c.Caution,
        c.ContractID, c.Contract?.ContractName, c.CommissionTypeID, c.CommissionType?.Name,
        c.CommissionRangeID, c.SupervisorId, c.Supervisor?.Username,
        c.CollectMonth, c.CollectDay, c.RetraitMonth, c.RetraitDay,
        c.UserCreate, c.CreateDate, c.UserValidation, c.DateValidation,
        c.LastUserModif, c.DateModification,
        c.LastUserSupervise, c.LastDateSupervise
    );

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CollectorDto>>> GetAll([FromQuery] string? search)
    {
        var query = _db.Collectors
            .Include(c => c.Agence).Include(c => c.Department)
            .Include(c => c.Contract).Include(c => c.CommissionType).Include(c => c.Supervisor)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c =>
                c.Name.Contains(search) || c.CollectorID.Contains(search) ||
                (c.PhoneNumber != null && c.PhoneNumber.Contains(search)) ||
                (c.Agence != null && c.Agence.Nom.Contains(search)));

        var result = await query.ToListAsync();
        return Ok(result.Select(ToDto));
    }

    // Users eligible to become a Collector: Active, Role = Collector, and not
    // already linked to an existing Collector row.
    [HttpGet("available-users")]
    public async Task<ActionResult<IEnumerable<AvailableUserDto>>> GetAvailableUsers()
    {
        var assignedCodes = await _db.Collectors.IgnoreQueryFilters().Select(c => c.CodeUser).ToListAsync();

        var users = await _db.Users.Include(u => u.Role).Include(u => u.Agence)
            .Where(u => u.Statut == "ACTIVE" && u.Role!.Code == "COLLECTOR" && !assignedCodes.Contains(u.CodeUser))
            .Select(u => new AvailableUserDto(
                u.CodeUser, u.Photo, u.FirstName, u.LastName,
                u.AgenceID, u.Agence!.Nom, u.DepartmentID, u.DepartmentRef!.DepartmentName, u.Phone, u.Email))
            .ToListAsync();

        return Ok(users);
    }

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

    // POST api/collector -> creates a PENDING record; User/Agency/Department are inherited
    // from the selected User and cannot be chosen directly.
    [HttpPost]
    public async Task<ActionResult> Create(CreateCollectorRequest request)
    {
        var user = await _db.Users.IgnoreQueryFilters().Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.CodeUser == request.CodeUser)
            ?? throw new KeyNotFoundException("User not found.");

        if (user.Statut != "ACTIVE")
            return BadRequest(new { message = "Selected user is not active." });
        if (user.Role?.Code != "COLLECTOR")
            return BadRequest(new { message = "Selected user does not have the Collector role." });

        var alreadyAssigned = await _db.Collectors.IgnoreQueryFilters().AnyAsync(c => c.CodeUser == request.CodeUser);
        if (alreadyAssigned)
            return BadRequest(new { message = "This user is already assigned as a Collector." });

        if (!user.AgenceID.HasValue)
            return BadRequest(new { message = "Selected user has no assigned agency." });

        var draft = new CollectorTMP
        {
            ActionType = PendingActionType.CREATE,
            CodeUser = request.CodeUser,
            Name = user.FirstName,
            Surname = user.LastName,
            PhoneNumber = user.Phone,
            AgenceID = user.AgenceID.Value,
            DepartmentID = user.DepartmentID,
            ZoneCollecteID = request.ZoneCollecteID,
            IsActive = true,
            CDETAT = "ACTIVE",
            DateEmploi = request.DateEmploi,
            ContactType = request.ContactType,
            CodeTerminal = request.CodeTerminal,
            Plafond = request.Plafond ?? 0,
            Caution = request.Caution,
            ContractID = request.ContractID,
            CommissionTypeID = request.CommissionTypeID,
            CommissionRangeID = request.CommissionRangeID,
            SupervisorId = request.SupervisorId,
            CollectMonth = request.CollectMonth,
            CollectDay = request.CollectDay,
            RetraitMonth = request.RetraitMonth,
            RetraitDay = request.RetraitDay,
            RequestUser = _currentUser.CodeUser!,
            NewData = JsonSerializer.Serialize(request)
        };

        _db.CollectorTMPs.Add(draft);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Collecteur soumis pour validation (Maker-Checker).", pendingId = draft.PendingID });
    }

    // PUT api/collector/{id} -> User/Agency/Department cannot be changed.
    [HttpPut("{id}")]
    public async Task<ActionResult> Update(string id, UpdateCollectorRequest request)
    {
        var existing = await _db.Collectors.FirstOrDefaultAsync(c => c.CollectorID == id)
            ?? throw new KeyNotFoundException("Collector not found in your agency.");

        var draft = new CollectorTMP
        {
            ActionType = PendingActionType.UPDATE,
            TargetCollectorID = id,
            ContactType = request.ContactType,
            CommissionTypeID = request.CommissionTypeID,
            CommissionRangeID = request.CommissionRangeID,
            ContractID = request.ContractID,
            Plafond = request.Plafond,
            CollectMonth = request.CollectMonth,
            CollectDay = request.CollectDay,
            RetraitMonth = request.RetraitMonth,
            RetraitDay = request.RetraitDay,
            SupervisorId = request.SupervisorId,
            ZoneCollecteID = request.ZoneCollecteID,
            CDETAT = request.CDETAT,
            IsActive = request.CDETAT == "ACTIVE",
            AgenceID = existing.AgenceID,
            RequestUser = _currentUser.CodeUser!,
            PreviousData = JsonSerializer.Serialize(existing),
            NewData = JsonSerializer.Serialize(request)
        };

        _db.CollectorTMPs.Add(draft);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Modification soumise pour validation.", pendingId = draft.PendingID });
    }

    // DELETE api/collector/{id} -> blocked if the Collector already has Clients assigned.
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(string id)
    {
        var existing = await _db.Collectors.FirstOrDefaultAsync(c => c.CollectorID == id)
            ?? throw new KeyNotFoundException("Collector not found in your agency.");

        var hasClients = await _db.Clients.IgnoreQueryFilters().AnyAsync(cl => cl.CollectorID == id);
        if (hasClients)
            return BadRequest(new { message = "This Collector is assigned to one or more clients and cannot be deleted." });

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
                var alreadyAssigned = await _db.Collectors.IgnoreQueryFilters().AnyAsync(c => c.CodeUser == draft.CodeUser);
                if (alreadyAssigned)
                    return BadRequest(new { message = "This user is already assigned as a Collector." });

                var newId = await GenerateNextCollectorId();
                _db.Collectors.Add(new Collector
                {
                    CollectorID = newId,
                    CodeUser = draft.CodeUser!,
                    Name = draft.Name!,
                    Surname = draft.Surname,
                    PhoneNumber = draft.PhoneNumber,
                    AgenceID = draft.AgenceID!.Value,
                    DepartmentID = draft.DepartmentID,
                    ZoneCollecteID = draft.ZoneCollecteID,
                    IsActive = draft.IsActive ?? true,
                    CDETAT = draft.CDETAT ?? "ACTIVE",
                    DateEmploi = draft.DateEmploi,
                    ContactType = draft.ContactType,
                    CodeTerminal = draft.CodeTerminal,
                    Plafond = draft.Plafond ?? 0,
                    Caution = draft.Caution,
                    ContractID = draft.ContractID,
                    CommissionTypeID = draft.CommissionTypeID,
                    CommissionRangeID = draft.CommissionRangeID,
                    SupervisorId = draft.SupervisorId,
                    CollectMonth = draft.CollectMonth,
                    CollectDay = draft.CollectDay,
                    RetraitMonth = draft.RetraitMonth,
                    RetraitDay = draft.RetraitDay,
                    UserCreate = draft.RequestUser,
                    UserValidation = _currentUser.CodeUser,
                    DateValidation = DateTime.UtcNow
                });
                break;

            case PendingActionType.UPDATE:
                var toUpdate = await _db.Collectors.FirstOrDefaultAsync(c => c.CollectorID == draft.TargetCollectorID)
                    ?? throw new KeyNotFoundException("Target collector no longer exists.");
                if (draft.ContactType != null) toUpdate.ContactType = draft.ContactType;
                if (draft.CommissionTypeID.HasValue) toUpdate.CommissionTypeID = draft.CommissionTypeID;
                if (draft.CommissionRangeID.HasValue) toUpdate.CommissionRangeID = draft.CommissionRangeID;
                if (draft.ContractID.HasValue) toUpdate.ContractID = draft.ContractID;
                if (draft.Plafond.HasValue) toUpdate.Plafond = draft.Plafond.Value;
                if (draft.CollectMonth.HasValue) toUpdate.CollectMonth = draft.CollectMonth;
                if (draft.CollectDay.HasValue) toUpdate.CollectDay = draft.CollectDay;
                if (draft.RetraitMonth.HasValue) toUpdate.RetraitMonth = draft.RetraitMonth;
                if (draft.RetraitDay.HasValue) toUpdate.RetraitDay = draft.RetraitDay;
                if (draft.SupervisorId != null) toUpdate.SupervisorId = draft.SupervisorId;
                if (draft.ZoneCollecteID.HasValue) toUpdate.ZoneCollecteID = draft.ZoneCollecteID;
                if (draft.CDETAT != null) { toUpdate.CDETAT = draft.CDETAT; toUpdate.IsActive = draft.CDETAT == "ACTIVE"; }
                toUpdate.LastUserModif = _currentUser.CodeUser;
                toUpdate.DateModification = DateTime.UtcNow;
                break;

            case PendingActionType.DELETE:
                var toDelete = await _db.Collectors.FirstOrDefaultAsync(c => c.CollectorID == draft.TargetCollectorID)
                    ?? throw new KeyNotFoundException("Target collector no longer exists.");

                var hasClients = await _db.Clients.IgnoreQueryFilters().AnyAsync(cl => cl.CollectorID == toDelete.CollectorID);
                if (hasClients)
                    return BadRequest(new { message = "This Collector is assigned to one or more clients and cannot be deleted." });

                toDelete.CDETAT = "INACTIVE";
                toDelete.IsActive = false;
                toDelete.LastUserModif = _currentUser.CodeUser;
                toDelete.DateModification = DateTime.UtcNow;
                break;
        }

        draft.PendingStatus = PendingStatusEnum.APPROVED;
        draft.ValidationUser = _currentUser.CodeUser;
        draft.ValidationDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Validé et appliqué en production." });
    }

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
