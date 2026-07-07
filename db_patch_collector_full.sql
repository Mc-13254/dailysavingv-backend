-- Patch: extend Collector (and its Maker-Checker CollectorTMP) with the
-- fields required by the full Collector Management spec: Surname, linked
-- Contract Type/Commission Type/Commission Range/Supervisor/Department,
-- financial limits, and the full audit trail (matching the pattern already
-- used on Users).

-- ---- Collector ----
EXEC sp_rename 'Collector.CreatedBy', 'UserCreate', 'COLUMN';
EXEC sp_rename 'Collector.CreatedDate', 'CreateDate', 'COLUMN';
GO

ALTER TABLE Collector ADD
    Surname             NVARCHAR(100)  NULL,
    DepartmentID        INT            NULL REFERENCES Department(DepartmentID),
    CDETAT              NVARCHAR(20)   NOT NULL DEFAULT 'ACTIVE',
    Caution             DECIMAL(18,2)  NULL,
    ContractID          INT            NULL REFERENCES ContractType(ContractTypeID),
    CommissionTypeID    INT            NULL REFERENCES CommissionType(CommissionTypeID),
    CommissionRangeID   INT            NULL REFERENCES CommissionRange(CommissionRangeID),
    SupervisorId        NVARCHAR(20)   NULL REFERENCES Users(CodeUser),
    CollectMonth        DECIMAL(18,2)  NULL,
    CollectDay          DECIMAL(18,2)  NULL,
    RetraitMonth        DECIMAL(18,2)  NULL,
    RetraitDay          DECIMAL(18,2)  NULL,
    UserValidation      NVARCHAR(20)   NULL,
    DateValidation      DATETIME2      NULL,
    LastUserModif       NVARCHAR(20)   NULL,
    DateModification    DATETIME2      NULL,
    LastUserSupervise   NVARCHAR(20)   NULL,
    LastDateSupervise   DATETIME2      NULL;
GO

-- ---- CollectorTMP ----
ALTER TABLE CollectorTMP ADD
    Surname             NVARCHAR(100)  NULL,
    DepartmentID        INT            NULL,
    CDETAT              NVARCHAR(20)   NULL,
    Caution             DECIMAL(18,2)  NULL,
    ContractID          INT            NULL,
    CommissionTypeID    INT            NULL,
    CommissionRangeID   INT            NULL,
    SupervisorId        NVARCHAR(20)   NULL,
    CollectMonth        DECIMAL(18,2)  NULL,
    CollectDay          DECIMAL(18,2)  NULL,
    RetraitMonth        DECIMAL(18,2)  NULL,
    RetraitDay          DECIMAL(18,2)  NULL;
GO

-- One User can only become one Collector.
ALTER TABLE Collector ADD CONSTRAINT UQ_Collector_CodeUser UNIQUE (CodeUser);
GO
