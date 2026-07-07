namespace DailySavingV.API.DTOs;

public record CollectorDto(
    string CollectorID, string CodeUser, string Name, string? Surname, string? PhoneNumber,
    int AgenceID, string? AgenceNom, int? DepartmentID, string? DepartmentNom,
    bool IsActive, string CDETAT, DateTime? DateEmploi, string? ContactType, string? CodeTerminal,
    decimal Plafond, decimal? Caution,
    int? ContractID, string? ContractNom, int? CommissionTypeID, string? CommissionTypeNom,
    int? CommissionRangeID, string? SupervisorId, string? SupervisorNom,
    decimal? CollectMonth, decimal? CollectDay, decimal? RetraitMonth, decimal? RetraitDay,
    string? UserCreate, DateTime CreateDate, string? UserValidation, DateTime? DateValidation,
    string? LastUserModif, DateTime? DateModification,
    string? LastUserSupervise, DateTime? LastDateSupervise
);

// Available User to become a Collector (Active, Role=Collector, not already assigned)
public record AvailableUserDto(
    string CodeUser, string? Photo, string? FirstName, string? LastName,
    int? AgenceID, string? AgenceNom, int? DepartmentID, string? DepartmentNom, string? Phone, string? Email
);

public record CreateCollectorRequest(
    string CodeUser, string? ContactType, DateTime? DateEmploi, string? CodeTerminal,
    decimal? Plafond, decimal? Caution,
    int? ContractID, int? CommissionTypeID, int? CommissionRangeID, string? SupervisorId,
    int? ZoneCollecteID, decimal? CollectMonth, decimal? CollectDay, decimal? RetraitMonth, decimal? RetraitDay
);

public record UpdateCollectorRequest(
    string? ContactType, int? CommissionTypeID, int? CommissionRangeID, int? ContractID,
    decimal? Plafond, decimal? CollectMonth, decimal? CollectDay, decimal? RetraitMonth, decimal? RetraitDay,
    string? SupervisorId, int? ZoneCollecteID, string CDETAT
);
