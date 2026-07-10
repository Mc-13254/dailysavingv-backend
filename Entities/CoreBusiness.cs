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
    public string? MiddleName { get; set; }
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

    // ---- Step 1: Personal information ----
    public DateTime? DateOfBirth { get; set; }
    public string? PlaceOfBirth { get; set; }
    public string? Nationality { get; set; }
    public string? MaritalStatus { get; set; }
    public string? Profession { get; set; }
    public string? Occupation { get; set; }
    public string? Employer { get; set; }
    public string? EducationLevel { get; set; }
    public decimal? MonthlyIncome { get; set; }

    // ---- Step 2: Contact information ----
    public string? SecondaryPhone { get; set; }
    public string? WhatsApp { get; set; }
    public string? Country { get; set; }
    public string? City { get; set; }
    public string? District { get; set; }
    public string? Neighborhood { get; set; }
    public string? Street { get; set; }
    public string? HouseNumber { get; set; }
    public string? PostalCode { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }

    // ---- Step 3: Identification (KYC) ----
    public DateTime? NationalIDIssueDate { get; set; }
    public DateTime? NationalIDExpiryDate { get; set; }
    public string? PassportNumber { get; set; }
    public string? DriverLicenseNumber { get; set; }
    public string? TaxIdentificationNumber { get; set; }
    public string? SocialSecurityNumber { get; set; }
    public string? DocumentType { get; set; }
    public string? IssuedBy { get; set; }

    // ---- Step 4: Documents (stored as relative URLs under /uploads) ----
    public string? NationalIDFrontUrl { get; set; }
    public string? NationalIDBackUrl { get; set; }
    public string? PassportUrl { get; set; }
    public string? ProofOfAddressUrl { get; set; }
    public string? SignatureUrl { get; set; }

    // ---- Step 5: Business information ----
    public string? BusinessName { get; set; }
    public string? BusinessAddress { get; set; }
    public string? BusinessType { get; set; }
    public int? YearsInBusiness { get; set; }
    public decimal? MonthlyRevenue { get; set; }
    public decimal? MonthlyExpenses { get; set; }

    // ---- Step 6: Banking information (Institution/Agency/Collector already modeled) ----
    public string ClientCategory { get; set; } = "INDIVIDUAL"; // Individual/Business/VIP/Association/NGO
    public string? AccountOfficer { get; set; } // CodeUser of the relationship manager

    // ---- Step 7: Emergency contact ----
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactRelationship { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? EmergencyContactAddress { get; set; }

    // ---- Step 8: Guarantor ----
    public string? GuarantorName { get; set; }
    public string? GuarantorRelationship { get; set; }
    public string? GuarantorPhone { get; set; }
    public string? GuarantorOccupation { get; set; }
    public string? GuarantorEmployer { get; set; }
    public string? GuarantorAddress { get; set; }

    // ---- Step 9: Risk & compliance ----
    public string RiskLevel { get; set; } = "LOW"; // LOW/MEDIUM/HIGH
    public bool IsPoliticallyExposed { get; set; }
    public bool IsBlacklisted { get; set; }
    public string AMLStatus { get; set; } = "PENDING"; // VERIFIED/PENDING/REJECTED

    // Agency-scoping key
    public int AgenceID { get; set; }
    public Agence? Agence { get; set; }

    public string? CollectorID { get; set; }
    public int? ZoneCollecteID { get; set; }   // NEW: zone the client belongs to (drives Collector inheritance)
    public ZoneCollecte? ZoneCollecte { get; set; }
    public string ValidationStatus { get; set; } = "PENDING";
    public string? RejectionReason { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public string? ValidatedBy { get; set; }
    public DateTime? ValidationDate { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedDate { get; set; }
}

public class Accounts
{
    public string AccountID { get; set; } = null!;   // e.g. CC-000001
    public string ClientID { get; set; } = null!;
    public Client? Client { get; set; }
    public string? NumCarnet { get; set; }
    public int? ContractID { get; set; }
    public Contract? Contract { get; set; }
    public string? CollectorID { get; set; }

    public string AccountType { get; set; } = "DAILY_SAVING"; // DAILY_SAVING/BUSINESS_SAVING/VIP_SAVING/ASSOCIATION
    public string Currency { get; set; } = "XAF";

    public decimal OpeningBalance { get; set; }
    public decimal Balance { get; set; }             // = CurrentBalance
    public decimal AvailableBalance { get; set; }
    public decimal BlockedBalance { get; set; }
    public decimal? MinimumBalance { get; set; }
    public decimal? MaximumBalance { get; set; }
    public decimal? DailyDepositLimit { get; set; }
    public decimal? DailyWithdrawalLimit { get; set; }
    public decimal? DailyTransactionLimit { get; set; }
    public bool OverdraftAllowed { get; set; }
    public decimal? OverdraftLimit { get; set; }

    public string Status { get; set; } = "PENDING"; // PENDING/ACTIVE/FROZEN/CLOSED/DORMANT

    // Member/GL account fields — only meaningful when AccountType == "MEMBER_GL".
    // Auto-created alongside a client's first regular savings account: joining
    // the microfinance as a member/investor, earning annual interest on their
    // GL balance, with withdrawals gated behind a minimum threshold.
    public decimal? AnnualInterestRate { get; set; }   // e.g. 3 = 3%/year
    public decimal? WithdrawalThreshold { get; set; }  // e.g. 800000 — balance must be at least this to withdraw at all
    public DateTime? LastInterestAppliedDate { get; set; }
    public string? FreezeReason { get; set; }
    public string? CloseReason { get; set; }
    public DateTime? ClosingDate { get; set; }
    public bool Active { get; set; } = true;         // kept for backward compat: derived from Status == ACTIVE

    // Agency-scoping key
    public int AgenceID { get; set; }
    public Agence? Agence { get; set; }

    public string? CreatedBy { get; set; }
    public DateTime CreateDate { get; set; } = DateTime.UtcNow;
    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedDate { get; set; }
}

public class Contract
{
    public int ContractID { get; set; }
    public string ContractNumber { get; set; } = null!;
    public string? ClientID { get; set; }
    public int? AgenceID { get; set; }
    public string? CollectorID { get; set; }
    public int? CommissionTypeID { get; set; }
    public CommissionType? CommissionType { get; set; }
    public int? CommissionRangeID { get; set; }
    public CommissionRange? CommissionRange { get; set; }

    public string CollectionFrequency { get; set; } = "DAILY"; // DAILY/WEEKLY/MONTHLY
    public string? CollectionDay { get; set; }                  // e.g. "Monday" for weekly/monthly

    public decimal? OpeningDeposit { get; set; }
    public decimal? MinimumBalance { get; set; }
    public decimal? MaximumBalance { get; set; }
    public string? PenaltyRules { get; set; }
    public int? GracePeriod { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? ContractType { get; set; }
    public int? ContractTypeID { get; set; }
    public ContractType? ContractTypeRef { get; set; }
    public string? ContractDetails { get; set; }
    public string? Description { get; set; }
    public string Statut { get; set; } = "ACTIVE"; // ACTIVE/TERMINATED
    public string? TerminationReason { get; set; } // Completed/CustomerRequest/Violation/Other
    public DateTime? TerminationDate { get; set; }
    public string? RenewalTerms { get; set; }
    public string? TerminationClause { get; set; }

    public string? PdfPath { get; set; }
    public bool CustomerSigned { get; set; }
    public bool OfficerSigned { get; set; }

    public string? CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedDate { get; set; }
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
