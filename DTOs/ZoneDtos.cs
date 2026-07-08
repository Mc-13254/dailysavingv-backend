namespace DailySavingV.API.DTOs;

public record ZoneDto(
    int ZoneCollecteID, string Code, string? Libelle, string? Description,
    int? VilleID, string? VilleNom, string? District, string? Neighborhood, string? Village,
    decimal? Latitude, decimal? Longitude, string? ShapeType, string? PolygonCoordinates,
    decimal? RadiusMeters, bool Statut, int ClientCount, int ActiveCollectorCount
);

public record CreateZoneRequest(
    string Libelle, string? Description, int? VilleID,
    string? District, string? Neighborhood, string? Village,
    decimal? Latitude, decimal? Longitude, string? ShapeType,
    string? PolygonCoordinates, decimal? RadiusMeters
);

public record UpdateZoneRequest(
    string? Libelle, string? Description, int? VilleID,
    string? District, string? Neighborhood, string? Village,
    decimal? Latitude, decimal? Longitude, string? ShapeType,
    string? PolygonCoordinates, decimal? RadiusMeters, bool? Statut
);

public record ClientInZoneDto(
    string ClientID, string Nom, string? Prenom, string? PhoneNumber, string? Address,
    decimal Balance, string ValidationStatus, string? CurrentCollectorID
);
