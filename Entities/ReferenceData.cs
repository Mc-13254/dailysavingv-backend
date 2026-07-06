namespace DailySavingV.API.Entities;

public class Pays
{
    public int PaysID { get; set; }
    public string Code { get; set; } = null!;
    public string Nom { get; set; } = null!;
    public bool Statut { get; set; } = true;
    public string? CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public ICollection<Region> Regions { get; set; } = new List<Region>();
}

public class Region
{
    public int RegionID { get; set; }
    public string Nom { get; set; } = null!;
    public int PaysID { get; set; }
    public Pays? Pays { get; set; }
    public bool Statut { get; set; } = true;
    public string? CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public ICollection<Ville> Villes { get; set; } = new List<Ville>();
}

public class Ville
{
    public int VilleID { get; set; }
    public string Nom { get; set; } = null!;
    public int RegionID { get; set; }
    public Region? Region { get; set; }
    public bool Statut { get; set; } = true;
    public string? CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}

public class TypeCNI
{
    public int TypeCNIID { get; set; }
    public string Code { get; set; } = null!;
    public string Libelle { get; set; } = null!;
    public bool Statut { get; set; } = true;
}

public class ClientStatus
{
    public int ClientStatusID { get; set; }
    public string Code { get; set; } = null!;
    public string Libelle { get; set; } = null!;
}

public class ZoneCollecte
{
    public int ZoneCollecteID { get; set; }
    public string Code { get; set; } = null!;
    public string? Libelle { get; set; }
    public int? VilleID { get; set; }
    public bool Statut { get; set; } = true;
    public string? UserCreate { get; set; }
    public DateTime CreateDate { get; set; } = DateTime.UtcNow;
}

public class Currency
{
    public string CurrencyCode { get; set; } = null!;
    public string Nom { get; set; } = null!;
    public string? Symbole { get; set; }
    public bool Statut { get; set; } = true;
}

public class Language
{
    public string LanguageCode { get; set; } = null!;
    public string Nom { get; set; } = null!;
    public bool Statut { get; set; } = true;
}

public class TimeZoneRef
{
    public int TimeZoneID { get; set; }
    public string Code { get; set; } = null!;
    public string Label { get; set; } = null!;
    public string? UtcOffset { get; set; }
    public bool Statut { get; set; } = true;
}
