-- Patch: Cash Session Management module.
-- Creates BusinessCalendar, CashSession, CashVariance, and links Transactions
-- to the session they were performed under (CashSessionID).
--
-- IDEMPOTENT: safe to re-run.

IF OBJECT_ID('dbo.BusinessCalendar', 'U') IS NULL
BEGIN
    CREATE TABLE BusinessCalendar (
        BusinessCalendarID INT IDENTITY(1,1) PRIMARY KEY,
        AgenceID           INT NOT NULL,
        WorkingDays        NVARCHAR(20) NOT NULL DEFAULT '1,2,3,4,5,6',
        OpeningTime        TIME NOT NULL DEFAULT '08:00:00',
        ClosingTime        TIME NOT NULL DEFAULT '17:00:00',
        GracePeriodMinutes INT NOT NULL DEFAULT 15,
        UpdatedBy          NVARCHAR(50) NULL,
        UpdatedDate        DATETIME2 NULL,
        CONSTRAINT FK_BusinessCalendar_Agence FOREIGN KEY (AgenceID) REFERENCES Agence(AgenceID),
        CONSTRAINT UQ_BusinessCalendar_Agence UNIQUE (AgenceID)
    );
END
GO

IF OBJECT_ID('dbo.CashSession', 'U') IS NULL
BEGIN
    CREATE TABLE CashSession (
        CashSessionID       INT IDENTITY(1,1) PRIMARY KEY,
        SessionNumber       NVARCHAR(50) NOT NULL UNIQUE,
        CodeUser            NVARCHAR(20) NOT NULL,
        AgenceID            INT NOT NULL,
        OpeningDate         DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        OpeningCash         DECIMAL(18,2) NOT NULL DEFAULT 0,
        PreviousClosingCash DECIMAL(18,2) NOT NULL DEFAULT 0,
        OpeningComment      NVARCHAR(300) NULL,
        ClosingDate         DATETIME2 NULL,
        ExpectedCash        DECIMAL(18,2) NULL,
        PhysicalCash        DECIMAL(18,2) NULL,
        CashDifference      DECIMAL(18,2) NULL,
        ClosingComment      NVARCHAR(300) NULL,
        ClosedBy            NVARCHAR(50) NULL,
        Status              NVARCHAR(15) NOT NULL DEFAULT 'OPEN',
        RequiresApproval    BIT NOT NULL DEFAULT 0,
        ApprovalStatus      NVARCHAR(15) NULL,
        ApprovedBy          NVARCHAR(50) NULL,
        ApprovalDate        DATETIME2 NULL,
        CreatedBy           NVARCHAR(50) NULL,
        CreatedDate         DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_CashSession_Agence FOREIGN KEY (AgenceID) REFERENCES Agence(AgenceID),
        CONSTRAINT FK_CashSession_User FOREIGN KEY (CodeUser) REFERENCES Users(CodeUser)
    );

    -- Business rule: only one OPEN session per user at a time.
    CREATE UNIQUE INDEX UQ_CashSession_User_Open ON CashSession(CodeUser) WHERE Status = 'OPEN';
    CREATE INDEX IX_CashSession_AgenceID ON CashSession(AgenceID);
END
GO

IF OBJECT_ID('dbo.CashVariance', 'U') IS NULL
BEGIN
    CREATE TABLE CashVariance (
        CashVarianceID     INT IDENTITY(1,1) PRIMARY KEY,
        CashSessionID      INT NOT NULL,
        VarianceAmount     DECIMAL(18,2) NOT NULL,
        VariancePercentage FLOAT NOT NULL DEFAULT 0,
        VarianceType       NVARCHAR(10) NOT NULL DEFAULT 'SHORTAGE',
        Reason             NVARCHAR(300) NULL,
        Comment            NVARCHAR(300) NULL,
        ApprovalStatus     NVARCHAR(15) NOT NULL DEFAULT 'PENDING',
        ApprovedBy         NVARCHAR(50) NULL,
        ApprovalDate       DATETIME2 NULL,
        CreatedDate        DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_CashVariance_Session FOREIGN KEY (CashSessionID) REFERENCES CashSession(CashSessionID)
    );
END
GO

-- Link every financial transaction to the Cash Session it was performed under.
IF COL_LENGTH('Transactions', 'CashSessionID') IS NULL
    ALTER TABLE Transactions ADD CashSessionID INT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Transactions_CashSession')
    ALTER TABLE Transactions ADD CONSTRAINT FK_Transactions_CashSession FOREIGN KEY (CashSessionID) REFERENCES CashSession(CashSessionID);
GO
