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
    public string? CreatedBy { get; set; }
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;

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
    public string? Location { get; set; }
    public string? ContactInfo { get; set; }
    public int? VilleID { get; set; }
    public string CodeIMF { get; set; } = null!;
    public IMF? IMF { get; set; }
    public string Statut { get; set; } = "ACTIVE";
    public string? CreatedBy { get; set; }
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
}
