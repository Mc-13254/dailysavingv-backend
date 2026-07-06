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
[Authorize(Policy = "AdminOnly")]
public class IMFController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public IMFController(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    private static IMFDto ToDto(Entities.IMF i) => new(
        i.CodeIMF, i.Libelle, i.ShortName, i.Statut, i.TauxTaxe, i.AssujettiTaxe,
        i.SuffixeCompte, i.PrefixeCompte, i.TailleCompte, i.CalculCommission,
        i.RegistrationNumber, i.TaxNumber, i.Description, i.LogoBase64,
        i.PrimaryPhone, i.SecondaryPhone, i.Email, i.Website,
        i.PaysID, i.Pays?.Nom, i.VilleID, i.Ville?.Nom, i.Address, i.PostalCode,
        i.CurrencyCode, i.Language, i.Timezone,
        i.CreatedBy, i.DateCreation, i.UpdatedBy, i.UpdatedDate
    );

    [HttpGet]
    public async Task<ActionResult<IEnumerable<IMFDto>>> GetAll()
    {
        var result = await _db.IMFs.Include(i => i.Pays).Include(i => i.Ville)
            .Select(i => i)
            .ToListAsync();
        return Ok(result.Select(ToDto));
    }

    // Supports the "only one active IMF" business rule on the frontend:
    // the Create button is disabled when this returns true.
    [HttpGet("has-active")]
    public async Task<ActionResult<bool>> HasActive()
    {
        var exists = await _db.IMFs.AnyAsync(i => i.Statut == "ACTIVE");
        return Ok(exists);
    }

    [HttpPost]
    public async Task<ActionResult> Create(CreateIMFRequest request)
    {
        // Business rule: only one active IMF allowed system-wide.
        var hasActive = await _db.IMFs.AnyAsync(i => i.Statut == "ACTIVE");
        if (hasActive)
            return BadRequest(new { message = "Une IMF active existe déjà. Modifiez l'IMF existante plutôt que d'en créer une nouvelle." });

        var draft = new IMFTmp
        {
            ActionType = PendingActionType.CREATE,
            TargetCodeIMF = request.CodeIMF,
            Libelle = request.Libelle,
            ShortName = request.ShortName,
            Statut = "ACTIVE",
            TauxTaxe = request.TauxTaxe,
            AssujettiTaxe = request.AssujettiTaxe,
            SuffixeCompte = request.SuffixeCompte,
            PrefixeCompte = request.PrefixeCompte,
            TailleCompte = request.TailleCompte,
            CalculCommission = request.CalculCommission,
            RegistrationNumber = request.RegistrationNumber,
            TaxNumber = request.TaxNumber,
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
            CurrencyCode = request.CurrencyCode,
            Language = request.Language,
            Timezone = request.Timezone,
            RequestUser = _currentUser.CodeUser!,
            NewData = JsonSerializer.Serialize(request)
        };

        _db.IMFTmps.Add(draft);
        await _db.SaveChangesAsync();

        return Ok(new { message = "IMF soumis pour validation.", pendingId = draft.PendingID });
    }

    [HttpPut("{code}")]
    public async Task<ActionResult> Update(string code, UpdateIMFRequest request)
    {
        var existing = await _db.IMFs.FirstOrDefaultAsync(i => i.CodeIMF == code)
            ?? throw new KeyNotFoundException("IMF not found.");

        var draft = new IMFTmp
        {
            ActionType = PendingActionType.UPDATE,
            TargetCodeIMF = code,
            Libelle = request.Libelle,
            ShortName = request.ShortName,
            Statut = request.Statut,
            TauxTaxe = request.TauxTaxe,
            AssujettiTaxe = request.AssujettiTaxe,
            SuffixeCompte = request.SuffixeCompte,
            PrefixeCompte = request.PrefixeCompte,
            TailleCompte = request.TailleCompte,
            CalculCommission = request.CalculCommission,
            TaxNumber = request.TaxNumber,
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
            CurrencyCode = request.CurrencyCode,
            Language = request.Language,
            Timezone = request.Timezone,
            RequestUser = _currentUser.CodeUser!,
            PreviousData = JsonSerializer.Serialize(existing),
            NewData = JsonSerializer.Serialize(request)
        };

        _db.IMFTmps.Add(draft);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Modification soumise pour validation.", pendingId = draft.PendingID });
    }

    [HttpDelete("{code}")]
    public async Task<ActionResult> Delete(string code)
    {
        var existing = await _db.IMFs.FirstOrDefaultAsync(i => i.CodeIMF == code)
            ?? throw new KeyNotFoundException("IMF not found.");

        // Protect referential integrity: block deletion if any agency uses this IMF.
        var isReferenced = await _db.Agences.AnyAsync(a => a.CodeIMF == code);
        if (isReferenced)
            return BadRequest(new { message = "Impossible de supprimer cette IMF car elle est déjà utilisée par une ou plusieurs agences." });

        var draft = new IMFTmp
        {
            ActionType = PendingActionType.DELETE,
            TargetCodeIMF = code,
            RequestUser = _currentUser.CodeUser!,
            PreviousData = JsonSerializer.Serialize(existing)
        };

        _db.IMFTmps.Add(draft);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Suppression soumise pour validation.", pendingId = draft.PendingID });
    }

    [HttpGet("pending")]
    public async Task<ActionResult<IEnumerable<IMFTmp>>> GetPending()
    {
        var pending = await _db.IMFTmps.Where(t => t.PendingStatus == PendingStatusEnum.PENDING).OrderBy(t => t.RequestDate).ToListAsync();
        return Ok(pending);
    }

    [HttpPost("pending/{pendingId:int}/approve")]
    public async Task<ActionResult> Approve(int pendingId)
    {
        var draft = await _db.IMFTmps.FirstOrDefaultAsync(t => t.PendingID == pendingId)
            ?? throw new KeyNotFoundException("Pending record not found.");

        if (draft.PendingStatus != PendingStatusEnum.PENDING)
            return BadRequest(new { message = "This request has already been processed." });

        if (draft.ActionType == PendingActionType.CREATE)
        {
            // Re-check the business rule at approval time too (state may have changed).
            var hasActive = await _db.IMFs.AnyAsync(i => i.Statut == "ACTIVE");
            if (hasActive)
                return BadRequest(new { message = "Une IMF active existe déjà — impossible d'approuver cette création." });

            _db.IMFs.Add(new Entities.IMF
            {
                CodeIMF = draft.TargetCodeIMF!,
                Libelle = draft.Libelle!,
                ShortName = draft.ShortName,
                Statut = "ACTIVE",
                TauxTaxe = draft.TauxTaxe ?? 0,
                AssujettiTaxe = draft.AssujettiTaxe ?? false,
                SuffixeCompte = draft.SuffixeCompte,
                PrefixeCompte = draft.PrefixeCompte,
                TailleCompte = draft.TailleCompte ?? 10,
                CalculCommission = draft.CalculCommission ?? true,
                RegistrationNumber = draft.RegistrationNumber,
                TaxNumber = draft.TaxNumber,
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
                CurrencyCode = draft.CurrencyCode,
                Language = draft.Language,
                Timezone = draft.Timezone,
                CreatedBy = draft.RequestUser
            });
        }
        else if (draft.ActionType == PendingActionType.UPDATE && draft.TargetCodeIMF != null)
        {
            var existing = await _db.IMFs.FirstOrDefaultAsync(i => i.CodeIMF == draft.TargetCodeIMF)
                ?? throw new KeyNotFoundException("Target IMF no longer exists.");

            if (draft.Libelle != null) existing.Libelle = draft.Libelle;
            existing.ShortName = draft.ShortName;
            if (draft.Statut != null) existing.Statut = draft.Statut;
            if (draft.TauxTaxe.HasValue) existing.TauxTaxe = draft.TauxTaxe.Value;
            if (draft.AssujettiTaxe.HasValue) existing.AssujettiTaxe = draft.AssujettiTaxe.Value;
            existing.SuffixeCompte = draft.SuffixeCompte;
            existing.PrefixeCompte = draft.PrefixeCompte;
            if (draft.TailleCompte.HasValue) existing.TailleCompte = draft.TailleCompte.Value;
            if (draft.CalculCommission.HasValue) existing.CalculCommission = draft.CalculCommission.Value;
            existing.TaxNumber = draft.TaxNumber;
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
            existing.CurrencyCode = draft.CurrencyCode;
            existing.Language = draft.Language;
            existing.Timezone = draft.Timezone;
            existing.UpdatedBy = _currentUser.CodeUser;
            existing.UpdatedDate = DateTime.UtcNow;
        }
        else if (draft.ActionType == PendingActionType.DELETE && draft.TargetCodeIMF != null)
        {
            var existing = await _db.IMFs.FirstOrDefaultAsync(i => i.CodeIMF == draft.TargetCodeIMF)
                ?? throw new KeyNotFoundException("Target IMF no longer exists.");

            // Re-check referential integrity at approval time too.
            var isReferenced = await _db.Agences.AnyAsync(a => a.CodeIMF == existing.CodeIMF);
            if (isReferenced)
                return BadRequest(new { message = "Impossible de valider : cette IMF est désormais utilisée par une ou plusieurs agences." });

            existing.Statut = "INACTIF"; // soft delete
            existing.UpdatedBy = _currentUser.CodeUser;
            existing.UpdatedDate = DateTime.UtcNow;
        }

        draft.PendingStatus = PendingStatusEnum.APPROVED;
        draft.ValidationUser = _currentUser.CodeUser;
        draft.ValidationDate = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new { message = "IMF validé." });
    }

    [HttpPost("pending/{pendingId:int}/reject")]
    public async Task<ActionResult> Reject(int pendingId, RejectRequest request)
    {
        var draft = await _db.IMFTmps.FirstOrDefaultAsync(t => t.PendingID == pendingId)
            ?? throw new KeyNotFoundException("Pending record not found.");

        draft.PendingStatus = PendingStatusEnum.REJECTED;
        draft.ValidationUser = _currentUser.CodeUser;
        draft.ValidationDate = DateTime.UtcNow;
        draft.RejectionReason = request.Reason;

        await _db.SaveChangesAsync();
        return Ok(new { message = "IMF rejeté." });
    }
}
