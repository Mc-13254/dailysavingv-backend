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
    ('1010', 'Caisse (Cash on Hand)',            'ASSET',     'DEBIT',  1),
    ('1020', 'Comptes bancaires',                 'ASSET',     'DEBIT',  1),
    ('1100', 'Prêts à recevoir (Loans Receivable)', 'ASSET',   'DEBIT',  0),
    ('2010', 'Dépôts clients (Client Savings)',   'LIABILITY', 'CREDIT', 0),
    ('3010', 'Résultats non distribués',          'EQUITY',    'CREDIT', 0),
    ('4010', 'Revenus d''intérêts sur prêts',      'REVENUE',   'CREDIT', 0),
    ('4020', 'Revenus de commissions',             'REVENUE',   'CREDIT', 0),
    ('4030', 'Revenus de pénalités',                'REVENUE',   'CREDIT', 0),
    ('5010', 'Pertes sur prêts (Write-off)',        'EXPENSE',   'DEBIT',  0),
    ('5030', 'Écarts de caisse (Cash Over/Short)',  'EXPENSE',   'DEBIT',  1);
END
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
