namespace DailySavingV.API.DTOs;

public record DocumentRowDto(
    int DocumentID, string EntityType, string? EntityID, string FileName, string? FileType,
    long FileSizeBytes, string? Description, string? Tags, string UploadedBy, DateTime UploadDate, string DownloadUrl
);

public record NotificationDto(int NotificationID, string Title, string Message, string Severity, string? Link, bool IsRead, DateTime CreatedDate);
