-- Patch: Double-entry Accounting Engine (Chart of Accounts + Journal Entries).
-- IDEMPOTENT: safe to re-run.

IF OBJECT_ID('dbo.GLAccount', 'U') IS NULL
BEGIN
    CREATE TABLE GLAccount (
        GLAccountID    INT IDENTITY(1,1) PRIMARY KEY,
        Code           NVARCHAR(10) NOT NULL UNIQUE,
        Name           NVARCHAR(100) NOT NULL,
        Type           NVARCHAR(15) NOT NULL,
        NormalBalance  NVARCHAR(6) NOT NULL,
        IsCashAccount  BIT NOT NULL DEFAULT 0,
        Statut         BIT NOT NULL DEFAULT 1
    );

    INSERT INTO GLAccount (Code, Name, Type, NormalBalance, IsCashAccount) VALUES
    ('1010', 'Caisse — Coffre (Vault)',          'ASSET',     'DEBIT',  1),
    ('1011', 'Caisse — Guichet (Till)',           'ASSET',     'DEBIT',  1),
    ('1020', 'Comptes bancaires',                 'ASSET',     'DEBIT',  1),
    ('1100', 'Prêts à recevoir (Loans Receivable)', 'ASSET',   'DEBIT',  0),
    ('2010', 'Dépôts clients (Client Savings)',   'LIABILITY', 'CREDIT', 0),
    ('3010', 'Résultats non distribués',          'EQUITY',    'CREDIT', 0),
    ('4010', 'Revenus d''intérêts sur prêts',      'REVENUE',   'CREDIT', 0),
    ('4020', 'Revenus de commissions',             'REVENUE',   'CREDIT', 0),
    ('4030', 'Revenus de pénalités',                'REVENUE',   'CREDIT', 0),
    ('5010', 'Pertes sur prêts (Write-off)',        'EXPENSE',   'DEBIT',  0),
    ('5030', 'Écarts de caisse (Cash Over/Short)',  'EXPENSE',   'DEBIT',  1),
    ('5040', 'Intérêts versés aux membres (GL)',     'EXPENSE',   'DEBIT',  0);
END
GO

IF NOT EXISTS (SELECT 1 FROM GLAccount WHERE Code = '5040')
    INSERT INTO GLAccount (Code, Name, Type, NormalBalance, IsCashAccount) VALUES ('5040', 'Intérêts versés aux membres (GL)', 'EXPENSE', 'DEBIT', 0);
GO

-- If this patch runs on a database that already seeded the accounts WITHOUT
-- 1011 (Till), add it without disturbing anything already posted.
IF NOT EXISTS (SELECT 1 FROM GLAccount WHERE Code = '1011')
    INSERT INTO GLAccount (Code, Name, Type, NormalBalance, IsCashAccount) VALUES ('1011', 'Caisse — Guichet (Till)', 'ASSET', 'DEBIT', 1);
GO

IF OBJECT_ID('dbo.JournalEntry', 'U') IS NULL
BEGIN
    CREATE TABLE JournalEntry (
        JournalEntryID  INT IDENTITY(1,1) PRIMARY KEY,
        EntryNumber     NVARCHAR(30) NOT NULL UNIQUE,
        EntryDate       DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        Description     NVARCHAR(300) NOT NULL,
        SourceType      NVARCHAR(30) NOT NULL,
        SourceReference NVARCHAR(50) NULL,
        AgenceID        INT NOT NULL,
        CreatedBy       NVARCHAR(50) NOT NULL,
        CreatedDate     DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
    CREATE INDEX IX_JournalEntry_EntryDate ON JournalEntry(EntryDate);
    CREATE INDEX IX_JournalEntry_SourceType_Reference ON JournalEntry(SourceType, SourceReference);
END
GO

IF OBJECT_ID('dbo.JournalEntryLine', 'U') IS NULL
BEGIN
    CREATE TABLE JournalEntryLine (
        JournalEntryLineID INT IDENTITY(1,1) PRIMARY KEY,
        JournalEntryID      INT NOT NULL,
        GLAccountID         INT NOT NULL,
        Debit               DECIMAL(18,2) NOT NULL DEFAULT 0,
        Credit              DECIMAL(18,2) NOT NULL DEFAULT 0,
        Description         NVARCHAR(200) NULL,
        CONSTRAINT FK_JournalEntryLine_Entry FOREIGN KEY (JournalEntryID) REFERENCES JournalEntry(JournalEntryID),
        CONSTRAINT FK_JournalEntryLine_Account FOREIGN KEY (GLAccountID) REFERENCES GLAccount(GLAccountID)
    );
    CREATE INDEX IX_JournalEntryLine_JournalEntryID ON JournalEntryLine(JournalEntryID);
    CREATE INDEX IX_JournalEntryLine_GLAccountID ON JournalEntryLine(GLAccountID);
END
GO

IF OBJECT_ID('dbo.AccountingPeriod', 'U') IS NULL
BEGIN
    CREATE TABLE AccountingPeriod (
        AccountingPeriodID INT IDENTITY(1,1) PRIMARY KEY,
        Year        INT NOT NULL,
        Month       INT NOT NULL,
        IsClosed    BIT NOT NULL DEFAULT 0,
        ClosedBy    NVARCHAR(50) NULL,
        ClosedDate  DATETIME2 NULL,
        CONSTRAINT UQ_AccountingPeriod_YearMonth UNIQUE (Year, Month)
    );
END
GO

IF OBJECT_ID('dbo.ManualJournalEntryDraft', 'U') IS NULL
BEGIN
    CREATE TABLE ManualJournalEntryDraft (
        ManualJournalEntryDraftID INT IDENTITY(1,1) PRIMARY KEY,
        EntryType             NVARCHAR(15) NOT NULL DEFAULT 'MANUAL',
        Description           NVARCHAR(300) NOT NULL,
        ReversalOfJournalEntryID INT NULL,
        AgenceID              INT NOT NULL,
        LinesJson             NVARCHAR(MAX) NOT NULL,
        Status                NVARCHAR(15) NOT NULL DEFAULT 'PENDING',
        RequestedBy           NVARCHAR(50) NOT NULL,
        RequestDate           DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        ApprovedBy            NVARCHAR(50) NULL,
        ApprovalDate          DATETIME2 NULL,
        RejectionReason       NVARCHAR(300) NULL,
        PostedJournalEntryID  INT NULL
    );
END
GO

IF OBJECT_ID('dbo.AccountingActivityLog', 'U') IS NULL
BEGIN
    CREATE TABLE AccountingActivityLog (
        AccountingActivityLogID INT IDENTITY(1,1) PRIMARY KEY,
        CodeUser   NVARCHAR(20) NOT NULL,
        Action     NVARCHAR(30) NOT NULL,
        ReportType NVARCHAR(50) NULL,
        Details    NVARCHAR(300) NULL,
        ActionDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
    CREATE INDEX IX_AccountingActivityLog_ActionDate ON AccountingActivityLog(ActionDate);
END
GO
