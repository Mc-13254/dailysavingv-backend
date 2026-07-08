namespace DailySavingV.API.Entities;

public class Collector
{
    public string CollectorID { get; set; } = null!;   // e.g. CO-00001
    public string CodeUser { get; set; } = null!;
    public Users? Users { get; set; }
    public string Name { get; set; } = null!;
    public string? Surname { get; set; }
    public string? PhoneNumber { get; set; }

    // Agency-scoping key: every read of Collector data MUST be filtered on this
    public int AgenceID { get; set; }
    public Agence? Agence { get; set; }

    public int? DepartmentID { get; set; }
    public Department? Department { get; set; }

    public int? ZoneCollecteID { get; set; }
    public ZoneCollecte? ZoneCollecte { get; set; }
    public bool IsActive { get; set; } = true;
    public string CDETAT { get; set; } = "ACTIVE"; // ACTIVE / INACTIVE / SUSPENDED / ONLEAVE
    public DateTime? DateEmploi { get; set; }
    public string? ContactType { get; set; } // Collector Type: Field/Senior/Supervisor Collector
    public string? CodeTerminal { get; set; }
    public decimal Plafond { get; set; }
    public decimal? Caution { get; set; }

    public int? ContractID { get; set; }
    public ContractType? Contract { get; set; }
    public int? CommissionTypeID { get; set; }
    public CommissionType? CommissionType { get; set; }
    public int? CommissionRangeID { get; set; }
    public CommissionRange? CommissionRange { get; set; }
    public string? SupervisorId { get; set; }
    public Users? Supervisor { get; set; }

    public decimal? CollectMonth { get; set; }  // monthly collection limit
    public decimal? CollectDay { get; set; }    // daily collection limit
    public decimal? RetraitMonth { get; set; }  // monthly withdrawal limit
    public decimal? RetraitDay { get; set; }    // daily withdrawal limit

    public string? UserCreate { get; set; }
    public DateTime CreateDate { get; set; } = DateTime.UtcNow;
    public string? UserValidation { get; set; }
    public DateTime? DateValidation { get; set; }
    public string? LastUserModif { get; set; }
    public DateTime? DateModification { get; set; }
    public string? LastUserSupervise { get; set; }
    public DateTime? LastDateSupervise { get; set; }
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
    public int? ZoneCollecteID { get; set; }   // NEW: zone the client belongs to (drives Collector inheritance)
    public ZoneCollecte? ZoneCollecte { get; set; }
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

public class NumberingParameter
{
    public int NumberingParameterID { get; set; }
    public string EntityName { get; set; } = null!;   // Agency/User/Collector/Client/...
    public string Prefix { get; set; } = null!;
    public string? Suffix { get; set; }
    public string? Separator { get; set; }            // "", "-", "/", ".", "_"
    public long CurrentNumber { get; set; }
    public long StartingNumber { get; set; } = 1;
    public int NumberLength { get; set; } = 6;
    public string PaddingCharacter { get; set; } = "0";
    public bool AllowReset { get; set; }
    public string? ResetFrequency { get; set; }        // Never/Daily/Monthly/Yearly
    public DateTime? NextResetDate { get; set; }
    public bool AutoIncrement { get; set; } = true;
    public int IncrementValue { get; set; } = 1;
    public string Statut { get; set; } = "ACTIVE";
    public string? CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedDate { get; set; }

    public string BuildPreview(long? number = null)
    {
        var n = (number ?? (CurrentNumber == 0 ? StartingNumber : CurrentNumber)).ToString().PadLeft(NumberLength, PaddingCharacter.Length == 1 ? PaddingCharacter[0] : '0');
        var sep = Separator ?? "";
        return $"{Prefix}{sep}{n}{(string.IsNullOrEmpty(Suffix) ? "" : sep + Suffix)}";
    }
}
