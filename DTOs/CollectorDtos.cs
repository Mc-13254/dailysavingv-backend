namespace DailySavingV.API.DTOs;

public record CollectorDto(
    string CollectorID, string CodeUser, string Name, string? PhoneNumber,
    int AgenceID, string? AgenceNom, bool IsActive, DateTime? DateEmploi,
    string? ContactType, string? CodeTerminal, decimal Plafond
);

public record CreateCollectorRequest(
    string CodeUser, string Name, string? PhoneNumber, int? ZoneCollecteID,
    DateTime? DateEmploi, string? ContactType, string? CodeTerminal, decimal Plafond
);

public record UpdateCollectorRequest(
    string? Name, string? PhoneNumber, bool? IsActive, int? ZoneCollecteID,
    string? ContactType, string? CodeTerminal, decimal? Plafond
);

public record PendingActionDto(string PendingId, string ActionType, string RequestUser, DateTime RequestDate, string PendingStatus);

public record RejectRequest(string Reason);
