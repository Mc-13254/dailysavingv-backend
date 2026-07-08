namespace DailySavingV.API.DTOs;

public record AssignZoneRequest(
    string CollectorID, List<int> ZoneCollecteIds, List<string>? ClientIds
);

public record TransferClientsRequest(
    List<string> ClientIds, string NewCollectorID
);

public record UpdateCollectorZonesRequest(
    List<int>? AddZoneIds, List<int>? RemoveZoneIds
);

public record CollectorAssignmentSummaryDto(
    string CollectorID, string CollectorName, string? PhotoUrl,
    List<ZoneDto> Zones, int TotalAssignedClients
);

public record AssignmentHistoryDto(
    long HistoryID, string CollectorID, string CollectorName, int ZoneCollecteID, string? ZoneLibelle,
    string? ClientID, string? ClientName, string EventType, string? FromCollectorID,
    DateTime EventDate, string? ActionBy
);
