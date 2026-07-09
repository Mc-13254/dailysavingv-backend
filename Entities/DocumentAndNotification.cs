namespace DailySavingV.API.Entities;

public class DocumentRecord
{
    public int DocumentID { get; set; }
    public string EntityType { get; set; } = null!; // CLIENT / CONTRACT / COLLECTOR / LOAN / AGENCY / OTHER
    public string? EntityID { get; set; }           // e.g. ClientID, ContractID.ToString(), LoanID.ToString()
    public string FileName { get; set; } = null!;
    public string FilePath { get; set; } = null!;    // relative path under /uploads/documents/
    public string? FileType { get; set; }            // pdf/png/jpg/docx...
    public long FileSizeBytes { get; set; }
    public string? Description { get; set; }
    public string? Tags { get; set; }                // comma-separated, free text
    public int AgenceID { get; set; }
    public string UploadedBy { get; set; } = null!;
    public DateTime UploadDate { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
    public string? DeletedBy { get; set; }
    public DateTime? DeletedDate { get; set; }
}

public class Notification
{
    public int NotificationID { get; set; }
    public string CodeUser { get; set; } = null!;   // recipient
    public string Title { get; set; } = null!;
    public string Message { get; set; } = null!;
    public string Severity { get; set; } = "INFO";  // INFO / WARNING / ALERT
    public string? Link { get; set; }               // in-app route to navigate to, e.g. /security/sessions
    public bool IsRead { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? ReadDate { get; set; }
}
