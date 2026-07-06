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
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UsersController(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    // Auto agency-scoped via the Users global query filter in AppDbContext
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserFullDto>>> GetAll()
    {
        var result = await _db.Users.Include(u => u.Role)
            .Select(u => new UserFullDto(u.CodeUser, u.Username, u.Email, u.Phone, u.Adresse, u.CNI, u.Role!.Code, u.AgenceID, u.ValidationStatus, u.Statut))
            .ToListAsync();
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = "SupervisorOrAdmin")]
    public async Task<ActionResult> Create(CreateUserRequest request)
    {
        // Hash immediately; the plain password is never persisted anywhere,
        // including the NewData audit snapshot below.
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        var draft = new UsersTmp
        {
            ActionType = PendingActionType.CREATE,
            Username = request.Username,
            PasswordHash = passwordHash,
            Email = request.Email,
            Phone = request.Phone,
            Adresse = request.Adresse,
            CNI = request.CNI,
            RoleID = request.RoleID,
            AgenceID = _currentUser.AgenceID,
            Statut = "ACTIVE",
            RequestUser = _currentUser.CodeUser!,
            NewData = JsonSerializer.Serialize(new { request.Username, request.Email, request.Phone, request.RoleID })
        };

        _db.UsersTmps.Add(draft);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Utilisateur soumis pour validation.", pendingId = draft.PendingID });
    }

    [HttpGet("pending")]
    [Authorize(Policy = "SupervisorOrAdmin")]
    public async Task<ActionResult<IEnumerable<object>>> GetPending()
    {
        var pending = await _db.UsersTmps
            .Where(t => t.PendingStatus == PendingStatusEnum.PENDING
                        && (_currentUser.IsHeadOffice || t.AgenceID == _currentUser.AgenceID))
            .OrderBy(t => t.RequestDate)
            // Never expose PasswordHash, even to Supervisors reviewing the request
            .Select(t => new
            {
                t.PendingID, t.ActionType, t.Username, t.Email, t.Phone,
                t.RequestUser, t.RequestDate, t.PendingStatus
            })
            .ToListAsync();
        return Ok(pending);
    }

    [HttpPut("{codeUser}")]
    [Authorize(Policy = "SupervisorOrAdmin")]
    public async Task<ActionResult> Update(string codeUser, UpdateUserRequest request)
    {
        var existing = await _db.Users.FirstOrDefaultAsync(u => u.CodeUser == codeUser)
            ?? throw new KeyNotFoundException("User not found in your agency.");

        var draft = new UsersTmp
        {
            ActionType = PendingActionType.UPDATE,
            TargetCodeUser = codeUser,
            Email = request.Email,
            Phone = request.Phone,
            Adresse = request.Adresse,
            CNI = request.CNI,
            RoleID = request.RoleID,
            Statut = request.Statut,
            PasswordHash = string.IsNullOrWhiteSpace(request.NewPassword) ? null : BCrypt.Net.BCrypt.HashPassword(request.NewPassword),
            AgenceID = existing.AgenceID,
            RequestUser = _currentUser.CodeUser!,
            PreviousData = JsonSerializer.Serialize(new { existing.Email, existing.Phone, existing.RoleID, existing.Statut }),
            NewData = JsonSerializer.Serialize(new { request.Email, request.Phone, request.RoleID, request.Statut })
        };

        _db.UsersTmps.Add(draft);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Modification soumise pour validation.", pendingId = draft.PendingID });
    }

    [HttpDelete("{codeUser}")]
    [Authorize(Policy = "SupervisorOrAdmin")]
    public async Task<ActionResult> Delete(string codeUser)
    {
        var existing = await _db.Users.FirstOrDefaultAsync(u => u.CodeUser == codeUser)
            ?? throw new KeyNotFoundException("User not found in your agency.");

        var draft = new UsersTmp
        {
            ActionType = PendingActionType.DELETE,
            TargetCodeUser = codeUser,
            AgenceID = existing.AgenceID,
            RequestUser = _currentUser.CodeUser!,
            PreviousData = JsonSerializer.Serialize(new { existing.Username, existing.Email })
        };

        _db.UsersTmps.Add(draft);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Suppression soumise pour validation.", pendingId = draft.PendingID });
    }

    [HttpPost("pending/{pendingId:int}/approve")]
    [Authorize(Policy = "SupervisorOrAdmin")]
    public async Task<ActionResult> Approve(int pendingId)
    {
        var draft = await _db.UsersTmps.FirstOrDefaultAsync(t => t.PendingID == pendingId)
            ?? throw new KeyNotFoundException("Pending record not found.");

        if (draft.PendingStatus != PendingStatusEnum.PENDING)
            return BadRequest(new { message = "This request has already been processed." });

        if (draft.ActionType == PendingActionType.CREATE)
        {
            var count = await _db.Users.IgnoreQueryFilters().CountAsync();
            var newId = $"U-{(count + 1):D3}";

            _db.Users.Add(new Entities.Users
            {
                CodeUser = newId,
                Username = draft.Username!,
                PasswordHash = draft.PasswordHash!,
                Email = draft.Email,
                Phone = draft.Phone,
                Adresse = draft.Adresse,
                CNI = draft.CNI,
                RoleID = draft.RoleID!.Value,
                AgenceID = draft.AgenceID,
                Statut = draft.Statut ?? "ACTIVE",
                ValidationStatus = "VALIDATED",
                CreatedBy = draft.RequestUser
            });
        }
        else if (draft.ActionType == PendingActionType.UPDATE && draft.TargetCodeUser != null)
        {
            var existing = await _db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.CodeUser == draft.TargetCodeUser)
                ?? throw new KeyNotFoundException("Target user no longer exists.");
            if (draft.Email != null) existing.Email = draft.Email;
            if (draft.Phone != null) existing.Phone = draft.Phone;
            if (draft.Adresse != null) existing.Adresse = draft.Adresse;
            if (draft.CNI != null) existing.CNI = draft.CNI;
            if (draft.RoleID.HasValue) existing.RoleID = draft.RoleID.Value;
            if (draft.Statut != null) existing.Statut = draft.Statut;
            if (draft.PasswordHash != null) existing.PasswordHash = draft.PasswordHash;
        }
        else if (draft.ActionType == PendingActionType.DELETE && draft.TargetCodeUser != null)
        {
            // Soft-delete: Collector.CodeUser references Users, so we deactivate.
            var existing = await _db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.CodeUser == draft.TargetCodeUser)
                ?? throw new KeyNotFoundException("Target user no longer exists.");
            existing.Statut = "INACTIVE";
        }

        draft.PendingStatus = PendingStatusEnum.APPROVED;
        draft.ValidationUser = _currentUser.CodeUser;
        draft.ValidationDate = DateTime.UtcNow;
        draft.PasswordHash = null; // clear once applied - no longer needed in the pending record

        await _db.SaveChangesAsync();
        return Ok(new { message = "Utilisateur validé et créé en production." });
    }

    [HttpPost("pending/{pendingId:int}/reject")]
    [Authorize(Policy = "SupervisorOrAdmin")]
    public async Task<ActionResult> Reject(int pendingId, RejectRequest request)
    {
        var draft = await _db.UsersTmps.FirstOrDefaultAsync(t => t.PendingID == pendingId)
            ?? throw new KeyNotFoundException("Pending record not found.");

        draft.PendingStatus = PendingStatusEnum.REJECTED;
        draft.ValidationUser = _currentUser.CodeUser;
        draft.ValidationDate = DateTime.UtcNow;
        draft.RejectionReason = request.Reason;
        draft.PasswordHash = null;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Utilisateur rejeté." });
    }
}
