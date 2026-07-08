namespace DailySavingV.API.Entities;

/// <summary>
/// Links a Collector to a Zone (many zones per collector, one ACTIVE collector
/// per zone at a time so a Client's effective collector - inherited through
/// its Zone - is always unambiguous).
/// </summary>
public class CollectorZoneAssignment
{
    public int AssignmentID { get; set; }
    public string CollectorID { get; set; } = null!;
    public Collector? Collector { get; set; }
    public int ZoneCollecteID { get; set; }
    public ZoneCollecte? ZoneCollecte { get; set; }

    public string Status { get; set; } = "ACTIVE"; // ACTIVE | ENDED
    public DateTime AssignmentDate { get; set; } = DateTime.UtcNow;
    public DateTime? EndDate { get; set; }
    public string? AssignedBy { get; set; }
}

/// <summary>
/// Append-only audit trail. Rows are never deleted or updated, per the
/// module's business rule ("Assignment history must never be deleted").
/// </summary>
public class ZoneAssignmentHistory
{
    public long HistoryID { get; set; }
    public string CollectorID { get; set; } = null!;
    public int ZoneCollecteID { get; set; }
    public string? ClientID { get; set; }         // null when the event is zone-level, not client-level
    public string EventType { get; set; } = null!; // ZONE_ASSIGNED | ZONE_REMOVED | CLIENT_ASSIGNED | CLIENT_TRANSFERRED
    public string? FromCollectorID { get; set; }   // populated for CLIENT_TRANSFERRED
    public DateTime EventDate { get; set; } = DateTime.UtcNow;
    public string? ActionBy { get; set; }
}

/// <summary>
/// Daily/Weekly/Monthly collection target used by the Performance dashboard's
/// "Target Achievement" KPIs. No table for this existed in the base schema.
/// </summary>
public class CollectorTarget
{
    public int TargetID { get; set; }
    public string CollectorID { get; set; } = null!;
    public Collector? Collector { get; set; }
    public string PeriodType { get; set; } = "MONTHLY"; // DAILY | WEEKLY | MONTHLY
    public DateTime PeriodStart { get; set; }
    public decimal TargetAmount { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
