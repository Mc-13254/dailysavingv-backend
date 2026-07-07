namespace DailySavingV.API.DTOs;

public record NumberingParameterDto(
    int NumberingParameterID, string EntityName, string Prefix, string? Suffix, string? Separator,
    long CurrentNumber, long StartingNumber, int NumberLength, string PaddingCharacter,
    bool AllowReset, string? ResetFrequency, DateTime? NextResetDate,
    bool AutoIncrement, int IncrementValue, string Preview, string Statut,
    string? CreatedBy, DateTime CreatedDate, string? UpdatedBy, DateTime? UpdatedDate
);

public record CreateNumberingParameterRequest(
    string EntityName, string Prefix, string? Suffix, string? Separator,
    long StartingNumber, int NumberLength, string PaddingCharacter,
    bool AllowReset, string? ResetFrequency, DateTime? NextResetDate,
    bool AutoIncrement, int IncrementValue
);

public record UpdateNumberingParameterRequest(
    string Prefix, string? Suffix, string? Separator,
    int NumberLength, string PaddingCharacter,
    bool AllowReset, string? ResetFrequency, DateTime? NextResetDate,
    bool AutoIncrement, int IncrementValue, string Statut
);

public record PreviewRequest(string Prefix, string? Suffix, string? Separator, int NumberLength, string PaddingCharacter, long? SampleNumber);
