using DailySavingV.API.Data;
using DailySavingV.API.DTOs;
using DailySavingV.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DailySavingV.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AdminOnly")]
public class NumberingParameterController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public NumberingParameterController(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    private static NumberingParameterDto ToDto(Entities.NumberingParameter p) => new(
        p.NumberingParameterID, p.EntityName, p.Prefix, p.Suffix, p.Separator,
        p.CurrentNumber, p.StartingNumber, p.NumberLength, p.PaddingCharacter,
        p.AllowReset, p.ResetFrequency, p.NextResetDate,
        p.AutoIncrement, p.IncrementValue, p.BuildPreview(), p.Statut,
        p.CreatedBy, p.CreatedDate, p.UpdatedBy, p.UpdatedDate
    );

    [HttpGet]
    public async Task<ActionResult<IEnumerable<NumberingParameterDto>>> GetAll()
    {
        var result = await _db.NumberingParameters.ToListAsync();
        return Ok(result.Select(ToDto));
    }

    [HttpGet("active")]
    public async Task<ActionResult<IEnumerable<NumberingParameterDto>>> GetActive()
    {
        var result = await _db.NumberingParameters.Where(p => p.Statut == "ACTIVE").ToListAsync();
        return Ok(result.Select(ToDto));
    }

    [HttpGet("by-entity/{entityName}")]
    public async Task<ActionResult<NumberingParameterDto>> GetByEntity(string entityName)
    {
        var p = await _db.NumberingParameters.FirstOrDefaultAsync(x => x.EntityName == entityName && x.Statut == "ACTIVE")
            ?? throw new KeyNotFoundException("No active numbering rule for this entity.");
        return Ok(ToDto(p));
    }

    // Live preview while the admin is still typing in the Create/Edit form —
    // does not touch the database.
    [HttpPost("preview")]
    public ActionResult<string> Preview(PreviewRequest request)
    {
        var sample = new Entities.NumberingParameter
        {
            Prefix = request.Prefix, Suffix = request.Suffix, Separator = request.Separator,
            NumberLength = request.NumberLength, PaddingCharacter = string.IsNullOrEmpty(request.PaddingCharacter) ? "0" : request.PaddingCharacter,
            CurrentNumber = request.SampleNumber ?? 1
        };
        return Ok(new { preview = sample.BuildPreview(request.SampleNumber ?? 1) });
    }

    [HttpPost]
    public async Task<ActionResult> Create(CreateNumberingParameterRequest request)
    {
        var entityTaken = await _db.NumberingParameters.AnyAsync(p => p.EntityName == request.EntityName);
        if (entityTaken)
            return BadRequest(new { message = "Cette entité possède déjà une règle de numérotation." });

        var prefixTaken = await _db.NumberingParameters.AnyAsync(p => p.Prefix == request.Prefix);
        if (prefixTaken)
            return BadRequest(new { message = "Ce préfixe est déjà utilisé par une autre règle." });

        _db.NumberingParameters.Add(new Entities.NumberingParameter
        {
            EntityName = request.EntityName,
            Prefix = request.Prefix,
            Suffix = request.Suffix,
            Separator = request.Separator,
            StartingNumber = request.StartingNumber,
            CurrentNumber = 0,
            NumberLength = request.NumberLength,
            PaddingCharacter = string.IsNullOrEmpty(request.PaddingCharacter) ? "0" : request.PaddingCharacter,
            AllowReset = request.AllowReset,
            ResetFrequency = request.ResetFrequency,
            NextResetDate = request.NextResetDate,
            AutoIncrement = request.AutoIncrement,
            IncrementValue = request.IncrementValue,
            Statut = "ACTIVE",
            CreatedBy = _currentUser.CodeUser
        });
        await _db.SaveChangesAsync();
        return Ok(new { message = "Règle de numérotation créée." });
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> Update(int id, UpdateNumberingParameterRequest request)
    {
        var existing = await _db.NumberingParameters.FirstOrDefaultAsync(p => p.NumberingParameterID == id)
            ?? throw new KeyNotFoundException("Numbering rule not found.");

        var prefixTaken = await _db.NumberingParameters.AnyAsync(p => p.Prefix == request.Prefix && p.NumberingParameterID != id);
        if (prefixTaken)
            return BadRequest(new { message = "Ce préfixe est déjà utilisé par une autre règle." });

        existing.Prefix = request.Prefix;
        existing.Suffix = request.Suffix;
        existing.Separator = request.Separator;
        existing.NumberLength = request.NumberLength;
        existing.PaddingCharacter = string.IsNullOrEmpty(request.PaddingCharacter) ? "0" : request.PaddingCharacter;
        existing.AllowReset = request.AllowReset;
        existing.ResetFrequency = request.ResetFrequency;
        existing.NextResetDate = request.NextResetDate;
        existing.AutoIncrement = request.AutoIncrement;
        existing.IncrementValue = request.IncrementValue;
        existing.Statut = request.Statut;
        existing.UpdatedBy = _currentUser.CodeUser;
        existing.UpdatedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Règle de numérotation modifiée." });
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        var existing = await _db.NumberingParameters.FirstOrDefaultAsync(p => p.NumberingParameterID == id)
            ?? throw new KeyNotFoundException("Numbering rule not found.");

        if (existing.CurrentNumber > 0)
            return BadRequest(new { message = "This numbering rule is already in use and cannot be deleted." });

        existing.Statut = "INACTIVE";
        existing.UpdatedBy = _currentUser.CodeUser;
        existing.UpdatedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Règle de numérotation désactivée." });
    }
}
