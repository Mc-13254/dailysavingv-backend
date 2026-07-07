-- Patch: create the Contract Type module (Business Parameters -> Contract
-- Types) with its Maker-Checker pending table, and link Contract to it.

CREATE TABLE ContractType (
    ContractTypeID          INT IDENTITY(1,1) PRIMARY KEY,
    ContractCode            NVARCHAR(20)  NOT NULL UNIQUE,
    ContractName            NVARCHAR(100) NOT NULL UNIQUE,
    ShortName               NVARCHAR(50)  NULL,
    Description             NVARCHAR(500) NULL,
    AllowDailyCollection    BIT           NOT NULL DEFAULT 0,
    AllowWeeklyCollection   BIT           NOT NULL DEFAULT 0,
    AllowMonthlyCollection  BIT           NOT NULL DEFAULT 0,
    MinimumCollectionAmount DECIMAL(18,2) NULL,
    MaximumCollectionAmount DECIMAL(18,2) NULL,
    DefaultCollectionAmount DECIMAL(18,2) NULL,
    MinimumOpeningBalance   DECIMAL(18,2) NULL,
    MaximumBalance          DECIMAL(18,2) NULL,
    InterestRate            DECIMAL(5,2)  NULL,
    ContractDuration        INT           NULL,
    DurationUnit            NVARCHAR(20)  NULL,
    PenaltyAmount           DECIMAL(18,2) NULL,
    GracePeriod             INT           NULL,
    Statut                  NVARCHAR(20)  NOT NULL DEFAULT 'ACTIVE',
    CreatedBy               NVARCHAR(50)  NULL,
    CreatedDate             DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedBy               NVARCHAR(50)  NULL,
    UpdatedDate             DATETIME2     NULL
);
GO

CREATE TABLE ContractTypeTmp (
    PendingID               INT IDENTITY(1,1) PRIMARY KEY,
    ActionType              NVARCHAR(10)  NOT NULL CHECK (ActionType IN ('CREATE','UPDATE','DELETE')),
    TargetContractTypeID    INT           NULL,
    ContractName            NVARCHAR(100) NULL,
    ShortName               NVARCHAR(50)  NULL,
    Description             NVARCHAR(500) NULL,
    AllowDailyCollection    BIT           NULL,
    AllowWeeklyCollection   BIT           NULL,
    AllowMonthlyCollection  BIT           NULL,
    MinimumCollectionAmount DECIMAL(18,2) NULL,
    MaximumCollectionAmount DECIMAL(18,2) NULL,
    DefaultCollectionAmount DECIMAL(18,2) NULL,
    MinimumOpeningBalance   DECIMAL(18,2) NULL,
    MaximumBalance          DECIMAL(18,2) NULL,
    InterestRate            DECIMAL(5,2)  NULL,
    ContractDuration        INT           NULL,
    DurationUnit            NVARCHAR(20)  NULL,
    PenaltyAmount           DECIMAL(18,2) NULL,
    GracePeriod             INT           NULL,
    Statut                  NVARCHAR(20)  NULL,
    PendingStatus           NVARCHAR(20)  NOT NULL DEFAULT 'PENDING' CHECK (PendingStatus IN ('PENDING','APPROVED','REJECTED')),
    RequestUser             NVARCHAR(20)  NOT NULL,
    RequestDate             DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
    ValidationUser          NVARCHAR(20)  NULL,
    ValidationDate          DATETIME2     NULL,
    RejectionReason         NVARCHAR(500) NULL,
    PreviousData            NVARCHAR(MAX) NULL,
    NewData                 NVARCHAR(MAX) NULL
);
GO

-- Link client contracts to a Contract Type (nullable: pre-existing contracts
-- keep using the old free-text ContractType column until reassigned).
ALTER TABLE Contract ADD ContractTypeID INT NULL REFERENCES ContractType(ContractTypeID);
GO
