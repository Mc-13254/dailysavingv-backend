namespace DailySavingV.API.Entities;

public class Collector
{
    public string CollectorID { get; set; } = null!;   // e.g. CO-00001
    public string CodeUser { get; set; } = null!;
    public Users? Users { get; set; }
    public string Name { get; set; } = null!;
    public string? PhoneNumber { get; set; }

    // Agency-scoping key: every read of Collector data MUST be filtered on this
    public int AgenceID { get; set; }
    public Agence? Agence { get; set; }

    public int? ZoneCollecteID { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? DateEmploi { get; set; }
    public string? ContactType { get; set; }
    public string? CodeTerminal { get; set; }
    public decimal Plafond { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}

public class Client
{
    public string ClientID { get; set; } = null!;   // e.g. CL-00001
    public string Nom { get; set; } = null!;
    public string? Prenom { get; set; }
    public string? Sexe { get; set; }
    public string? Image { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public string? Email { get; set; }
    public string? CompanyName { get; set; }
    public string ClientType { get; set; } = "INDIVIDUAL";
    public int ClientStatusID { get; set; }
    public int NbrPersonnesACharge { get; set; }
    public int? TypeCNIID { get; set; }
    public string? NumeroCNI { get; set; }

    // Agency-scoping key
    public int AgenceID { get; set; }
    public Agence? Agence { get; set; }

    public string? CollectorID { get; set; }
    public string ValidationStatus { get; set; } = "PENDING";
    public string? CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}

public class Accounts
{
    public string AccountID { get; set; } = null!;   // e.g. CC-000001
    public string ClientID { get; set; } = null!;
    public Client? Client { get; set; }
    public string? NumCarnet { get; set; }
    public decimal Balance { get; set; }
    public bool Active { get; set; } = true;

    // Agency-scoping key
    public int AgenceID { get; set; }
    public Agence? Agence { get; set; }

    public string? CreatedBy { get; set; }
    public DateTime CreateDate { get; set; } = DateTime.UtcNow;
}

public class Contract
{
    public int ContractID { get; set; }
    public string ContractNumber { get; set; } = null!;
    public string? ClientID { get; set; }
    public int? AgenceID { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? ContractType { get; set; }
    public int? ContractTypeID { get; set; }
    public ContractType? ContractTypeRef { get; set; }
    public string? ContractDetails { get; set; }
    public string? Description { get; set; }
    public string Statut { get; set; } = "ACTIVE";
    public string? RenewalTerms { get; set; }
    public string? TerminationClause { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}

public class ContractType
{
    public int ContractTypeID { get; set; }
    public string ContractCode { get; set; } = null!;
    public string ContractName { get; set; } = null!;
    public string? ShortName { get; set; }
    public string? Description { get; set; }

    public bool AllowDailyCollection { get; set; }
    public bool AllowWeeklyCollection { get; set; }
    public bool AllowMonthlyCollection { get; set; }

    public decimal? MinimumCollectionAmount { get; set; }
    public decimal? MaximumCollectionAmount { get; set; }
    public decimal? DefaultCollectionAmount { get; set; }
    public decimal? MinimumOpeningBalance { get; set; }
    public decimal? MaximumBalance { get; set; }
    public decimal? InterestRate { get; set; }
    public int? ContractDuration { get; set; }
    public string? DurationUnit { get; set; } // Days/Weeks/Months/Years
    public decimal? PenaltyAmount { get; set; }
    public int? GracePeriod { get; set; }

    public string Statut { get; set; } = "ACTIVE";
    public string? CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedDate { get; set; }
}
