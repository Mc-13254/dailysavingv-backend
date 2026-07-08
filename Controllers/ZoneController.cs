using DailySavingV.API.Data;
using DailySavingV.API.DTOs;
using DailySavingV.API.Entities;
using DailySavingV.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DailySavingV.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ZoneController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ZoneController(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    // GET api/zone?search=...  -> auto agency-scoped (global query filter on ZoneCollecte)
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ZoneDto>>> GetAll([FromQuery] string? search)
    {
        var query = _db.ZoneCollectes.Include(z => z.Ville).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(z =>
                (z.Libelle != null && z.Libelle.Contains(search)) ||
                (z.Village != null && z.Village.Contains(search)) ||
                (z.District != null && z.District.Contains(search)) ||
                (z.Ville != null && z.Ville.Nom.Contains(search)));

        var zones = await query.OrderBy(z => z.Libelle).ToListAsync();
        var result = new List<ZoneDto>();
        foreach (var z in zones)
            result.Add(await ToDto(z));

        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ZoneDto>> GetById(int id)
    {
        var zone = await _db.ZoneCollectes.Include(z => z.Ville).FirstOrDefaultAsync(z => z.ZoneCollecteID == id);
        if (zone == null) return NotFound();
        return Ok(await ToDto(zone));
    }

    // GET api/zone/{id}/clients -> clients whose ZoneCollecteID = id (agency-scoped via Client filter)
    [HttpGet("{id:int}/clients")]
    public async Task<ActionResult<IEnumerable<ClientInZoneDto>>> GetClients(int id)
    {
        var clients = await _db.Clients
            .Where(c => c.ZoneCollecteID == id)
            .ToListAsync();

        var clientIds = clients.Select(c => c.ClientID).ToList();
        var balances = await _db.Accounts.IgnoreQueryFilters()
            .Where(a => clientIds.Contains(a.ClientID) && a.Active)
            .GroupBy(a => a.ClientID)
            .Select(g => new { ClientID = g.Key, Balance = g.Sum(a => a.Balance) })
            .ToListAsync();
        var balanceMap = balances.ToDictionary(b => b.ClientID, b => b.Balance);

        var result = clients.Select(c => new ClientInZoneDto(
            c.ClientID, c.Nom, c.Prenom, c.PhoneNumber, c.Address,
            balanceMap.GetValueOrDefault(c.ClientID, 0m), c.ValidationStatus, c.CollectorID
        ));

        return Ok(result);
    }

    // POST api/zone -> created directly (no Maker-Checker for zones; see integration notes)
    [Authorize(Policy = "SupervisorOrAdmin")]
    [HttpPost]
    public async Task<ActionResult<ZoneDto>> Create(CreateZoneRequest request)
    {
        var zone = new ZoneCollecte
        {
            Code = await GenerateNextZoneCode(),
            Libelle = request.Libelle,
            Description = request.Description,
            VilleID = request.VilleID,
            District = request.District,
            Neighborhood = request.Neighborhood,
            Village = request.Village,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            ShapeType = request.ShapeType,
            PolygonCoordinates = request.PolygonCoordinates,
            RadiusMeters = request.RadiusMeters,
            AgenceID = _currentUser.IsHeadOffice ? null : _currentUser.AgenceID,
            Statut = true,
            UserCreate = _currentUser.CodeUser,
            CreateDate = DateTime.UtcNow
        };

        _db.ZoneCollectes.Add(zone);
        await _db.SaveChangesAsync();
        return Ok(await ToDto(zone));
    }

    [Authorize(Policy = "SupervisorOrAdmin")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ZoneDto>> Update(int id, UpdateZoneRequest request)
    {
        var zone = await _db.ZoneCollectes.FirstOrDefaultAsync(z => z.ZoneCollecteID == id)
            ?? throw new KeyNotFoundException("Zone not found.");

        if (request.Libelle != null) zone.Libelle = request.Libelle;
        if (request.Description != null) zone.Description = request.Description;
        if (request.VilleID.HasValue) zone.VilleID = request.VilleID;
        if (request.District != null) zone.District = request.District;
        if (request.Neighborhood != null) zone.Neighborhood = request.Neighborhood;
        if (request.Village != null) zone.Village = request.Village;
        if (request.Latitude.HasValue) zone.Latitude = request.Latitude;
        if (request.Longitude.HasValue) zone.Longitude = request.Longitude;
        if (request.ShapeType != null) zone.ShapeType = request.ShapeType;
        if (request.PolygonCoordinates != null) zone.PolygonCoordinates = request.PolygonCoordinates;
        if (request.RadiusMeters.HasValue) zone.RadiusMeters = request.RadiusMeters;
        if (request.Statut.HasValue) zone.Statut = request.Statut.Value;

        await _db.SaveChangesAsync();
        return Ok(await ToDto(zone));
    }

    // DELETE api/zone/{id} -> soft delete only (Statut = false); history is preserved
    [Authorize(Policy = "SupervisorOrAdmin")]
    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        var zone = await _db.ZoneCollectes.FirstOrDefaultAsync(z => z.ZoneCollecteID == id)
            ?? throw new KeyNotFoundException("Zone not found.");

        zone.Statut = false;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Zone désactivée (soft delete)." });
    }

    private async Task<ZoneDto> ToDto(ZoneCollecte z)
    {
        var clientCount = await _db.Clients.CountAsync(c => c.ZoneCollecteID == z.ZoneCollecteID);
        var activeCollectors = await _db.CollectorZoneAssignments
            .CountAsync(a => a.ZoneCollecteID == z.ZoneCollecteID && a.Status == "ACTIVE");

        return new ZoneDto(
            z.ZoneCollecteID, z.Code, z.Libelle, z.Description,
            z.VilleID, z.Ville?.Nom, z.District, z.Neighborhood, z.Village,
            z.Latitude, z.Longitude, z.ShapeType, z.PolygonCoordinates,
            z.RadiusMeters, z.Statut, clientCount, activeCollectors
        );
    }

    private async Task<string> GenerateNextZoneCode()
    {
        var count = await _db.ZoneCollectes.IgnoreQueryFilters().CountAsync();
        return $"ZC-{(count + 1):D5}";
    }
}
