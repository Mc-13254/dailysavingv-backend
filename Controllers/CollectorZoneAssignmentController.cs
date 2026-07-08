using DailySavingV.API.Data;
using DailySavingV.API.DTOs;
using DailySavingV.API.Entities;
using DailySavingV.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DailySavingV.API.Controllers;

[ApiController]
[Route("api/collector-zone-assignment")]
[Authorize]
public class CollectorZoneAssignmentController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CollectorZoneAssignmentController(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    // GET api/collector-zone-assignment/{collectorId}
    [HttpGet("{collectorId}")]
    public async Task<ActionResult<CollectorAssignmentSummaryDto>> GetForCollector(string collectorId)
    {
        var collector = await _db.Collectors.FirstOrDefaultAsync(c => c.CollectorID == collectorId)
            ?? throw new KeyNotFoundException("Collector not found in your agency.");

        var assignments = await _db.CollectorZoneAssignments
            .Include(a => a.ZoneCollecte).ThenInclude(z => z!.Ville)
            .Where(a => a.CollectorID == collectorId && a.Status == "ACTIVE")
            .ToListAsync();

        var zoneDtos = new List<ZoneDto>();
        foreach (var a in assignments)
        {
            if (a.ZoneCollecte == null) continue;
            var clientCount = await _db.Clients.CountAsync(c => c.ZoneCollecteID == a.ZoneCollecteID);
            var activeCollectors = await _db.CollectorZoneAssignments
                .CountAsync(x => x.ZoneCollecteID == a.ZoneCollecteID && x.Status == "ACTIVE");
            zoneDtos.Add(new ZoneDto(
                a.ZoneCollecte.ZoneCollecteID, a.ZoneCollecte.Code, a.ZoneCollecte.Libelle, a.ZoneCollecte.Description,
                a.ZoneCollecte.VilleID, a.ZoneCollecte.Ville?.Nom, a.ZoneCollecte.District, a.ZoneCollecte.Neighborhood,
                a.ZoneCollecte.Village, a.ZoneCollecte.Latitude, a.ZoneCollecte.Longitude, a.ZoneCollecte.ShapeType,
                a.ZoneCollecte.PolygonCoordinates, a.ZoneCollecte.RadiusMeters, a.ZoneCollecte.Statut, clientCount, activeCollectors
            ));
        }

        var totalClients = await _db.Clients.CountAsync(c => c.CollectorID == collectorId);

        return Ok(new CollectorAssignmentSummaryDto(
            collector.CollectorID, $"{collector.Name} {collector.Surname}".Trim(), null,
            zoneDtos, totalClients
        ));
    }

    // POST api/collector-zone-assignment/assign
    // Assigns one or more zones to a collector, then optionally assigns specific
    // clients within those zones immediately (Client.CollectorID + ZoneCollecteID are updated).
    [Authorize(Policy = "SupervisorOrAdmin")]
    [HttpPost("assign")]
    public async Task<ActionResult> AssignZones(AssignZoneRequest request)
    {
        var collector = await _db.Collectors.FirstOrDefaultAsync(c => c.CollectorID == request.CollectorID)
            ?? throw new KeyNotFoundException("Collector not found in your agency.");

        foreach (var zoneId in request.ZoneCollecteIds)
        {
            var existingActive = await _db.CollectorZoneAssignments
                .FirstOrDefaultAsync(a => a.ZoneCollecteID == zoneId && a.Status == "ACTIVE");

            if (existingActive != null && existingActive.CollectorID != request.CollectorID)
            {
                return BadRequest(new
                {
                    message = $"La zone {zoneId} a déjà un collecteur actif. Utilisez le transfert pour la réaffecter."
                });
            }
            if (existingActive != null) continue; // already assigned to this same collector

            _db.CollectorZoneAssignments.Add(new CollectorZoneAssignment
            {
                CollectorID = request.CollectorID,
                ZoneCollecteID = zoneId,
                Status = "ACTIVE",
                AssignmentDate = DateTime.UtcNow,
                AssignedBy = _currentUser.CodeUser
            });

            _db.ZoneAssignmentHistories.Add(new ZoneAssignmentHistory
            {
                CollectorID = request.CollectorID,
                ZoneCollecteID = zoneId,
                EventType = "ZONE_ASSIGNED",
                ActionBy = _currentUser.CodeUser
            });

            // Also keep Collector.ZoneCollecteID (legacy single-zone field used
            // by the existing Collector Management screen) pointing at the most
            // recently assigned zone, for backward compatibility.
            collector.ZoneCollecteID = zoneId;
        }

        if (request.ClientIds is { Count: > 0 })
        {
            foreach (var clientId in request.ClientIds)
                await AssignClientToCollector(clientId, request.CollectorID);
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = "Zone(s) affectée(s) avec succès." });
    }

    // PUT api/collector-zone-assignment/{collectorId}/zones -> add/remove zones
    [Authorize(Policy = "SupervisorOrAdmin")]
    [HttpPut("{collectorId}/zones")]
    public async Task<ActionResult> UpdateZones(string collectorId, UpdateCollectorZonesRequest request)
    {
        if (request.AddZoneIds != null)
            await AssignZones(new AssignZoneRequest(collectorId, request.AddZoneIds, null));

        if (request.RemoveZoneIds != null)
        {
            foreach (var zoneId in request.RemoveZoneIds)
            {
                var active = await _db.CollectorZoneAssignments.FirstOrDefaultAsync(
                    a => a.CollectorID == collectorId && a.ZoneCollecteID == zoneId && a.Status == "ACTIVE");
                if (active == null) continue;

                active.Status = "ENDED";
                active.EndDate = DateTime.UtcNow;

                _db.ZoneAssignmentHistories.Add(new ZoneAssignmentHistory
                {
                    CollectorID = collectorId,
                    ZoneCollecteID = zoneId,
                    EventType = "ZONE_REMOVED",
                    ActionBy = _currentUser.CodeUser
                });
            }
            await _db.SaveChangesAsync();
        }

        return Ok(new { message = "Zones mises à jour." });
    }

    // POST api/collector-zone-assignment/transfer-clients
    [Authorize(Policy = "SupervisorOrAdmin")]
    [HttpPost("transfer-clients")]
    public async Task<ActionResult> TransferClients(TransferClientsRequest request)
    {
        var newCollector = await _db.Collectors.FirstOrDefaultAsync(c => c.CollectorID == request.NewCollectorID)
            ?? throw new KeyNotFoundException("Target collector not found in your agency.");

        foreach (var clientId in request.ClientIds)
            await AssignClientToCollector(clientId, request.NewCollectorID, transferred: true);

        await _db.SaveChangesAsync();
        return Ok(new { message = $"{request.ClientIds.Count} client(s) transféré(s) vers {newCollector.Name}." });
    }

    // GET api/collector-zone-assignment/history?collectorId=&zoneId=
    [HttpGet("history")]
    public async Task<ActionResult<IEnumerable<AssignmentHistoryDto>>> GetHistory(
        [FromQuery] string? collectorId, [FromQuery] int? zoneId)
    {
        // Collectors is already agency-scoped via the global query filter, so this
        // also scopes the (otherwise agency-agnostic) history table to the caller's agency.
        var agencyCollectorIds = await _db.Collectors.Select(c => c.CollectorID).ToListAsync();

        var query = _db.ZoneAssignmentHistories.Where(h => agencyCollectorIds.Contains(h.CollectorID));
        if (!string.IsNullOrWhiteSpace(collectorId)) query = query.Where(h => h.CollectorID == collectorId);
        if (zoneId.HasValue) query = query.Where(h => h.ZoneCollecteID == zoneId.Value);

        var history = await query.OrderByDescending(h => h.EventDate).Take(500).ToListAsync();

        var collectorIds = history.Select(h => h.CollectorID).Distinct().ToList();
        var collectors = await _db.Collectors.IgnoreQueryFilters()
            .Where(c => collectorIds.Contains(c.CollectorID))
            .ToDictionaryAsync(c => c.CollectorID, c => $"{c.Name} {c.Surname}".Trim());

        var zoneIds = history.Select(h => h.ZoneCollecteID).Distinct().ToList();
        var zones = await _db.ZoneCollectes.IgnoreQueryFilters()
            .Where(z => zoneIds.Contains(z.ZoneCollecteID))
            .ToDictionaryAsync(z => z.ZoneCollecteID, z => z.Libelle);

        var clientIds = history.Where(h => h.ClientID != null).Select(h => h.ClientID!).Distinct().ToList();
        var clients = await _db.Clients.IgnoreQueryFilters()
            .Where(c => clientIds.Contains(c.ClientID))
            .ToDictionaryAsync(c => c.ClientID, c => $"{c.Nom} {c.Prenom}".Trim());

        var result = history.Select(h => new AssignmentHistoryDto(
            h.HistoryID, h.CollectorID, collectors.GetValueOrDefault(h.CollectorID, h.CollectorID),
            h.ZoneCollecteID, zones.GetValueOrDefault(h.ZoneCollecteID),
            h.ClientID, h.ClientID != null ? clients.GetValueOrDefault(h.ClientID, h.ClientID) : null,
            h.EventType, h.FromCollectorID, h.EventDate, h.ActionBy
        ));

        return Ok(result);
    }

    private async Task AssignClientToCollector(string clientId, string collectorId, bool transferred = false)
    {
        var client = await _db.Clients.FirstOrDefaultAsync(c => c.ClientID == clientId);
        if (client == null) return;

        var previousCollectorId = client.CollectorID;

        client.CollectorID = collectorId;
        // Keep the client's zone consistent with whichever zone the target
        // collector actually manages, if the client didn't already belong to one.
        if (client.ZoneCollecteID == null)
        {
            var zoneAssignment = await _db.CollectorZoneAssignments
                .FirstOrDefaultAsync(a => a.CollectorID == collectorId && a.Status == "ACTIVE");
            if (zoneAssignment != null) client.ZoneCollecteID = zoneAssignment.ZoneCollecteID;
        }

        _db.ZoneAssignmentHistories.Add(new ZoneAssignmentHistory
        {
            CollectorID = collectorId,
            ZoneCollecteID = client.ZoneCollecteID ?? 0,
            ClientID = clientId,
            EventType = transferred ? "CLIENT_TRANSFERRED" : "CLIENT_ASSIGNED",
            FromCollectorID = transferred ? previousCollectorId : null,
            ActionBy = _currentUser.CodeUser
        });
    }
}
