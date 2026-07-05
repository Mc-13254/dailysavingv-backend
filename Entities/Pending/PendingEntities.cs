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
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Adresse { get; set; }
    public string? CNI { get; set; }
    public string? Photo { get; set; }
    public int? RoleID { get; set; }
    public int? AgenceID { get; set; }
    public string? Statut { get; set; }
}

public class CollectorTMP : PendingBase
{
    public string? TargetCollectorID { get; set; }
    public string? CodeUser { get; set; }
    public string? Name { get; set; }
    public string? PhoneNumber { get; set; }
    public int? AgenceID { get; set; }
    public int? ZoneCollecteID { get; set; }
    public bool? IsActive { get; set; }
    public DateTime? DateEmploi { get; set; }
    public string? ContactType { get; set; }
    public string? CodeTerminal { get; set; }
    public decimal? Plafond { get; set; }
}

public class ClientTmp : PendingBase
{
    public string? TargetClientID { get; set; }
    public string? Nom { get; set; }
    public string? Prenom { get; set; }
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
}

public class AccountsTMP : PendingBase
{
    public string? TargetAccountID { get; set; }
    public string? ClientID { get; set; }
    public string? NumCarnet { get; set; }
    public decimal? Balance { get; set; }
    public bool? Active { get; set; }
    public int? AgenceID { get; set; }
}

public class ContractTmp : PendingBase
{
    public int? TargetContractID { get; set; }
    public string? ContractNumber { get; set; }
    public string? ClientID { get; set; }
    public int? AgenceID { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? ContractType { get; set; }
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
    public int? CommissionTypeID { get; set; }
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    public string? CalculationMethod { get; set; }
    public decimal? FixedAmount { get; set; }
    public decimal? PercentageRate { get; set; }
    public string? Currency { get; set; }
}

public class AgenceTmp : PendingBase
{
    public int? TargetAgenceID { get; set; }
    public string? CodeAgence { get; set; }
    public string? Nom { get; set; }
    public string? Location { get; set; }
    public string? ContactInfo { get; set; }
    public int? VilleID { get; set; }
    public string? CodeIMF { get; set; }
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
