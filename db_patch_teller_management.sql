-- Patch: Teller Management (Vault + Cash Movements).
-- IDEMPOTENT: safe to re-run.

IF OBJECT_ID('dbo.Vault', 'U') IS NULL
BEGIN
    CREATE TABLE Vault (
        VaultID       INT IDENTITY(1,1) PRIMARY KEY,
        AgenceID      INT NOT NULL UNIQUE,
        Balance       DECIMAL(18,2) NOT NULL DEFAULT 0,
        MinimumBalance DECIMAL(18,2) NULL,
        MaximumBalance DECIMAL(18,2) NULL,
        UpdatedDate   DATETIME2 NULL,
        CONSTRAINT FK_Vault_Agence FOREIGN KEY (AgenceID) REFERENCES Agence(AgenceID)
    );
END
GO

IF OBJECT_ID('dbo.CashMovement', 'U') IS NULL
BEGIN
    CREATE TABLE CashMovement (
        CashMovementID  INT IDENTITY(1,1) PRIMARY KEY,
        MovementNumber  NVARCHAR(30) NOT NULL UNIQUE,
        AgenceID        INT NOT NULL,
        MovementType    NVARCHAR(15) NOT NULL,
        FromCodeUser    NVARCHAR(20) NULL,
        ToCodeUser      NVARCHAR(20) NULL,
        Amount          DECIMAL(18,2) NOT NULL,
        Reason          NVARCHAR(300) NULL,
        Status          NVARCHAR(15) NOT NULL DEFAULT 'PENDING',
        RequestedBy     NVARCHAR(50) NOT NULL,
        RequestDate     DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        ApprovedBy      NVARCHAR(50) NULL,
        ApprovalDate    DATETIME2 NULL,
        RejectionReason NVARCHAR(300) NULL
    );
    CREATE INDEX IX_CashMovement_AgenceID ON CashMovement(AgenceID);
    CREATE INDEX IX_CashMovement_Status ON CashMovement(Status);
END
GO
