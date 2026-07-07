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

    private static AgenceDto ToDto(Entities.Agence a) => new(
        a.AgenceID, a.CodeAgence, a.Nom, a.ShortName, a.Description, a.LogoBase64,
        a.PrimaryPhone, a.SecondaryPhone, a.Email, a.Website,
        a.PaysID, a.Pays?.Nom, a.VilleID, a.Ville?.Nom, a.Address, a.PostalCode,
        a.CodeIMF, a.IMF?.Libelle, a.ManagerId, a.Manager?.Username, a.OpeningDate,
        a.Statut, a.DateCreated, a.CreatedBy, a.UpdatedBy, a.UpdatedDate
    );

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AgenceDto>>> GetAll()
    {
        var result = await _db.Agences
            .Include(a => a.Pays).Include(a => a.Ville).Include(a => a.IMF).Include(a => a.Manager)
            .ToListAsync();
        return Ok(result.Select(ToDto));
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
            ShortName = request.ShortName,
            Description = request.Description,
            LogoBase64 = request.LogoBase64,
            PrimaryPhone = request.PrimaryPhone,
            SecondaryPhone = request.SecondaryPhone,
            Email = request.Email,
            Website = request.Website,
            PaysID = request.PaysID,
            VilleID = request.VilleID,
            Address = request.Address,
            PostalCode = request.PostalCode,
            CodeIMF = request.CodeIMF,
            ManagerId = request.ManagerId,
            OpeningDate = request.OpeningDate,
            Statut = "ACTIVE",
            RequestUser = _currentUser.CodeUser!,
            NewData = JsonSerializer.Serialize(request)
        };

        _db.AgenceTmps.Add(draft);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Agence soumise pour validation.", pendingId = draft.PendingID });
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult> Update(int id, UpdateAgenceRequest request)
    {
        var existing = await _db.Agences.FirstOrDefaultAsync(a => a.AgenceID == id)
            ?? throw new KeyNotFoundException("Agence not found.");

        var draft = new AgenceTmp
        {
            ActionType = PendingActionType.UPDATE,
            TargetAgenceID = id,
            Nom = request.Nom,
            ShortName = request.ShortName,
            Description = request.Description,
            LogoBase64 = request.LogoBase64,
            PrimaryPhone = request.PrimaryPhone,
            SecondaryPhone = request.SecondaryPhone,
            Email = request.Email,
            Website = request.Website,
            PaysID = request.PaysID,
            VilleID = request.VilleID,
            Address = request.Address,
            PostalCode = request.PostalCode,
            ManagerId = request.ManagerId,
            OpeningDate = request.OpeningDate,
            Statut = request.Statut,
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

        // Protect referential integrity: block deletion if any user/collector/client uses this agency.
        var isReferenced = await _db.Users.IgnoreQueryFilters().AnyAsync(u => u.AgenceID == id)
            || await _db.Collectors.IgnoreQueryFilters().AnyAsync(c => c.AgenceID == id)
            || await _db.Clients.IgnoreQueryFilters().AnyAsync(c => c.AgenceID == id);
        if (isReferenced)
            return BadRequest(new { message = "Impossible de supprimer cette agence car elle est déjà utilisée par des utilisateurs, collecteurs ou clients." });

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
                ShortName = draft.ShortName,
                Description = draft.Description,
                LogoBase64 = draft.LogoBase64,
                PrimaryPhone = draft.PrimaryPhone,
                SecondaryPhone = draft.SecondaryPhone,
                Email = draft.Email,
                Website = draft.Website,
                PaysID = draft.PaysID,
                VilleID = draft.VilleID,
                Address = draft.Address,
                PostalCode = draft.PostalCode,
                CodeIMF = draft.CodeIMF!,
                ManagerId = draft.ManagerId,
                OpeningDate = draft.OpeningDate,
                Statut = "ACTIVE",
                CreatedBy = draft.RequestUser
            });
        }
        else if (draft.ActionType == PendingActionType.UPDATE && draft.TargetAgenceID.HasValue)
        {
            var existing = await _db.Agences.FirstOrDefaultAsync(a => a.AgenceID == draft.TargetAgenceID.Value)
                ?? throw new KeyNotFoundException("Target agency no longer exists.");
            if (draft.Nom != null) existing.Nom = draft.Nom;
            existing.ShortName = draft.ShortName;
            existing.Description = draft.Description;
            if (draft.LogoBase64 != null) existing.LogoBase64 = draft.LogoBase64;
            existing.PrimaryPhone = draft.PrimaryPhone;
            existing.SecondaryPhone = draft.SecondaryPhone;
            existing.Email = draft.Email;
            existing.Website = draft.Website;
            existing.PaysID = draft.PaysID;
            existing.VilleID = draft.VilleID;
            existing.Address = draft.Address;
            existing.PostalCode = draft.PostalCode;
            existing.ManagerId = draft.ManagerId;
            existing.OpeningDate = draft.OpeningDate;
            if (draft.Statut != null) existing.Statut = draft.Statut;
            existing.UpdatedBy = _currentUser.CodeUser;
            existing.UpdatedDate = DateTime.UtcNow;
        }
        else if (draft.ActionType == PendingActionType.DELETE && draft.TargetAgenceID.HasValue)
        {
            var existing = await _db.Agences.FirstOrDefaultAsync(a => a.AgenceID == draft.TargetAgenceID.Value)
                ?? throw new KeyNotFoundException("Target agency no longer exists.");

            var isReferenced = await _db.Users.IgnoreQueryFilters().AnyAsync(u => u.AgenceID == existing.AgenceID)
                || await _db.Collectors.IgnoreQueryFilters().AnyAsync(c => c.AgenceID == existing.AgenceID)
                || await _db.Clients.IgnoreQueryFilters().AnyAsync(c => c.AgenceID == existing.AgenceID);
            if (isReferenced)
                return BadRequest(new { message = "Impossible de valider : cette agence est désormais utilisée." });

            existing.Statut = "INACTIVE";
            existing.UpdatedBy = _currentUser.CodeUser;
            existing.UpdatedDate = DateTime.UtcNow;
        }

        draft.PendingStatus = PendingStatusEnum.APPROVED;
        draft.ValidationUser = _currentUser.CodeUser;
        draft.ValidationDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Agence validée." });
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
