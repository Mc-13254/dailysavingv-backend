using DailySavingV.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DailySavingV.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GeoController : ControllerBase
{
    private readonly AppDbContext _db;
    public GeoController(AppDbContext db) { _db = db; }

    [HttpGet("countries")]
    public async Task<ActionResult> GetCountries()
    {
        var result = await _db.Pays
            .Select(p => new { p.PaysID, p.Nom, p.Code })
            .OrderBy(p => p.Nom)
            .ToListAsync();
        return Ok(result);
    }

    // Cities depend on the selected country (Pays -> Region -> Ville in our schema).
    [HttpGet("cities")]
    public async Task<ActionResult> GetCities([FromQuery] int? paysId)
    {
        var query = _db.Villes.Include(v => v.Region).AsQueryable();
        if (paysId.HasValue)
            query = query.Where(v => v.Region!.PaysID == paysId.Value);

        var result = await query
            .Select(v => new { v.VilleID, v.Nom })
            .OrderBy(v => v.Nom)
            .ToListAsync();
        return Ok(result);
    }

    [HttpGet("currencies")]
    public async Task<ActionResult> GetCurrencies()
    {
        var result = await _db.Currencies.Where(c => c.Statut)
            .Select(c => new { c.CurrencyCode, c.Nom, c.Symbole })
            .OrderBy(c => c.CurrencyCode)
            .ToListAsync();
        return Ok(result);
    }

    [HttpGet("languages")]
    public async Task<ActionResult> GetLanguages()
    {
        var result = await _db.Languages.Where(l => l.Statut)
            .Select(l => new { l.LanguageCode, l.Nom })
            .ToListAsync();
        return Ok(result);
    }

    [HttpGet("timezones")]
    public async Task<ActionResult> GetTimeZones()
    {
        var result = await _db.TimeZones.Where(t => t.Statut)
            .Select(t => new { t.TimeZoneID, t.Code, t.Label, t.UtcOffset })
            .OrderBy(t => t.Label)
            .ToListAsync();
        return Ok(result);
    }
}
