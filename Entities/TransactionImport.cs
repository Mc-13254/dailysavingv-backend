namespace DailySavingV.API.Entities;

public class TransactionImportBatch
{
    public int BatchID { get; set; }
    public string FileName { get; set; } = null!;
    public string UploadedBy { get; set; } = null!;
    public DateTime UploadedDate { get; set; } = DateTime.UtcNow;
    public int TotalRows { get; set; }
    public int AgenceID { get; set; }
    public string Status { get; set; } = "PENDING"; // PENDING | PARTIALLY_APPROVED | COMPLETED

    public ICollection<TransactionImportRow> Rows { get; set; } = new List<TransactionImportRow>();
}

public class TransactionImportRow
{
    public int RowID { get; set; }
    public int BatchID { get; set; }
    public TransactionImportBatch? Batch { get; set; }
    public int RowNumber { get; set; }

    public string TransactionType { get; set; } = null!;
    public string AccountID { get; set; } = null!;
    public string? ToAccountID { get; set; }
    public string? CollectorID { get; set; }
    public decimal Montant { get; set; }
    public string? RemitterName { get; set; }
    public string? BeneficiaryName { get; set; }
    public string? RefRowLabel { get; set; } // free text from the spreadsheet to help identify the row during review

    public string Status { get; set; } = "PENDING"; // PENDING | APPROVED | REJECTED | ERROR
    public string? ErrorMessage { get; set; }
    public long? TransactionID { get; set; } // set once approved and actually posted
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovalDate { get; set; }
}
