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

        // ---- Keys ----
        modelBuilder.Entity<IMF>().HasKey(x => x.CodeIMF);
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
