namespace DailySavingV.API.Entities;

public class IMF
{
    public string CodeIMF { get; set; } = null!;
    public string Libelle { get; set; } = null!;
    public string Statut { get; set; } = "ACTIVE";
    public decimal TauxTaxe { get; set; }
    public bool AssujettiTaxe { get; set; }
    public string? SuffixeCompte { get; set; }
    public string? PrefixeCompte { get; set; }
    public int TailleCompte { get; set; } = 10;
    public bool CalculCommission { get; set; } = true;

    // General information
    public string? ShortName { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? TaxNumber { get; set; }
    public string? Description { get; set; }
    public string? LogoBase64 { get; set; }

    // Contact information
    public string? PrimaryPhone { get; set; }
    public string? SecondaryPhone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }

    // Location
    public int? PaysID { get; set; }
    public Pays? Pays { get; set; }
    public int? VilleID { get; set; }
    public Ville? Ville { get; set; }
    public string? Address { get; set; }
    public string? PostalCode { get; set; }

    // Business settings
    public string? CurrencyCode { get; set; }
    public string? Language { get; set; }
    public string? Timezone { get; set; }

    // Audit
    public string? CreatedBy { get; set; }
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;
    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedDate { get; set; }

    public ICollection<Agence> Agences { get; set; } = new List<Agence>();
}

public class ConfigSyst
{
    public int ConfigSystID { get; set; }
    public string Cle { get; set; } = null!;
    public string? Valeur { get; set; }
    public string? Description { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}

public class Agence
{
    public int AgenceID { get; set; }
    public string CodeAgence { get; set; } = null!;
    public string Nom { get; set; } = null!;
    public string? ShortName { get; set; }
    public string? Description { get; set; }
    public string? LogoBase64 { get; set; }

    public string? PrimaryPhone { get; set; }
    public string? SecondaryPhone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }

    public int? PaysID { get; set; }
    public Pays? Pays { get; set; }
    public int? VilleID { get; set; }
    public Ville? Ville { get; set; }
    public string? Address { get; set; }
    public string? PostalCode { get; set; }
    public string? Location { get; set; } // legacy free-text field, kept for compatibility

    public string CodeIMF { get; set; } = null!;
    public IMF? IMF { get; set; }
    public string? ManagerId { get; set; }
    public Users? Manager { get; set; }
    public DateTime? OpeningDate { get; set; }

    public string? ContactInfo { get; set; } // legacy free-text field, kept for compatibility
    public string Statut { get; set; } = "ACTIVE";
    public string? CreatedBy { get; set; }
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedDate { get; set; }
}

public class Department
{
    public int DepartmentID { get; set; }
    public string DepartmentCode { get; set; } = null!;
    public string DepartmentName { get; set; } = null!;
    public string? ShortName { get; set; }
    public string? Description { get; set; }
    public string CodeIMF { get; set; } = null!;
    public IMF? IMF { get; set; }
    public int AgenceID { get; set; }
    public Agence? Agence { get; set; }
    public string? ManagerId { get; set; }
    public Users? Manager { get; set; }
    public string Statut { get; set; } = "ACTIVE";
    public string? CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedDate { get; set; }
}
