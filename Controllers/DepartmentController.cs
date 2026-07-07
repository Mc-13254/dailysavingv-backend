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
public class DepartmentController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DepartmentController(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    private static DepartmentDto ToDto(Entities.Department d) => new(
        d.DepartmentID, d.DepartmentCode, d.DepartmentName, d.ShortName, d.Description,
        d.CodeIMF, d.IMF?.Libelle, d.AgenceID, d.Agence?.Nom,
        d.ManagerId, d.Manager?.Username, d.Statut,
        d.CreatedBy, d.CreatedDate, d.UpdatedBy, d.UpdatedDate
    );

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DepartmentDto>>> GetAll()
    {
        var result = await _db.Departments
            .Include(d => d.IMF).Include(d => d.Agence).Include(d => d.Manager)
            .ToListAsync();
        return Ok(result.Select(ToDto));
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult> Create(CreateDepartmentRequest request)
    {
        var nameExists = await _db.Departments.AnyAsync(d => d.DepartmentName == request.DepartmentName && d.AgenceID == request.AgenceID);
        if (nameExists)
            return BadRequest(new { message = "A department with this name already exists in this agency." });

        var draft = new DepartmentTmp
        {
            ActionType = PendingActionType.CREATE,
            DepartmentName = request.DepartmentName,
            ShortName = request.ShortName,
            Description = request.Description,
            CodeIMF = request.CodeIMF,
            AgenceID = request.AgenceID,
            ManagerId = request.ManagerId,
            Statut = "ACTIVE",
            RequestUser = _currentUser.CodeUser!,
            NewData = JsonSerializer.Serialize(request)
        };

        _db.DepartmentTmps.Add(draft);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Département soumis pour validation.", pendingId = draft.PendingID });
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult> Update(int id, UpdateDepartmentRequest request)
    {
        var existing = await _db.Departments.FirstOrDefaultAsync(d => d.DepartmentID == id)
            ?? throw new KeyNotFoundException("Department not found.");

        var nameTaken = await _db.Departments.AnyAsync(d => d.DepartmentName == request.DepartmentName && d.AgenceID == request.AgenceID && d.DepartmentID != id);
        if (nameTaken)
            return BadRequest(new { message = "A department with this name already exists in this agency." });

        var draft = new DepartmentTmp
        {
            ActionType = PendingActionType.UPDATE,
            TargetDepartmentID = id,
            DepartmentName = request.DepartmentName,
            ShortName = request.ShortName,
            Description = request.Description,
            AgenceID = request.AgenceID,
            ManagerId = request.ManagerId,
            Statut = request.Statut,
            RequestUser = _currentUser.CodeUser!,
            PreviousData = JsonSerializer.Serialize(existing),
            NewData = JsonSerializer.Serialize(request)
        };

        _db.DepartmentTmps.Add(draft);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Modification soumise pour validation.", pendingId = draft.PendingID });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult> Delete(int id)
    {
        var existing = await _db.Departments.FirstOrDefaultAsync(d => d.DepartmentID == id)
            ?? throw new KeyNotFoundException("Department not found.");

        var hasUsers = await _db.Users.IgnoreQueryFilters().AnyAsync(u => u.DepartmentID == id);
        if (hasUsers)
            return BadRequest(new { message = "This Department contains users and cannot be deleted." });

        var draft = new DepartmentTmp
        {
            ActionType = PendingActionType.DELETE,
            TargetDepartmentID = id,
            RequestUser = _currentUser.CodeUser!,
            PreviousData = JsonSerializer.Serialize(existing)
        };

        _db.DepartmentTmps.Add(draft);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Suppression soumise pour validation.", pendingId = draft.PendingID });
    }

    [HttpGet("pending")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<IEnumerable<DepartmentTmp>>> GetPending()
    {
        var pending = await _db.DepartmentTmps
            .Where(t => t.PendingStatus == PendingStatusEnum.PENDING)
            .OrderBy(t => t.RequestDate)
            .ToListAsync();
        return Ok(pending);
    }

    [HttpPost("pending/{pendingId:int}/approve")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult> Approve(int pendingId)
    {
        var draft = await _db.DepartmentTmps.FirstOrDefaultAsync(t => t.PendingID == pendingId)
            ?? throw new KeyNotFoundException("Pending record not found.");

        if (draft.PendingStatus != PendingStatusEnum.PENDING)
            return BadRequest(new { message = "This request has already been processed." });

        if (draft.ActionType == PendingActionType.CREATE)
        {
            if (string.IsNullOrWhiteSpace(draft.CodeIMF) || !await _db.IMFs.AnyAsync(i => i.CodeIMF == draft.CodeIMF))
                return BadRequest(new { message = "Impossible de valider : l'IMF référencée n'existe pas." });
            if (!draft.AgenceID.HasValue || !await _db.Agences.AnyAsync(a => a.AgenceID == draft.AgenceID.Value))
                return BadRequest(new { message = "Impossible de valider : l'agence référencée n'existe pas." });

            var nameExists = await _db.Departments.AnyAsync(d => d.DepartmentName == draft.DepartmentName && d.AgenceID == draft.AgenceID);
            if (nameExists)
                return BadRequest(new { message = "A department with this name already exists in this agency." });

            var count = await _db.Departments.CountAsync();
            var code = $"DEP{(count + 1):D3}";

            _db.Departments.Add(new Entities.Department
            {
                DepartmentCode = code,
                DepartmentName = draft.DepartmentName!,
                ShortName = draft.ShortName,
                Description = draft.Description,
                CodeIMF = draft.CodeIMF!,
                AgenceID = draft.AgenceID!.Value,
                ManagerId = draft.ManagerId,
                Statut = "ACTIVE",
                CreatedBy = draft.RequestUser
            });
        }
        else if (draft.ActionType == PendingActionType.UPDATE && draft.TargetDepartmentID.HasValue)
        {
            var existing = await _db.Departments.FirstOrDefaultAsync(d => d.DepartmentID == draft.TargetDepartmentID.Value)
                ?? throw new KeyNotFoundException("Target department no longer exists.");
            if (draft.DepartmentName != null) existing.DepartmentName = draft.DepartmentName;
            existing.ShortName = draft.ShortName;
            existing.Description = draft.Description;
            if (draft.AgenceID.HasValue) existing.AgenceID = draft.AgenceID.Value;
            existing.ManagerId = draft.ManagerId;
            if (draft.Statut != null) existing.Statut = draft.Statut;
            existing.UpdatedBy = _currentUser.CodeUser;
            existing.UpdatedDate = DateTime.UtcNow;
        }
        else if (draft.ActionType == PendingActionType.DELETE && draft.TargetDepartmentID.HasValue)
        {
            var existing = await _db.Departments.FirstOrDefaultAsync(d => d.DepartmentID == draft.TargetDepartmentID.Value)
                ?? throw new KeyNotFoundException("Target department no longer exists.");

            var hasUsers = await _db.Users.IgnoreQueryFilters().AnyAsync(u => u.DepartmentID == existing.DepartmentID);
            if (hasUsers)
                return BadRequest(new { message = "This Department contains users and cannot be deleted." });

            existing.Statut = "INACTIVE";
            existing.UpdatedBy = _currentUser.CodeUser;
            existing.UpdatedDate = DateTime.UtcNow;
        }

        draft.PendingStatus = PendingStatusEnum.APPROVED;
        draft.ValidationUser = _currentUser.CodeUser;
        draft.ValidationDate = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Département validé." });
    }

    [HttpPost("pending/{pendingId:int}/reject")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult> Reject(int pendingId, RejectRequest request)
    {
        var draft = await _db.DepartmentTmps.FirstOrDefaultAsync(t => t.PendingID == pendingId)
            ?? throw new KeyNotFoundException("Pending record not found.");

        draft.PendingStatus = PendingStatusEnum.REJECTED;
        draft.ValidationUser = _currentUser.CodeUser;
        draft.ValidationDate = DateTime.UtcNow;
        draft.RejectionReason = request.Reason;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Département rejeté." });
    }
}
