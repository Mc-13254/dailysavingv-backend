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

    private static UserFullDto ToDto(Entities.Users u) => new(
        u.CodeUser, u.Username, u.Email, u.Phone, u.Adresse, u.CNI, u.Photo,
        u.RoleID, u.Role?.Code ?? "", u.Role?.Libelle,
        u.FirstName, u.LastName, u.TypeUser,
        u.AgenceID, u.Agence?.Nom, u.Agence?.CodeIMF,
        u.DebitMax, u.CreditMax, u.ValidationMax, u.PlafondCollect, u.Caution,
        u.Signe, u.ValidationStatus, u.Statut,
        u.CreatedBy, u.CreatedDate, u.LastLogin,
        u.UserValidation, u.DateValidation,
        u.LastUserModif, u.DateModification,
        u.LastDateSupervise, u.LastUserSupervise
    );

    // Auto agency-scoped via the Users global query filter in AppDbContext
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserFullDto>>> GetAll()
    {
        var result = await _db.Users.Include(u => u.Role).Include(u => u.Agence).ToListAsync();
        return Ok(result.Select(ToDto));
    }

    [HttpPost]
    [Authorize(Policy = "SupervisorOrAdmin")]
    public async Task<ActionResult> Create(CreateUserRequest request)
    {
        if (request.Password != request.ConfirmPassword)
            return BadRequest(new { message = "Le mot de passe et sa confirmation ne correspondent pas." });

        var usernameTaken = await _db.Users.IgnoreQueryFilters().AnyAsync(u => u.Username == request.Username);
        if (usernameTaken)
            return BadRequest(new { message = "Ce nom d'utilisateur existe déjà." });

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var emailTaken = await _db.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == request.Email);
            if (emailTaken)
                return BadRequest(new { message = "Cet email est déjà utilisé." });
        }

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
            Photo = request.Photo,
            Signe = request.Signe,
            RoleID = request.RoleID,
            AgenceID = request.AgenceID ?? _currentUser.AgenceID,
            FirstName = request.FirstName,
            LastName = request.LastName,
            TypeUser = request.TypeUser,
            DebitMax = request.DebitMax,
            CreditMax = request.CreditMax,
            ValidationMax = request.ValidationMax,
            PlafondCollect = request.PlafondCollect,
            Caution = request.Caution,
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
        var existing = await _db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.CodeUser == codeUser)
            ?? throw new KeyNotFoundException("User not found.");

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var emailTaken = await _db.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == request.Email && u.CodeUser != codeUser);
            if (emailTaken)
                return BadRequest(new { message = "Cet email est déjà utilisé par un autre utilisateur." });
        }

        var draft = new UsersTmp
        {
            ActionType = PendingActionType.UPDATE,
            TargetCodeUser = codeUser,
            Email = request.Email,
            Phone = request.Phone,
            Adresse = request.Adresse,
            CNI = request.CNI,
            Photo = request.Photo,
            Signe = request.Signe,
            RoleID = request.RoleID,
            AgenceID = request.AgenceID ?? existing.AgenceID,
            FirstName = request.FirstName,
            LastName = request.LastName,
            TypeUser = request.TypeUser,
            DebitMax = request.DebitMax,
            CreditMax = request.CreditMax,
            ValidationMax = request.ValidationMax,
            PlafondCollect = request.PlafondCollect,
            Caution = request.Caution,
            Statut = request.Statut,
            PasswordHash = string.IsNullOrWhiteSpace(request.NewPassword) ? null : BCrypt.Net.BCrypt.HashPassword(request.NewPassword),
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
        var existing = await _db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.CodeUser == codeUser)
            ?? throw new KeyNotFoundException("User not found.");

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
            var usernameTaken = await _db.Users.IgnoreQueryFilters().AnyAsync(u => u.Username == draft.Username);
            if (usernameTaken)
                return BadRequest(new { message = "Ce nom d'utilisateur existe déjà — impossible de valider." });

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
                Photo = draft.Photo,
                Signe = draft.Signe,
                RoleID = draft.RoleID!.Value,
                AgenceID = draft.AgenceID,
                FirstName = draft.FirstName,
                LastName = draft.LastName,
                TypeUser = draft.TypeUser,
                DebitMax = draft.DebitMax,
                CreditMax = draft.CreditMax,
                ValidationMax = draft.ValidationMax,
                PlafondCollect = draft.PlafondCollect,
                Caution = draft.Caution,
                Statut = draft.Statut ?? "ACTIVE",
                ValidationStatus = "VALIDATED",
                CreatedBy = draft.RequestUser,
                UserValidation = _currentUser.CodeUser,
                DateValidation = DateTime.UtcNow
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
            if (draft.Photo != null) existing.Photo = draft.Photo;
            if (draft.Signe != null) existing.Signe = draft.Signe;
            if (draft.RoleID.HasValue) existing.RoleID = draft.RoleID.Value;
            if (draft.AgenceID.HasValue) existing.AgenceID = draft.AgenceID;
            if (draft.FirstName != null) existing.FirstName = draft.FirstName;
            if (draft.LastName != null) existing.LastName = draft.LastName;
            if (draft.TypeUser != null) existing.TypeUser = draft.TypeUser;
            if (draft.DebitMax.HasValue) existing.DebitMax = draft.DebitMax;
            if (draft.CreditMax.HasValue) existing.CreditMax = draft.CreditMax;
            if (draft.ValidationMax.HasValue) existing.ValidationMax = draft.ValidationMax;
            if (draft.PlafondCollect.HasValue) existing.PlafondCollect = draft.PlafondCollect;
            if (draft.Caution.HasValue) existing.Caution = draft.Caution;
            if (draft.Statut != null) existing.Statut = draft.Statut;
            if (draft.PasswordHash != null) existing.PasswordHash = draft.PasswordHash;
            existing.LastUserModif = _currentUser.CodeUser;
            existing.DateModification = DateTime.UtcNow;
        }
        else if (draft.ActionType == PendingActionType.DELETE && draft.TargetCodeUser != null)
        {
            // Soft-delete: Collector.CodeUser references Users, so we deactivate.
            var existing = await _db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.CodeUser == draft.TargetCodeUser)
                ?? throw new KeyNotFoundException("Target user no longer exists.");
            existing.Statut = "INACTIVE";
            existing.LastUserModif = _currentUser.CodeUser;
            existing.DateModification = DateTime.UtcNow;
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
