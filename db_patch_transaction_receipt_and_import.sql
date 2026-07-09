-- Patch: bank-receipt style transaction fields (remitter/beneficiary, transfer
-- destination account) + Excel bulk import (Maker-Checker).
--
-- IDEMPOTENT: safe to re-run.

IF COL_LENGTH('Transactions', 'ToAccountID') IS NULL
    ALTER TABLE Transactions ADD ToAccountID NVARCHAR(20) NULL;
IF COL_LENGTH('Transactions', 'RemitterName') IS NULL
    ALTER TABLE Transactions ADD RemitterName NVARCHAR(150) NULL;
IF COL_LENGTH('Transactions', 'BeneficiaryName') IS NULL
    ALTER TABLE Transactions ADD BeneficiaryName NVARCHAR(150) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Transactions_ToAccount')
    ALTER TABLE Transactions ADD CONSTRAINT FK_Transactions_ToAccount FOREIGN KEY (ToAccountID) REFERENCES Accounts(AccountID);
GO

IF OBJECT_ID('dbo.TransactionImportBatch', 'U') IS NULL
BEGIN
    CREATE TABLE TransactionImportBatch (
        BatchID       INT IDENTITY(1,1) PRIMARY KEY,
        FileName      NVARCHAR(260) NOT NULL,
        UploadedBy    NVARCHAR(50) NOT NULL,
        UploadedDate  DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        TotalRows     INT NOT NULL DEFAULT 0,
        AgenceID      INT NOT NULL,
        Status        NVARCHAR(20) NOT NULL DEFAULT 'PENDING',
        CONSTRAINT FK_TransactionImportBatch_Agence FOREIGN KEY (AgenceID) REFERENCES Agence(AgenceID)
    );
END
GO

IF OBJECT_ID('dbo.TransactionImportRow', 'U') IS NULL
BEGIN
    CREATE TABLE TransactionImportRow (
        RowID           INT IDENTITY(1,1) PRIMARY KEY,
        BatchID         INT NOT NULL,
        RowNumber       INT NOT NULL,
        TransactionType NVARCHAR(30) NOT NULL,
        AccountID       NVARCHAR(20) NOT NULL,
        ToAccountID     NVARCHAR(20) NULL,
        CollectorID     NVARCHAR(20) NULL,
        Montant         DECIMAL(18,2) NOT NULL,
        RemitterName    NVARCHAR(150) NULL,
        BeneficiaryName NVARCHAR(150) NULL,
        RefRowLabel     NVARCHAR(200) NULL,
        Status          NVARCHAR(20) NOT NULL DEFAULT 'PENDING',
        ErrorMessage    NVARCHAR(500) NULL,
        TransactionID   BIGINT NULL,
        ApprovedBy      NVARCHAR(50) NULL,
        ApprovalDate    DATETIME2 NULL,
        CONSTRAINT FK_TransactionImportRow_Batch FOREIGN KEY (BatchID) REFERENCES TransactionImportBatch(BatchID)
    );
    CREATE INDEX IX_TransactionImportRow_BatchID ON TransactionImportRow(BatchID);
    CREATE INDEX IX_TransactionImportRow_Status ON TransactionImportRow(Status);
END
GO
