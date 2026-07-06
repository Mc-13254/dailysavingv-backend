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
}
