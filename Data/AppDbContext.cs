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
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<ContractType> ContractTypes => Set<ContractType>();
    public DbSet<NumberingParameter> NumberingParameters => Set<NumberingParameter>();

    // RBAC
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Fonctionnalite> Fonctionnalites => Set<Fonctionnalite>();
    public DbSet<Habilitation> Habilitations => Set<Habilitation>();
    public DbSet<Habiliter> Habiliters => Set<Habiliter>();
    public DbSet<RoleFonctionnalite> RoleFonctionnalites => Set<RoleFonctionnalite>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    // Users
    public DbSet<Users> Users => Set<Users>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<FailedLoginAttempt> FailedLoginAttempts => Set<FailedLoginAttempt>();
    public DbSet<PasswordPolicy> PasswordPolicies => Set<PasswordPolicy>();
    public DbSet<PasswordHistory> PasswordHistories => Set<PasswordHistory>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<ErrorLog> ErrorLogs => Set<ErrorLog>();

    // Core business (agency-scoped)
    public DbSet<Collector> Collectors => Set<Collector>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Accounts> Accounts => Set<Accounts>();
    public DbSet<Contract> Contracts => Set<Contract>();

    // Commission engine
    public DbSet<CommissionType> CommissionTypes => Set<CommissionType>();
    public DbSet<CommissionRange> CommissionRanges => Set<CommissionRange>();

    // Collector Zone Assignment & Performance (new modules)
    public DbSet<CollectorZoneAssignment> CollectorZoneAssignments => Set<CollectorZoneAssignment>();
    public DbSet<ZoneAssignmentHistory> ZoneAssignmentHistories => Set<ZoneAssignmentHistory>();
    public DbSet<CollectorTarget> CollectorTargets => Set<CollectorTarget>();

    // Transactions & audit
    public DbSet<Transactions> Transactions => Set<Transactions>();
    public DbSet<LoanProduct> LoanProducts => Set<LoanProduct>();
    public DbSet<LoanApplication> LoanApplications => Set<LoanApplication>();
    public DbSet<Loan> Loans => Set<Loan>();
    public DbSet<LoanInstallment> LoanInstallments => Set<LoanInstallment>();
    public DbSet<LoanRepayment> LoanRepayments => Set<LoanRepayment>();
    public DbSet<Vault> Vaults => Set<Vault>();
    public DbSet<CashMovement> CashMovements => Set<CashMovement>();
    public DbSet<HistTransactions> HistTransactions => Set<HistTransactions>();
    public DbSet<BusinessCalendar> BusinessCalendars => Set<BusinessCalendar>();
    public DbSet<CashSession> CashSessions => Set<CashSession>();
    public DbSet<CashVariance> CashVariances => Set<CashVariance>();
    public DbSet<TransactionImportBatch> TransactionImportBatches => Set<TransactionImportBatch>();
    public DbSet<TransactionImportRow> TransactionImportRows => Set<TransactionImportRow>();
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
    public DbSet<RoleTmp> RoleTmps => Set<RoleTmp>();
    public DbSet<DepartmentTmp> DepartmentTmps => Set<DepartmentTmp>();
    public DbSet<ContractTypeTmp> ContractTypeTmps => Set<ContractTypeTmp>();
    public DbSet<IMFTmp> IMFTmps => Set<IMFTmp>();

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
        modelBuilder.Entity<CollectorZoneAssignment>().ToTable("CollectorZoneAssignment");
        modelBuilder.Entity<CollectorZoneAssignment>().HasKey(x => x.AssignmentID);
        modelBuilder.Entity<ZoneAssignmentHistory>().ToTable("ZoneAssignmentHistory");
        modelBuilder.Entity<ZoneAssignmentHistory>().HasKey(x => x.HistoryID);
        modelBuilder.Entity<CollectorTarget>().ToTable("CollectorTarget");
        modelBuilder.Entity<CollectorTarget>().HasKey(x => x.TargetID);
        modelBuilder.Entity<Activite>().ToTable("Activite");
        modelBuilder.Entity<UsersTmp>().ToTable("UsersTmp");
        modelBuilder.Entity<CollectorTMP>().ToTable("CollectorTMP");
        modelBuilder.Entity<ClientTmp>().ToTable("ClientTmp");
        modelBuilder.Entity<AccountsTMP>().ToTable("AccountsTMP");
        modelBuilder.Entity<ContractTmp>().ToTable("ContractTmp");
        modelBuilder.Entity<CommissionTypeTmp>().ToTable("CommissionTypeTmp");
        modelBuilder.Entity<CommissionRangeTmp>().ToTable("CommissionRangeTmp");
        modelBuilder.Entity<AgenceTmp>().ToTable("AgenceTmp");
        modelBuilder.Entity<RoleTmp>().ToTable("RoleTmp");
        modelBuilder.Entity<Department>().ToTable("Department");
        modelBuilder.Entity<Permission>().ToTable("Permission");
        modelBuilder.Entity<RolePermission>().ToTable("RolePermission");
        modelBuilder.Entity<DepartmentTmp>().ToTable("DepartmentTmp");
        modelBuilder.Entity<ContractType>().ToTable("ContractType");
        modelBuilder.Entity<NumberingParameter>().ToTable("NumberingParameter");
        modelBuilder.Entity<ContractTypeTmp>().ToTable("ContractTypeTmp");
        modelBuilder.Entity<IMFTmp>().ToTable("IMFTmp");

        // ---- Keys ----
        modelBuilder.Entity<IMF>().HasKey(x => x.CodeIMF);
        modelBuilder.Entity<Currency>().HasKey(x => x.CurrencyCode);
        modelBuilder.Entity<Currency>().ToTable("Currency");
        modelBuilder.Entity<Language>().HasKey(x => x.LanguageCode);
        modelBuilder.Entity<Language>().ToTable("Language");
        modelBuilder.Entity<TimeZoneRef>().ToTable("TimeZoneRef");
        modelBuilder.Entity<TimeZoneRef>().HasKey(x => x.TimeZoneID);
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
        modelBuilder.Entity<FailedLoginAttempt>().HasKey(x => x.AttemptID);

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
        modelBuilder.Entity<RoleTmp>().HasKey(x => x.PendingID);
        modelBuilder.Entity<DepartmentTmp>().HasKey(x => x.PendingID);
        modelBuilder.Entity<ContractTypeTmp>().HasKey(x => x.PendingID);
        modelBuilder.Entity<IMFTmp>().HasKey(x => x.PendingID);

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

        modelBuilder.Entity<Collector>()
            .HasOne(x => x.Department).WithMany()
            .HasForeignKey(x => x.DepartmentID)
            .IsRequired(false);

        modelBuilder.Entity<Collector>()
            .HasOne(x => x.Contract).WithMany()
            .HasForeignKey(x => x.ContractID)
            .IsRequired(false);

        modelBuilder.Entity<Collector>()
            .HasOne(x => x.CommissionType).WithMany()
            .HasForeignKey(x => x.CommissionTypeID)
            .IsRequired(false);

        modelBuilder.Entity<Collector>()
            .HasOne(x => x.CommissionRange).WithMany()
            .HasForeignKey(x => x.CommissionRangeID)
            .IsRequired(false);

        modelBuilder.Entity<Collector>()
            .HasOne(x => x.Supervisor).WithMany()
            .HasForeignKey(x => x.SupervisorId)
            .IsRequired(false);

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

        // Explicit config for the second FK to Accounts (Transactions.Account
        // already points to Accounts via AccountID) — two navigations to the
        // same target type need to be disambiguated explicitly, the same
        // lesson learned from the CashSession.User shadow-column bug.
        modelBuilder.Entity<Transactions>()
            .HasOne(x => x.ToAccount).WithMany()
            .HasForeignKey(x => x.ToAccountID)
            .IsRequired(false);

        modelBuilder.Entity<Transactions>()
            .HasOne(x => x.CashSession).WithMany()
            .HasForeignKey(x => x.CashSessionID)
            .IsRequired(false);

        modelBuilder.Entity<CommissionRange>()
            .HasOne(x => x.CommissionType).WithMany(x => x.Ranges)
            .HasForeignKey(x => x.CommissionTypeID)
            .IsRequired();

        // SQL Server forbids EF Core's OUTPUT clause on tables with triggers
        // (TRG_CommissionRange_NoOverlap) — disable it for this entity.
        modelBuilder.Entity<CommissionRange>().ToTable(tb => tb.UseSqlOutputClause(false));

        modelBuilder.Entity<IMF>()
            .HasOne(x => x.Pays).WithMany()
            .HasForeignKey(x => x.PaysID)
            .IsRequired(false);

        modelBuilder.Entity<IMF>()
            .HasOne(x => x.Ville).WithMany()
            .HasForeignKey(x => x.VilleID)
            .IsRequired(false);

        modelBuilder.Entity<Agence>()
            .HasOne(x => x.Pays).WithMany()
            .HasForeignKey(x => x.PaysID)
            .IsRequired(false);

        modelBuilder.Entity<Agence>()
            .HasOne(x => x.Manager).WithMany()
            .HasForeignKey(x => x.ManagerId)
            .IsRequired(false);

        modelBuilder.Entity<Users>()
            .HasOne(x => x.Pays).WithMany()
            .HasForeignKey(x => x.PaysID)
            .IsRequired(false);

        modelBuilder.Entity<Users>()
            .HasOne(x => x.Ville).WithMany()
            .HasForeignKey(x => x.VilleID)
            .IsRequired(false);

        modelBuilder.Entity<Department>()
            .HasOne(x => x.IMF).WithMany()
            .HasForeignKey(x => x.CodeIMF)
            .IsRequired();

        modelBuilder.Entity<Department>()
            .HasOne(x => x.Agence).WithMany()
            .HasForeignKey(x => x.AgenceID)
            .IsRequired();

        modelBuilder.Entity<Department>()
            .HasOne(x => x.Manager).WithMany()
            .HasForeignKey(x => x.ManagerId)
            .IsRequired(false);

        modelBuilder.Entity<Users>()
            .HasOne(x => x.DepartmentRef).WithMany()
            .HasForeignKey(x => x.DepartmentID)
            .IsRequired(false);

        modelBuilder.Entity<RolePermission>()
            .HasOne(x => x.Role).WithMany()
            .HasForeignKey(x => x.RoleID)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        modelBuilder.Entity<RolePermission>()
            .HasOne(x => x.Permission).WithMany()
            .HasForeignKey(x => x.PermissionID)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        modelBuilder.Entity<RolePermission>()
            .HasIndex(x => new { x.RoleID, x.PermissionID })
            .IsUnique();

        modelBuilder.Entity<Contract>()
            .HasOne(x => x.ContractTypeRef).WithMany()
            .HasForeignKey(x => x.ContractTypeID)
            .IsRequired(false);

        // EF Core stores enums as integers by default. The *Tmp (Pending)
        // tables' ActionType/PendingStatus columns are NVARCHAR with a CHECK
        // constraint expecting the text ('CREATE'/'UPDATE'/'DELETE', etc.),
        // so every Pending entity needs its enum properties converted to
        // strings - applied once here for all of them instead of repeating
        // it per entity.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(Entities.Pending.PendingBase).IsAssignableFrom(entityType.ClrType)) continue;

            modelBuilder.Entity(entityType.ClrType)
                .Property(nameof(Entities.Pending.PendingBase.ActionType))
                .HasConversion<string>();

            modelBuilder.Entity(entityType.ClrType)
                .Property(nameof(Entities.Pending.PendingBase.PendingStatus))
                .HasConversion<string>();
        }

        // ---- Enum -> string conversions (readable values in DB, matches CHECK constraints) ----
        modelBuilder.Entity<CommissionRange>()
            .Property(x => x.CalculationMethod)
            .HasConversion<string>();

        modelBuilder.Entity<Transactions>()
            .Property(x => x.TransactionType)
            .HasConversion<string>();

        // ---- Decimal precision (match DECIMAL(18,2) / (5,2) from the SQL schema) ----
        modelBuilder.Entity<CommissionRange>().Property(x => x.Inf).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<CommissionRange>().Property(x => x.Sup).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<CommissionRange>().Property(x => x.Fixe).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<CommissionRange>().Property(x => x.TAUX).HasColumnType("decimal(5,2)");
        modelBuilder.Entity<CommissionRange>().Property(x => x.Minimum).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<CommissionRange>().Property(x => x.Maximum).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Accounts>().Property(x => x.Balance).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Accounts>().Property(x => x.OpeningBalance).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Accounts>().Property(x => x.AvailableBalance).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Accounts>().Property(x => x.BlockedBalance).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Accounts>().Property(x => x.MinimumBalance).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Accounts>().Property(x => x.MaximumBalance).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Accounts>().Property(x => x.DailyDepositLimit).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Accounts>().Property(x => x.DailyWithdrawalLimit).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Accounts>().Property(x => x.DailyTransactionLimit).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Accounts>().Property(x => x.OverdraftLimit).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<AccountsTMP>().Property(x => x.Balance).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<AccountsTMP>().Property(x => x.OpeningBalance).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<AccountsTMP>().Property(x => x.AvailableBalance).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<AccountsTMP>().Property(x => x.BlockedBalance).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<AccountsTMP>().Property(x => x.MinimumBalance).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<AccountsTMP>().Property(x => x.MaximumBalance).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<AccountsTMP>().Property(x => x.DailyDepositLimit).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<AccountsTMP>().Property(x => x.DailyWithdrawalLimit).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<AccountsTMP>().Property(x => x.DailyTransactionLimit).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<AccountsTMP>().Property(x => x.OverdraftLimit).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Contract>().Property(x => x.OpeningDeposit).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Contract>().Property(x => x.MinimumBalance).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Contract>().Property(x => x.MaximumBalance).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<ContractTmp>().Property(x => x.OpeningDeposit).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<ContractTmp>().Property(x => x.MinimumBalance).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<ContractTmp>().Property(x => x.MaximumBalance).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Client>().Property(x => x.MonthlyIncome).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Client>().Property(x => x.MonthlyRevenue).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Client>().Property(x => x.MonthlyExpenses).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Client>().Property(x => x.Latitude).HasColumnType("decimal(9,6)");
        modelBuilder.Entity<Client>().Property(x => x.Longitude).HasColumnType("decimal(9,6)");
        modelBuilder.Entity<ClientTmp>().Property(x => x.MonthlyIncome).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<ClientTmp>().Property(x => x.MonthlyRevenue).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<ClientTmp>().Property(x => x.MonthlyExpenses).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<ClientTmp>().Property(x => x.Latitude).HasColumnType("decimal(9,6)");
        modelBuilder.Entity<ClientTmp>().Property(x => x.Longitude).HasColumnType("decimal(9,6)");

        // Business rules: unique National ID / phone (nulls allowed, so filtered).
        modelBuilder.Entity<Client>().HasIndex(c => c.NumeroCNI).IsUnique().HasFilter("[NumeroCNI] IS NOT NULL");
        modelBuilder.Entity<Client>().HasIndex(c => c.PhoneNumber).IsUnique().HasFilter("[PhoneNumber] IS NOT NULL");
        // Only one ACTIVE contract per client.
        // A client may hold several active contracts (one per savings product),
        // so no uniqueness constraint on Contract.ClientID anymore.
        // Only one account per contract.
        modelBuilder.Entity<Accounts>().HasIndex(a => a.ContractID).IsUnique().HasFilter("[ContractID] IS NOT NULL");
        modelBuilder.Entity<Transactions>().Property(x => x.Montant).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Transactions>().Property(x => x.MontantCommission).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Transactions>().Property(x => x.OpeningBalance).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Transactions>().Property(x => x.ClosingBalance).HasColumnType("decimal(18,2)");

        modelBuilder.Entity<LoanProduct>().Property(x => x.AnnualInterestRate).HasColumnType("decimal(9,4)");
        modelBuilder.Entity<LoanProduct>().Property(x => x.MinAmount).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<LoanProduct>().Property(x => x.MaxAmount).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<LoanProduct>().Property(x => x.PenaltyRatePerDay).HasColumnType("decimal(9,4)");

        modelBuilder.Entity<LoanApplication>().Property(x => x.RequestedAmount).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<LoanApplication>().Property(x => x.ApprovedAmount).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<LoanApplication>().HasOne(x => x.Client).WithMany().HasForeignKey(x => x.ClientID).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<LoanApplication>().HasOne(x => x.LoanProduct).WithMany().HasForeignKey(x => x.LoanProductID).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Loan>().Property(x => x.PrincipalAmount).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Loan>().Property(x => x.AnnualInterestRate).HasColumnType("decimal(9,4)");
        modelBuilder.Entity<Loan>().Property(x => x.TotalPrincipal).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Loan>().Property(x => x.TotalInterest).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Loan>().Property(x => x.OutstandingPrincipal).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Loan>().Property(x => x.OutstandingInterest).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Loan>().Property(x => x.OutstandingPenalty).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Loan>().HasOne(x => x.Client).WithMany().HasForeignKey(x => x.ClientID).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Loan>().HasOne(x => x.LoanProduct).WithMany().HasForeignKey(x => x.LoanProductID).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Loan>().HasOne(x => x.LoanApplication).WithMany().HasForeignKey(x => x.LoanApplicationID).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Loan>().HasIndex(x => x.LoanNumber).IsUnique();

        modelBuilder.Entity<LoanInstallment>().Property(x => x.PrincipalDue).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<LoanInstallment>().Property(x => x.InterestDue).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<LoanInstallment>().Property(x => x.PenaltyDue).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<LoanInstallment>().Property(x => x.PrincipalPaid).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<LoanInstallment>().Property(x => x.InterestPaid).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<LoanInstallment>().Property(x => x.PenaltyPaid).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<LoanInstallment>().HasOne(x => x.Loan).WithMany().HasForeignKey(x => x.LoanID).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<LoanRepayment>().Property(x => x.Amount).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<LoanRepayment>().Property(x => x.PrincipalPaid).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<LoanRepayment>().Property(x => x.InterestPaid).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<LoanRepayment>().Property(x => x.PenaltyPaid).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<LoanRepayment>().HasOne(x => x.Loan).WithMany().HasForeignKey(x => x.LoanID).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Vault>().Property(x => x.Balance).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Vault>().Property(x => x.MinimumBalance).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Vault>().Property(x => x.MaximumBalance).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Vault>().HasOne(x => x.Agence).WithMany().HasForeignKey(x => x.AgenceID).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Vault>().HasIndex(x => x.AgenceID).IsUnique();

        modelBuilder.Entity<CashMovement>().Property(x => x.Amount).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<CashMovement>().HasIndex(x => x.MovementNumber).IsUnique();
        modelBuilder.Entity<Collector>().Property(x => x.Plafond).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Collector>().Property(x => x.Caution).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Collector>().Property(x => x.CollectMonth).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Collector>().Property(x => x.CollectDay).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Collector>().Property(x => x.RetraitMonth).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Collector>().Property(x => x.RetraitDay).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<CollectorTMP>().Property(x => x.Plafond).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<CollectorTMP>().Property(x => x.Caution).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<CollectorTMP>().Property(x => x.CollectMonth).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<CollectorTMP>().Property(x => x.CollectDay).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<CollectorTMP>().Property(x => x.RetraitMonth).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<CollectorTMP>().Property(x => x.RetraitDay).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<IMF>().Property(x => x.TauxTaxe).HasColumnType("decimal(5,2)");
        modelBuilder.Entity<HistCalculComis>().Property(x => x.MontantCommission).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<HistCalculComis>().Property(x => x.MontantTransaction).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<HistCalculComis>().Property(x => x.TauxAppliqueOuFixe).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<AccountsTMP>().Property(x => x.Balance).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<CollectorTMP>().Property(x => x.Plafond).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<CommissionRangeTmp>().Property(x => x.Fixe).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<CommissionRangeTmp>().Property(x => x.Sup).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<CommissionRangeTmp>().Property(x => x.Inf).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<CommissionRangeTmp>().Property(x => x.TAUX).HasColumnType("decimal(5,2)");
        modelBuilder.Entity<CommissionRangeTmp>().Property(x => x.Minimum).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<CommissionRangeTmp>().Property(x => x.Maximum).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<IMFTmp>().Property(x => x.TauxTaxe).HasColumnType("decimal(5,2)");

        modelBuilder.Entity<ContractType>().Property(x => x.MinimumCollectionAmount).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<ContractType>().Property(x => x.MaximumCollectionAmount).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<ContractType>().Property(x => x.DefaultCollectionAmount).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<ContractType>().Property(x => x.MinimumOpeningBalance).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<ContractType>().Property(x => x.MaximumBalance).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<ContractType>().Property(x => x.InterestRate).HasColumnType("decimal(5,2)");
        modelBuilder.Entity<ContractType>().Property(x => x.PenaltyAmount).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<ContractTypeTmp>().Property(x => x.MinimumCollectionAmount).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<ContractTypeTmp>().Property(x => x.MaximumCollectionAmount).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<ContractTypeTmp>().Property(x => x.DefaultCollectionAmount).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<ContractTypeTmp>().Property(x => x.MinimumOpeningBalance).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<ContractTypeTmp>().Property(x => x.MaximumBalance).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<ContractTypeTmp>().Property(x => x.InterestRate).HasColumnType("decimal(5,2)");
        modelBuilder.Entity<ContractTypeTmp>().Property(x => x.PenaltyAmount).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Users>().Property(x => x.DebitMax).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Users>().Property(x => x.CreditMax).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Users>().Property(x => x.ValidationMax).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Users>().Property(x => x.PlafondCollect).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Users>().Property(x => x.Caution).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<UsersTmp>().Property(x => x.DebitMax).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<UsersTmp>().Property(x => x.CreditMax).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<UsersTmp>().Property(x => x.ValidationMax).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<UsersTmp>().Property(x => x.PlafondCollect).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<UsersTmp>().Property(x => x.Caution).HasColumnType("decimal(18,2)");

        modelBuilder.Entity<ZoneCollecte>().Property(x => x.Latitude).HasColumnType("decimal(9,6)");
        modelBuilder.Entity<ZoneCollecte>().Property(x => x.Longitude).HasColumnType("decimal(9,6)");
        modelBuilder.Entity<ZoneCollecte>().Property(x => x.RadiusMeters).HasColumnType("decimal(10,2)");
        modelBuilder.Entity<CollectorTarget>().Property(x => x.TargetAmount).HasColumnType("decimal(18,2)");

        modelBuilder.Entity<BusinessCalendar>().ToTable("BusinessCalendar");
        modelBuilder.Entity<CashSession>().ToTable("CashSession");
        modelBuilder.Entity<CashSession>().Property(x => x.OpeningCash).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<CashSession>().Property(x => x.PreviousClosingCash).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<CashSession>().Property(x => x.ExpectedCash).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<CashSession>().Property(x => x.PhysicalCash).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<CashSession>().Property(x => x.CashDifference).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<CashSession>().HasIndex(x => x.SessionNumber).IsUnique();
        modelBuilder.Entity<CashVariance>().ToTable("CashVariance");
        modelBuilder.Entity<CashVariance>().Property(x => x.VarianceAmount).HasColumnType("decimal(18,2)");

        modelBuilder.Entity<TransactionImportBatch>().ToTable("TransactionImportBatch");
        modelBuilder.Entity<TransactionImportBatch>().HasKey(x => x.BatchID);
        modelBuilder.Entity<TransactionImportRow>().ToTable("TransactionImportRow");
        modelBuilder.Entity<TransactionImportRow>().HasKey(x => x.RowID);
        modelBuilder.Entity<TransactionImportRow>().Property(x => x.Montant).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<TransactionImportRow>()
            .HasOne(r => r.Batch)
            .WithMany(b => b.Rows)
            .HasForeignKey(r => r.BatchID);

        // Business rule: at most one ACTIVE collector per zone at a time, so a
        // Client's effective collector (inherited via its Zone) is unambiguous.
        modelBuilder.Entity<CollectorZoneAssignment>()
            .HasIndex(x => x.ZoneCollecteID)
            .HasFilter("[Status] = 'ACTIVE'")
            .IsUnique();

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

        // Zones created by an agency are only visible to that agency (HQ sees all).
        // Zones with AgenceID == null are treated as global/shared zones.
        modelBuilder.Entity<ZoneCollecte>()
            .HasQueryFilter(x => _currentUser.IsHeadOffice || x.AgenceID == null || x.AgenceID == _currentUser.AgenceID);
    }
}
