namespace DailySavingV.API.Entities.Pending;

public enum ActionType { CREATE, UPDATE, DELETE }
public enum PendingStatus { PENDING, APPROVED, REJECTED }

/// <summary>
/// Common workflow-tracking fields shared by every *Tmp pending table.
/// Business fields are declared per-entity below (nullable, mirroring production).
/// </summary>
public abstract class PendingBase
{
    public int PendingID { get; set; }
    public ActionType ActionType { get; set; }
    public PendingStatus PendingStatus { get; set; } = PendingStatus.PENDING;
    public string RequestUser { get; set; } = null!;
    public DateTime RequestDate { get; set; } = DateTime.UtcNow;
    public string? ValidationUser { get; set; }
    public DateTime? ValidationDate { get; set; }
    public string? RejectionReason { get; set; }
    public string? PreviousData { get; set; }   // JSON snapshot before change
    public string? NewData { get; set; }        // JSON snapshot of requested change
}

public class UsersTmp : PendingBase
{
    public string? TargetCodeUser { get; set; }
    public string? Username { get; set; }
    public string? PasswordHash { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Adresse { get; set; }
    public string? CNI { get; set; }
    public string? Photo { get; set; }
    public int? RoleID { get; set; }
    public int? AgenceID { get; set; }
    public string? Statut { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Nationality { get; set; }
    public string? MaritalStatus { get; set; }
    public string? TypeUser { get; set; }
    public string? Department { get; set; }
    public string? JobTitle { get; set; }
    public int? PaysID { get; set; }
    public int? VilleID { get; set; }
    public decimal? DebitMax { get; set; }
    public decimal? CreditMax { get; set; }
    public decimal? ValidationMax { get; set; }
    public decimal? PlafondCollect { get; set; }
    public decimal? Caution { get; set; }
    public string? Signe { get; set; }
}

public class CollectorTMP : PendingBase
{
    public string? TargetCollectorID { get; set; }
    public string? CodeUser { get; set; }
    public string? Name { get; set; }
    public string? Surname { get; set; }
    public string? PhoneNumber { get; set; }
    public int? AgenceID { get; set; }
    public int? DepartmentID { get; set; }
    public int? ZoneCollecteID { get; set; }
    public bool? IsActive { get; set; }
    public string? CDETAT { get; set; }
    public DateTime? DateEmploi { get; set; }
    public string? ContactType { get; set; }
    public string? CodeTerminal { get; set; }
    public decimal? Plafond { get; set; }
    public decimal? Caution { get; set; }
    public int? ContractID { get; set; }
    public int? CommissionTypeID { get; set; }
    public int? CommissionRangeID { get; set; }
    public string? SupervisorId { get; set; }
    public decimal? CollectMonth { get; set; }
    public decimal? CollectDay { get; set; }
    public decimal? RetraitMonth { get; set; }
    public decimal? RetraitDay { get; set; }
}

public class ClientTmp : PendingBase
{
    public string? TargetClientID { get; set; }
    public string? Nom { get; set; }
    public string? Prenom { get; set; }
    public string? MiddleName { get; set; }
    public string? Sexe { get; set; }
    public string? Image { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public string? Email { get; set; }
    public string? CompanyName { get; set; }
    public string? ClientType { get; set; }
    public int? ClientStatusID { get; set; }
    public int? NbrPersonnesACharge { get; set; }
    public int? TypeCNIID { get; set; }
    public string? NumeroCNI { get; set; }
    public int? AgenceID { get; set; }
    public string? CollectorID { get; set; }

    public DateTime? DateOfBirth { get; set; }
    public string? PlaceOfBirth { get; set; }
    public string? Nationality { get; set; }
    public string? MaritalStatus { get; set; }
    public string? Profession { get; set; }
    public string? Occupation { get; set; }
    public string? Employer { get; set; }
    public string? EducationLevel { get; set; }
    public decimal? MonthlyIncome { get; set; }

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

    public DateTime? NationalIDIssueDate { get; set; }
    public DateTime? NationalIDExpiryDate { get; set; }
    public string? PassportNumber { get; set; }
    public string? DriverLicenseNumber { get; set; }
    public string? TaxIdentificationNumber { get; set; }
    public string? SocialSecurityNumber { get; set; }
    public string? DocumentType { get; set; }
    public string? IssuedBy { get; set; }

    public string? NationalIDFrontUrl { get; set; }
    public string? NationalIDBackUrl { get; set; }
    public string? PassportUrl { get; set; }
    public string? ProofOfAddressUrl { get; set; }
    public string? SignatureUrl { get; set; }

    public string? BusinessName { get; set; }
    public string? BusinessAddress { get; set; }
    public string? BusinessType { get; set; }
    public int? YearsInBusiness { get; set; }
    public decimal? MonthlyRevenue { get; set; }
    public decimal? MonthlyExpenses { get; set; }

    public string? ClientCategory { get; set; }
    public string? AccountOfficer { get; set; }

    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactRelationship { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? EmergencyContactAddress { get; set; }

    public string? GuarantorName { get; set; }
    public string? GuarantorRelationship { get; set; }
    public string? GuarantorPhone { get; set; }
    public string? GuarantorOccupation { get; set; }
    public string? GuarantorEmployer { get; set; }
    public string? GuarantorAddress { get; set; }

    public string? RiskLevel { get; set; }
    public bool? IsPoliticallyExposed { get; set; }
    public bool? IsBlacklisted { get; set; }
    public string? AMLStatus { get; set; }
}

public class AccountsTMP : PendingBase
{
    public string? TargetAccountID { get; set; }
    public string? ClientID { get; set; }
    public string? NumCarnet { get; set; }
    public int? ContractID { get; set; }
    public string? CollectorID { get; set; }
    public string? AccountType { get; set; }
    public string? Currency { get; set; }
    public decimal? OpeningBalance { get; set; }
    public decimal? Balance { get; set; }
    public decimal? AvailableBalance { get; set; }
    public decimal? BlockedBalance { get; set; }
    public decimal? MinimumBalance { get; set; }
    public decimal? MaximumBalance { get; set; }
    public decimal? DailyDepositLimit { get; set; }
    public decimal? DailyWithdrawalLimit { get; set; }
    public decimal? DailyTransactionLimit { get; set; }
    public bool? OverdraftAllowed { get; set; }
    public decimal? OverdraftLimit { get; set; }
    public string? Status { get; set; }
    public bool? Active { get; set; }
    public int? AgenceID { get; set; }
}

public class ContractTmp : PendingBase
{
    public int? TargetContractID { get; set; }
    public string? ContractNumber { get; set; }
    public string? ClientID { get; set; }
    public int? AgenceID { get; set; }
    public string? CollectorID { get; set; }
    public int? CommissionTypeID { get; set; }
    public int? CommissionRangeID { get; set; }
    public string? CollectionFrequency { get; set; }
    public string? CollectionDay { get; set; }
    public decimal? OpeningDeposit { get; set; }
    public decimal? MinimumBalance { get; set; }
    public decimal? MaximumBalance { get; set; }
    public string? PenaltyRules { get; set; }
    public int? GracePeriod { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? ContractType { get; set; }
    public int? ContractTypeID { get; set; }
    public string? ContractDetails { get; set; }
    public string? Description { get; set; }
    public string? Statut { get; set; }
    public string? RenewalTerms { get; set; }
    public string? TerminationClause { get; set; }
}

public class CommissionTypeTmp : PendingBase
{
    public int? TargetCommissionTypeID { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Statut { get; set; }
}

public class CommissionRangeTmp : PendingBase
{
    public int? TargetCommissionRangeID { get; set; }
    public string? Description { get; set; }
    public int? CommissionTypeID { get; set; }
    public string? CodeU { get; set; }
    public decimal? Inf { get; set; }
    public decimal? Sup { get; set; }
    public string? CalculationMethod { get; set; }
    public decimal? Fixe { get; set; }
    public decimal? TAUX { get; set; }
    public decimal? Minimum { get; set; }
    public decimal? Maximum { get; set; }
}

public class AgenceTmp : PendingBase
{
    public int? TargetAgenceID { get; set; }
    public string? CodeAgence { get; set; }
    public string? Nom { get; set; }
    public string? ShortName { get; set; }
    public string? Description { get; set; }
    public string? LogoBase64 { get; set; }
    public string? PrimaryPhone { get; set; }
    public string? SecondaryPhone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public int? PaysID { get; set; }
    public int? VilleID { get; set; }
    public string? Address { get; set; }
    public string? PostalCode { get; set; }
    public string? Location { get; set; }
    public string? CodeIMF { get; set; }
    public string? ManagerId { get; set; }
    public DateTime? OpeningDate { get; set; }
    public string? ContactInfo { get; set; }
    public string? Statut { get; set; }
}

public class RoleTmp : PendingBase
{
    public int? TargetRoleID { get; set; }
    public string? Code { get; set; }
    public string? Libelle { get; set; }
    public string? Description { get; set; }
    public bool? Statut { get; set; }
}

public class DepartmentTmp : PendingBase
{
    public int? TargetDepartmentID { get; set; }
    public string? DepartmentName { get; set; }
    public string? ShortName { get; set; }
    public string? Description { get; set; }
    public string? CodeIMF { get; set; }
    public int? AgenceID { get; set; }
    public string? ManagerId { get; set; }
    public string? Statut { get; set; }
}

public class ContractTypeTmp : PendingBase
{
    public int? TargetContractTypeID { get; set; }
    public string? ContractName { get; set; }
    public string? ShortName { get; set; }
    public string? Description { get; set; }
    public bool? AllowDailyCollection { get; set; }
    public bool? AllowWeeklyCollection { get; set; }
    public bool? AllowMonthlyCollection { get; set; }
    public decimal? MinimumCollectionAmount { get; set; }
    public decimal? MaximumCollectionAmount { get; set; }
    public decimal? DefaultCollectionAmount { get; set; }
    public decimal? MinimumOpeningBalance { get; set; }
    public decimal? MaximumBalance { get; set; }
    public decimal? InterestRate { get; set; }
    public int? ContractDuration { get; set; }
    public string? DurationUnit { get; set; }
    public decimal? PenaltyAmount { get; set; }
    public int? GracePeriod { get; set; }
    public string? Statut { get; set; }
}

public class IMFTmp : PendingBase
{
    public string? TargetCodeIMF { get; set; }
    public string? Libelle { get; set; }
    public string? Statut { get; set; }
    public decimal? TauxTaxe { get; set; }
    public bool? AssujettiTaxe { get; set; }
    public string? SuffixeCompte { get; set; }
    public string? PrefixeCompte { get; set; }
    public int? TailleCompte { get; set; }
    public bool? CalculCommission { get; set; }

    public string? ShortName { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? TaxNumber { get; set; }
    public string? Description { get; set; }
    public string? LogoBase64 { get; set; }
    public string? PrimaryPhone { get; set; }
    public string? SecondaryPhone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public int? PaysID { get; set; }
    public int? VilleID { get; set; }
    public string? Address { get; set; }
    public string? PostalCode { get; set; }
    public string? CurrencyCode { get; set; }
    public string? Language { get; set; }
    public string? Timezone { get; set; }
}

public class TransactionsTMP : PendingBase
{
    public long? TargetTransactionID { get; set; }
    public string? TransactionType { get; set; }
    public string? AccountID { get; set; }
    public string? ClientID { get; set; }
    public string? CollectorID { get; set; }
    public int? AgenceID { get; set; }
    public decimal? Montant { get; set; }
}
