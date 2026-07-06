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

    [HttpGet]
    public async Task<ActionResult<IEnumerable<IMFDto>>> GetAll()
    {
        var result = await _db.IMFs
            .Select(i => new IMFDto(i.CodeIMF, i.Libelle, i.Statut, i.TauxTaxe, i.AssujettiTaxe, i.SuffixeCompte, i.PrefixeCompte, i.TailleCompte, i.CalculCommission, i.DateCreation))
            .ToListAsync();
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult> Create(CreateIMFRequest request)
    {
        var draft = new IMFTmp
        {
            ActionType = PendingActionType.CREATE,
            TargetCodeIMF = request.CodeIMF,
            Libelle = request.Libelle,
            Statut = "ACTIVE",
            TauxTaxe = request.TauxTaxe,
            AssujettiTaxe = request.AssujettiTaxe,
            SuffixeCompte = request.SuffixeCompte,
            PrefixeCompte = request.PrefixeCompte,
            TailleCompte = request.TailleCompte,
            CalculCommission = request.CalculCommission,
            RequestUser = _currentUser.CodeUser!,
            NewData = JsonSerializer.Serialize(request)
        };

        _db.IMFTmps.Add(draft);
        await _db.SaveChangesAsync();

        return Ok(new { message = "IMF soumis pour validation.", pendingId = draft.PendingID });
    }

    [HttpGet("pending")]
    public async Task<ActionResult<IEnumerable<IMFTmp>>> GetPending()
    {
        var pending = await _db.IMFTmps.Where(t => t.PendingStatus == PendingStatusEnum.PENDING).OrderBy(t => t.RequestDate).ToListAsync();
        return Ok(pending);
    }

    [HttpPut("{code}")]
    public async Task<ActionResult> Update(string code, CreateIMFRequest request)
    {
        var existing = await _db.IMFs.FirstOrDefaultAsync(i => i.CodeIMF == code)
            ?? throw new KeyNotFoundException("IMF not found.");

        var draft = new IMFTmp
        {
            ActionType = PendingActionType.UPDATE,
            TargetCodeIMF = code,
            Libelle = request.Libelle,
            TauxTaxe = request.TauxTaxe,
            AssujettiTaxe = request.AssujettiTaxe,
            SuffixeCompte = request.SuffixeCompte,
            PrefixeCompte = request.PrefixeCompte,
            TailleCompte = request.TailleCompte,
            CalculCommission = request.CalculCommission,
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

    [HttpPost("pending/{pendingId:int}/approve")]
    public async Task<ActionResult> Approve(int pendingId)
    {
        var draft = await _db.IMFTmps.FirstOrDefaultAsync(t => t.PendingID == pendingId)
            ?? throw new KeyNotFoundException("Pending record not found.");

        if (draft.PendingStatus != PendingStatusEnum.PENDING)
            return BadRequest(new { message = "This request has already been processed." });

        if (draft.ActionType == PendingActionType.CREATE)
        {
            _db.IMFs.Add(new Entities.IMF
            {
                CodeIMF = draft.TargetCodeIMF!,
                Libelle = draft.Libelle!,
                Statut = draft.Statut ?? "ACTIVE",
                TauxTaxe = draft.TauxTaxe ?? 0,
                AssujettiTaxe = draft.AssujettiTaxe ?? false,
                SuffixeCompte = draft.SuffixeCompte,
                PrefixeCompte = draft.PrefixeCompte,
                TailleCompte = draft.TailleCompte ?? 10,
                CalculCommission = draft.CalculCommission ?? true,
                CreatedBy = draft.RequestUser
            });
        }
        else if (draft.ActionType == PendingActionType.UPDATE && draft.TargetCodeIMF != null)
        {
            var existing = await _db.IMFs.FirstOrDefaultAsync(i => i.CodeIMF == draft.TargetCodeIMF)
                ?? throw new KeyNotFoundException("Target IMF no longer exists.");
            if (draft.Libelle != null) existing.Libelle = draft.Libelle;
            if (draft.TauxTaxe.HasValue) existing.TauxTaxe = draft.TauxTaxe.Value;
            if (draft.AssujettiTaxe.HasValue) existing.AssujettiTaxe = draft.AssujettiTaxe.Value;
            if (draft.SuffixeCompte != null) existing.SuffixeCompte = draft.SuffixeCompte;
            if (draft.PrefixeCompte != null) existing.PrefixeCompte = draft.PrefixeCompte;
            if (draft.TailleCompte.HasValue) existing.TailleCompte = draft.TailleCompte.Value;
            if (draft.CalculCommission.HasValue) existing.CalculCommission = draft.CalculCommission.Value;
        }
        else if (draft.ActionType == PendingActionType.DELETE && draft.TargetCodeIMF != null)
        {
            var existing = await _db.IMFs.FirstOrDefaultAsync(i => i.CodeIMF == draft.TargetCodeIMF)
                ?? throw new KeyNotFoundException("Target IMF no longer exists.");
            existing.Statut = "INACTIF";
        }

        draft.PendingStatus = PendingStatusEnum.APPROVED;
        draft.ValidationUser = _currentUser.CodeUser;
        draft.ValidationDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new { message = "IMF validé et créé en production." });
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
