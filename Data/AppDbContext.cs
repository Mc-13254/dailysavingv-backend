using DailySavingV.API.Entities;
using DailySavingV.API.Entities.Pending;
using DailySavingV.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DailySavingV.API.Data;

public class AppDbContext : DbContext
{
    private readonly ICurrentUserService _currentUser;

    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUserService currentUser)
        : base(options)
    {
        _currentUser = currentUser;
    }

    // Reference data
    public DbSet<Pays> Pays => Set<Pays>();
    public DbSet<Region> Regions => Set<Region>();
    public DbSet<Ville> Villes => Set<Ville>();
    public DbSet<TypeCNI> TypeCNIs => Set<TypeCNI>();
    public DbSet<ClientStatus> ClientStatuses => Set<ClientStatus>();
    public DbSet<ZoneCollecte> ZoneCollectes => Set<ZoneCollecte>();
    public DbSet<Currency> Currencies => Set<Currency>();
    public DbSet<Language> Languages => Set<Language>();
    public DbSet<TimeZoneRef> TimeZones => Set<TimeZoneRef>();

    // Institution
    public DbSet<IMF> IMFs => Set<IMF>();
    public DbSet<ConfigSyst> ConfigSysts => Set<ConfigSyst>();
    public DbSet<Agence> Agences => Set<Agence>();

    // RBAC
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Fonctionnalite> Fonctionnalites => Set<Fonctionnalite>();
    public DbSet<Habilitation> Habilitations => Set<Habilitation>();
    public DbSet<Habiliter> Habiliters => Set<Habiliter>();
    public DbSet<RoleFonctionnalite> RoleFonctionnalites => Set<RoleFonctionnalite>();

    // Users
    public DbSet<Users> Users => Set<Users>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    // Core business (agency-scoped)
    public DbSet<Collector> Collectors => Set<Collector>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Accounts> Accounts => Set<Accounts>();
    public DbSet<Contract> Contracts => Set<Contract>();

    // Commission engine
    public DbSet<CommissionType> CommissionTypes => Set<CommissionType>();
    public DbSet<CommissionRange> CommissionRanges => Set<CommissionRange>();

    // Transactions & audit
    public DbSet<Transactions> Transactions => Set<Transactions>();
    public DbSet<HistTransactions> HistTransactions => Set<HistTransactions>();
    public DbSet<HistCalculComis> HistCalculComis => Set<HistCalculComis>();
    public DbSet<Activite> Activites => Set<Activite>();

    // Pending / Maker-Checker
    public DbSet<UsersTmp> UsersTmps => Set<UsersTmp>();
    public DbSet<CollectorTMP> CollectorTMPs => Set<CollectorTMP>();
    public DbSet<ClientTmp> ClientTmps => Set<ClientTmp>();
    public DbSet<AccountsTMP> AccountsTMPs => Set<AccountsTMP>();
    public DbSet<ContractTmp> ContractTmps => Set<ContractTmp>();
    public DbSet<CommissionTypeTmp> CommissionTypeTmps => Set<CommissionTypeTmp>();
    public DbSet<CommissionRangeTmp> CommissionRangeTmps => Set<CommissionRangeTmp>();
    public DbSet<AgenceTmp> AgenceTmps => Set<AgenceTmp>();
    public DbSet<IMFTmp> IMFTmps => Set<IMFTmp>();
    public DbSet<TransactionsTMP> TransactionsTMPs => Set<TransactionsTMP>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ---- Table name mappings ----
        // EF Core defaults to using the DbSet property name (often pluralized,
        // e.g. "Roles") as the table name. The SQL script actually created most
        // tables using their singular business-entity name (e.g. "Role"),
        // which caused "Invalid object name 'Roles'" errors at runtime.
        // Every mismatch below is mapped explicitly to the real table name.
        modelBuilder.Entity<Region>().ToTable("Region");
        modelBuilder.Entity<Ville>().ToTable("Ville");
        modelBuilder.Entity<TypeCNI>().ToTable("TypeCNI");
        modelBuilder.Entity<ClientStatus>().ToTable("ClientStatus");
        modelBuilder.Entity<ZoneCollecte>().ToTable("ZoneCollecte");
        modelBuilder.Entity<IMF>().ToTable("IMF");
        modelBuilder.Entity<ConfigSyst>().ToTable("ConfigSyst");
        modelBuilder.Entity<Agence>().ToTable("Agence");
        modelBuilder.Entity<Role>().ToTable("Role");
        modelBuilder.Entity<Fonctionnalite>().ToTable("Fonctionnalite");
        modelBuilder.Entity<Habilitation>().ToTable("Habilitation");
        modelBuilder.Entity<Habiliter>().ToTable("Habiliter");
        modelBuilder.Entity<RoleFonctionnalite>().ToTable("RoleFonctionnalite");
        modelBuilder.Entity<Collector>().ToTable("Collector");
        modelBuilder.Entity<Client>().ToTable("Client");
        modelBuilder.Entity<Contract>().ToTable("Contract");
        modelBuilder.Entity<CommissionType>().ToTable("CommissionType");
        modelBuilder.Entity<CommissionRange>().ToTable("CommissionRange");
        modelBuilder.Entity<Activite>().ToTable("Activite");
        modelBuilder.Entity<UsersTmp>().ToTable("UsersTmp");
        modelBuilder.Entity<CollectorTMP>().ToTable("CollectorTMP");
        modelBuilder.Entity<ClientTmp>().ToTable("ClientTmp");
        modelBuilder.Entity<AccountsTMP>().ToTable("AccountsTMP");
        modelBuilder.Entity<ContractTmp>().ToTable("ContractTmp");
        modelBuilder.Entity<CommissionTypeTmp>().ToTable("CommissionTypeTmp");
        modelBuilder.Entity<CommissionRangeTmp>().ToTable("CommissionRangeTmp");
        modelBuilder.Entity<AgenceTmp>().ToTable("AgenceTmp");
        modelBuilder.Entity<IMFTmp>().ToTable("IMFTmp");
        modelBuilder.Entity<TransactionsTMP>().ToTable("TransactionsTMP");

        // ---- Keys ----
        modelBuilder.Entity<IMF>().HasKey(x => x.CodeIMF);
        modelBuilder.Entity<Currency>().HasKey(x => x.CurrencyCode);
        modelBuilder.Entity<Currency>().ToTable("Currency");
        modelBuilder.Entity<Language>().HasKey(x => x.LanguageCode);
        modelBuilder.Entity<Language>().ToTable("Language");
        modelBuilder.Entity<TimeZoneRef>().ToTable("TimeZoneRef");
        modelBuilder.Entity<Users>().HasKey(x => x.CodeUser);
        modelBuilder.Entity<Collector>().HasKey(x => x.CollectorID);
        modelBuilder.Entity<Client>().HasKey(x => x.ClientID);
        modelBuilder.Entity<Accounts>().HasKey(x => x.AccountID);
        modelBuilder.Entity<Habiliter>().HasKey(x => new { x.RoleID, x.FonctionnaliteID, x.HabilitationID });
        modelBuilder.Entity<RoleFonctionnalite>().HasKey(x => new { x.RoleID, x.FonctionnaliteID });

        // The properties below don't match EF Core's automatic key-discovery
        // convention (property name must be "Id" or "{ClassName}Id") because
        // the class names are plural or abbreviated differently from the
        // legacy field names (e.g. class "Transactions" + property
        // "TransactionID", class "RefreshToken" + property "TokenID").
        // Every one of these MUST be declared explicitly or EF Core throws
        // "requires a primary key to be defined" at startup.
        modelBuilder.Entity<Transactions>().HasKey(x => x.TransactionID);
        modelBuilder.Entity<HistTransactions>().HasKey(x => x.HistTransactionID);
        modelBuilder.Entity<RefreshToken>().HasKey(x => x.TokenID);

        // Pending (Maker-Checker) tables all use "PendingID" as their key,
        // which never matches the "{ClassName}Id" convention either.
        modelBuilder.Entity<UsersTmp>().HasKey(x => x.PendingID);
        modelBuilder.Entity<CollectorTMP>().HasKey(x => x.PendingID);
        modelBuilder.Entity<ClientTmp>().HasKey(x => x.PendingID);
        modelBuilder.Entity<AccountsTMP>().HasKey(x => x.PendingID);
        modelBuilder.Entity<ContractTmp>().HasKey(x => x.PendingID);
        modelBuilder.Entity<CommissionTypeTmp>().HasKey(x => x.PendingID);
        modelBuilder.Entity<CommissionRangeTmp>().HasKey(x => x.PendingID);
        modelBuilder.Entity<AgenceTmp>().HasKey(x => x.PendingID);
        modelBuilder.Entity<IMFTmp>().HasKey(x => x.PendingID);
        modelBuilder.Entity<TransactionsTMP>().HasKey(x => x.PendingID);

        // =====================================================================
        // RELATIONSHIPS: explicit FK configuration.
        // Without this, EF Core's convention sometimes fails to associate an
        // existing scalar FK property (e.g. Agence.CodeIMF) with its navigation
        // property (Agence.IMF) - especially when the FK is a string business
        // key rather than a surrogate int, or when the navigation is nullable
        // but the scalar FK is not. When that happens, EF silently creates an
        // extra *shadow* column (e.g. "IMFCodeIMF") instead of reusing the real
        // one, causing "Invalid column name" at runtime. Configuring every
        // relationship explicitly removes all ambiguity.
        // =====================================================================
        modelBuilder.Entity<Region>()
            .HasOne(x => x.Pays).WithMany(x => x.Regions)
            .HasForeignKey(x => x.PaysID);

        modelBuilder.Entity<Ville>()
            .HasOne(x => x.Region).WithMany(x => x.Villes)
            .HasForeignKey(x => x.RegionID);

        modelBuilder.Entity<Agence>()
            .HasOne(x => x.IMF).WithMany(x => x.Agences)
            .HasForeignKey(x => x.CodeIMF)
            .IsRequired();

        modelBuilder.Entity<Users>()
            .HasOne(x => x.Role).WithMany()
            .HasForeignKey(x => x.RoleID)
            .IsRequired();

        modelBuilder.Entity<Users>()
            .HasOne(x => x.Agence).WithMany()
            .HasForeignKey(x => x.AgenceID)
            .IsRequired(false);

        modelBuilder.Entity<Collector>()
            .HasOne(x => x.Users).WithMany()
            .HasForeignKey(x => x.CodeUser)
            .IsRequired();

        modelBuilder.Entity<Collector>()
            .HasOne(x => x.Agence).WithMany()
            .HasForeignKey(x => x.AgenceID)
            .IsRequired();

        modelBuilder.Entity<Client>()
            .HasOne(x => x.Agence).WithMany()
            .HasForeignKey(x => x.AgenceID)
            .IsRequired();

        modelBuilder.Entity<Accounts>()
            .HasOne(x => x.Client).WithMany()
            .HasForeignKey(x => x.ClientID)
            .IsRequired();

        modelBuilder.Entity<Accounts>()
            .HasOne(x => x.Agence).WithMany()
            .HasForeignKey(x => x.AgenceID)
            .IsRequired();

        modelBuilder.Entity<Transactions>()
            .HasOne(x => x.Account).WithMany()
            .HasForeignKey(x => x.AccountID)
            .IsRequired();

        modelBuilder.Entity<Transactions>()
            .HasOne(x => x.Client).WithMany()
            .HasForeignKey(x => x.ClientID)
            .IsRequired();

        modelBuilder.Entity<Transactions>()
            .HasOne(x => x.Agence).WithMany()
            .HasForeignKey(x => x.AgenceID)
            .IsRequired();

        modelBuilder.Entity<CommissionRange>()
            .HasOne(x => x.CommissionType).WithMany(x => x.Ranges)
            .HasForeignKey(x => x.CommissionTypeID)
            .IsRequired();

        modelBuilder.Entity<IMF>()
            .HasOne(x => x.Pays).WithMany()
            .HasForeignKey(x => x.PaysID)
            .IsRequired(false);

        modelBuilder.Entity<IMF>()
            .HasOne(x => x.Ville).WithMany()
            .HasForeignKey(x => x.VilleID)
            .IsRequired(false);

        // ---- Enum -> string conversions (readable values in DB, matches CHECK constraints) ----
        modelBuilder.Entity<CommissionRange>()
            .Property(x => x.CalculationMethod)
            .HasConversion<string>();

        modelBuilder.Entity<Transactions>()
            .Property(x => x.TransactionType)
            .HasConversion<string>();

        // ---- Decimal precision (match DECIMAL(18,2) / (5,2) from the SQL schema) ----
        modelBuilder.Entity<CommissionRange>().Property(x => x.MinAmount).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<CommissionRange>().Property(x => x.MaxAmount).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<CommissionRange>().Property(x => x.FixedAmount).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<CommissionRange>().Property(x => x.PercentageRate).HasColumnType("decimal(5,2)");
        modelBuilder.Entity<Accounts>().Property(x => x.Balance).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Transactions>().Property(x => x.Montant).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Transactions>().Property(x => x.MontantCommission).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Collector>().Property(x => x.Plafond).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<IMF>().Property(x => x.TauxTaxe).HasColumnType("decimal(5,2)");
        modelBuilder.Entity<HistCalculComis>().Property(x => x.MontantCommission).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<HistCalculComis>().Property(x => x.MontantTransaction).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<HistCalculComis>().Property(x => x.TauxAppliqueOuFixe).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<AccountsTMP>().Property(x => x.Balance).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<CollectorTMP>().Property(x => x.Plafond).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<CommissionRangeTmp>().Property(x => x.FixedAmount).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<CommissionRangeTmp>().Property(x => x.MaxAmount).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<CommissionRangeTmp>().Property(x => x.MinAmount).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<CommissionRangeTmp>().Property(x => x.PercentageRate).HasColumnType("decimal(5,2)");
        modelBuilder.Entity<IMFTmp>().Property(x => x.TauxTaxe).HasColumnType("decimal(5,2)");
        modelBuilder.Entity<TransactionsTMP>().Property(x => x.Montant).HasColumnType("decimal(18,2)");

        // =====================================================================
        // AGENCY SCOPING: global query filters.
        // These make EVERY LINQ query against these DbSets automatically drop
        // rows outside the connected user's agency - no need to remember to
        // add ".Where(x => x.AgenceID == ...)" in every controller/service.
        // Admin/HQ users (AgenceID == null on their account) bypass the filter.
        // =====================================================================
        modelBuilder.Entity<Collector>()
            .HasQueryFilter(x => _currentUser.IsHeadOffice || x.AgenceID == _currentUser.AgenceID);

        modelBuilder.Entity<Client>()
            .HasQueryFilter(x => _currentUser.IsHeadOffice || x.AgenceID == _currentUser.AgenceID);

        modelBuilder.Entity<Accounts>()
            .HasQueryFilter(x => _currentUser.IsHeadOffice || x.AgenceID == _currentUser.AgenceID);

        modelBuilder.Entity<Transactions>()
            .HasQueryFilter(x => _currentUser.IsHeadOffice || x.AgenceID == _currentUser.AgenceID);

        modelBuilder.Entity<Users>()
            .HasQueryFilter(x => _currentUser.IsHeadOffice || x.AgenceID == _currentUser.AgenceID);
    }
}
