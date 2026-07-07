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
public class RolesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public RolesController(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<RoleDto>>> GetAll()
    {
        var roles = await _db.Roles.ToListAsync();
        var userCounts = await _db.Users.IgnoreQueryFilters()
            .GroupBy(u => u.RoleID)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Key, g => g.Count);

        var result = roles.Select(r => new RoleDto(
            r.RoleID, r.Code, r.Libelle, r.Description, r.Statut,
            userCounts.TryGetValue(r.RoleID, out var c) ? c : 0,
            r.CreatedBy, r.CreatedDate, r.UpdatedBy, r.UpdatedDate
        ));
        return Ok(result);
    }

    // Used to populate the Role dropdown when creating a User — only active roles.
    [HttpGet("active")]
    public async Task<ActionResult> GetActive()
    {
        var roles = await _db.Roles.Where(r => r.Statut)
            .Select(r => new { r.RoleID, r.Code, r.Libelle })
            .ToListAsync();
        return Ok(roles);
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult> Create(CreateRoleRequest request)
    {
        var nameExists = await _db.Roles.AnyAsync(r => r.Libelle == request.Libelle)
            || await _db.RoleTmps.AnyAsync(t => t.Libelle == request.Libelle && t.PendingStatus == PendingStatusEnum.PENDING);
        if (nameExists)
            return BadRequest(new { message = "Role already exists." });

        var draft = new RoleTmp
        {
            ActionType = PendingActionType.CREATE,
            Libelle = request.Libelle,
            Description = request.Description,
            Statut = true,
            RequestUser = _currentUser.CodeUser!,
            NewData = JsonSerializer.Serialize(request)
        };

        _db.RoleTmps.Add(draft);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Rôle soumis pour validation.", pendingId = draft.PendingID });
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult> Update(int id, UpdateRoleRequest request)
    {
        var existing = await _db.Roles.FirstOrDefaultAsync(r => r.RoleID == id)
            ?? throw new KeyNotFoundException("Role not found.");

        var nameTaken = await _db.Roles.AnyAsync(r => r.Libelle == request.Libelle && r.RoleID != id);
        if (nameTaken)
            return BadRequest(new { message = "Role already exists." });

        var draft = new RoleTmp
        {
            ActionType = PendingActionType.UPDATE,
            TargetRoleID = id,
            Libelle = request.Libelle,
            Description = request.Description,
            Statut = request.Statut,
            RequestUser = _currentUser.CodeUser!,
            PreviousData = JsonSerializer.Serialize(existing),
            NewData = JsonSerializer.Serialize(request)
        };

        _db.RoleTmps.Add(draft);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Modification soumise pour validation.", pendingId = draft.PendingID });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult> Delete(int id)
    {
        var existing = await _db.Roles.FirstOrDefaultAsync(r => r.RoleID == id)
            ?? throw new KeyNotFoundException("Role not found.");

        var isUsed = await _db.Users.IgnoreQueryFilters().AnyAsync(u => u.RoleID == id);
        if (isUsed)
            return BadRequest(new { message = "This role is assigned to one or more users. Deletion is not allowed." });

        var draft = new RoleTmp
        {
            ActionType = PendingActionType.DELETE,
            TargetRoleID = id,
            RequestUser = _currentUser.CodeUser!,
            PreviousData = JsonSerializer.Serialize(existing)
        };

        _db.RoleTmps.Add(draft);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Suppression soumise pour validation.", pendingId = draft.PendingID });
    }

    [HttpGet("pending")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<IEnumerable<RoleTmp>>> GetPending()
    {
        var pending = await _db.RoleTmps
            .Where(t => t.PendingStatus == PendingStatusEnum.PENDING)
            .OrderBy(t => t.RequestDate)
            .ToListAsync();
        return Ok(pending);
    }

    [HttpPost("pending/{pendingId:int}/approve")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult> Approve(int pendingId)
    {
        var draft = await _db.RoleTmps.FirstOrDefaultAsync(t => t.PendingID == pendingId)
            ?? throw new KeyNotFoundException("Pending record not found.");

        if (draft.PendingStatus != PendingStatusEnum.PENDING)
            return BadRequest(new { message = "This request has already been processed." });

        if (draft.ActionType == PendingActionType.CREATE)
        {
            var nameExists = await _db.Roles.AnyAsync(r => r.Libelle == draft.Libelle);
            if (nameExists)
                return BadRequest(new { message = "Role already exists." });

            var count = await _db.Roles.CountAsync();
            var code = $"ROL{(count + 1):D3}";

            _db.Roles.Add(new Entities.Role
            {
                Code = code,
                Libelle = draft.Libelle!,
                Description = draft.Description,
                Statut = true,
                CreatedBy = draft.RequestUser
            });
        }
        else if (draft.ActionType == PendingActionType.UPDATE && draft.TargetRoleID.HasValue)
        {
            var existing = await _db.Roles.FirstOrDefaultAsync(r => r.RoleID == draft.TargetRoleID.Value)
                ?? throw new KeyNotFoundException("Target role no longer exists.");
            if (draft.Libelle != null) existing.Libelle = draft.Libelle;
            existing.Description = draft.Description;
            if (draft.Statut.HasValue) existing.Statut = draft.Statut.Value;
            existing.UpdatedBy = _currentUser.CodeUser;
            existing.UpdatedDate = DateTime.UtcNow;
        }
        else if (draft.ActionType == PendingActionType.DELETE && draft.TargetRoleID.HasValue)
        {
            var existing = await _db.Roles.FirstOrDefaultAsync(r => r.RoleID == draft.TargetRoleID.Value)
                ?? throw new KeyNotFoundException("Target role no longer exists.");

            var isUsed = await _db.Users.IgnoreQueryFilters().AnyAsync(u => u.RoleID == existing.RoleID);
            if (isUsed)
                return BadRequest(new { message = "This role is assigned to one or more users. Deletion is not allowed." });

            existing.Statut = false; // soft delete
            existing.UpdatedBy = _currentUser.CodeUser;
            existing.UpdatedDate = DateTime.UtcNow;
        }

        draft.PendingStatus = PendingStatusEnum.APPROVED;
        draft.ValidationUser = _currentUser.CodeUser;
        draft.ValidationDate = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Rôle validé." });
    }

    [HttpPost("pending/{pendingId:int}/reject")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult> Reject(int pendingId, RejectRequest request)
    {
        var draft = await _db.RoleTmps.FirstOrDefaultAsync(t => t.PendingID == pendingId)
            ?? throw new KeyNotFoundException("Pending record not found.");

        draft.PendingStatus = PendingStatusEnum.REJECTED;
        draft.ValidationUser = _currentUser.CodeUser;
        draft.ValidationDate = DateTime.UtcNow;
        draft.RejectionReason = request.Reason;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Rôle rejeté." });
    }
}
