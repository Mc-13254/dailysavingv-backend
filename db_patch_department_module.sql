-- Patch: create the Department module (Administration -> Departments) with
-- its Maker-Checker pending table, and link Users to a Department.

CREATE TABLE Department (
    DepartmentID    INT IDENTITY(1,1) PRIMARY KEY,
    DepartmentCode  NVARCHAR(20)  NOT NULL UNIQUE,
    DepartmentName  NVARCHAR(100) NOT NULL,
    ShortName       NVARCHAR(50)  NULL,
    Description     NVARCHAR(500) NULL,
    CodeIMF         NVARCHAR(20)  NOT NULL REFERENCES IMF(CodeIMF),
    AgenceID        INT           NOT NULL REFERENCES Agence(AgenceID),
    ManagerId       NVARCHAR(20)  NULL REFERENCES Users(CodeUser),
    Statut          NVARCHAR(20)  NOT NULL DEFAULT 'ACTIVE',
    CreatedBy       NVARCHAR(50)  NULL,
    CreatedDate     DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedBy       NVARCHAR(50)  NULL,
    UpdatedDate     DATETIME2     NULL
);
GO

CREATE TABLE DepartmentTmp (
    PendingID          INT IDENTITY(1,1) PRIMARY KEY,
    ActionType         NVARCHAR(10)  NOT NULL CHECK (ActionType IN ('CREATE','UPDATE','DELETE')),
    TargetDepartmentID INT           NULL,
    DepartmentName     NVARCHAR(100) NULL,
    ShortName          NVARCHAR(50)  NULL,
    Description        NVARCHAR(500) NULL,
    CodeIMF            NVARCHAR(20)  NULL,
    AgenceID           INT           NULL,
    ManagerId          NVARCHAR(20)  NULL,
    Statut             NVARCHAR(20)  NULL,
    PendingStatus      NVARCHAR(20)  NOT NULL DEFAULT 'PENDING' CHECK (PendingStatus IN ('PENDING','APPROVED','REJECTED')),
    RequestUser        NVARCHAR(20)  NOT NULL,
    RequestDate        DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
    ValidationUser     NVARCHAR(20)  NULL,
    ValidationDate      DATETIME2     NULL,
    RejectionReason    NVARCHAR(500) NULL,
    PreviousData       NVARCHAR(MAX) NULL,
    NewData            NVARCHAR(MAX) NULL
);
GO

-- Every user belongs to one Department (optional, since existing users predate this).
ALTER TABLE Users ADD DepartmentID INT NULL REFERENCES Department(DepartmentID);
GO
