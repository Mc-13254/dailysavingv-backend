-- Patch: add audit fields to Role, and create the RoleTmp pending
-- (Maker-Checker) table so Roles follow the same workflow as every
-- other module (IMF, Agence, etc).

ALTER TABLE Role ADD
    UpdatedBy   NVARCHAR(50) NULL,
    UpdatedDate DATETIME2    NULL;
GO

CREATE TABLE RoleTmp (
    PendingID       INT IDENTITY(1,1) PRIMARY KEY,
    ActionType      NVARCHAR(10)  NOT NULL CHECK (ActionType IN ('CREATE','UPDATE','DELETE')),
    TargetRoleID    INT           NULL,
    Code            NVARCHAR(50)  NULL,
    Libelle         NVARCHAR(100) NULL,
    Description     NVARCHAR(500) NULL,
    Statut          BIT           NULL,
    PendingStatus   NVARCHAR(20)  NOT NULL DEFAULT 'PENDING' CHECK (PendingStatus IN ('PENDING','APPROVED','REJECTED')),
    RequestUser     NVARCHAR(20)  NOT NULL,
    RequestDate     DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
    ValidationUser  NVARCHAR(20)  NULL,
    ValidationDate  DATETIME2     NULL,
    RejectionReason NVARCHAR(500) NULL,
    PreviousData    NVARCHAR(MAX) NULL,
    NewData         NVARCHAR(MAX) NULL
);
GO
