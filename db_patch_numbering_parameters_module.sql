-- Patch: create the Numbering Parameters module (Business Parameters ->
-- Numbering Parameters) with seed rows for the entities already used
-- elsewhere in the system.

CREATE TABLE NumberingParameter (
    NumberingParameterID INT IDENTITY(1,1) PRIMARY KEY,
    EntityName           NVARCHAR(50)  NOT NULL UNIQUE,
    Prefix               NVARCHAR(20)  NOT NULL UNIQUE,
    Suffix               NVARCHAR(20)  NULL,
    Separator            NVARCHAR(5)   NULL,
    CurrentNumber        BIGINT        NOT NULL DEFAULT 0,
    StartingNumber       BIGINT        NOT NULL DEFAULT 1,
    NumberLength         INT           NOT NULL DEFAULT 6,
    PaddingCharacter     NVARCHAR(1)   NOT NULL DEFAULT '0',
    AllowReset           BIT           NOT NULL DEFAULT 0,
    ResetFrequency       NVARCHAR(20)  NULL,
    NextResetDate        DATETIME2     NULL,
    AutoIncrement        BIT           NOT NULL DEFAULT 1,
    IncrementValue       INT           NOT NULL DEFAULT 1,
    Statut               NVARCHAR(20)  NOT NULL DEFAULT 'ACTIVE',
    CreatedBy            NVARCHAR(50)  NULL,
    CreatedDate          DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedBy            NVARCHAR(50)  NULL,
    UpdatedDate          DATETIME2     NULL
);
GO

INSERT INTO NumberingParameter (EntityName, Prefix, Separator, CurrentNumber, StartingNumber, NumberLength, PaddingCharacter, AutoIncrement, IncrementValue, Statut) VALUES
('Agency',        'AG',  '', 0, 1, 6, '0', 1, 1, 'ACTIVE'),
('Department',    'DEP', '', 0, 1, 3, '0', 1, 1, 'ACTIVE'),
('Role',          'ROL', '', 0, 1, 3, '0', 1, 1, 'ACTIVE'),
('User',          'USR', '', 0, 1, 6, '0', 1, 1, 'ACTIVE'),
('Collector',     'COL', '', 0, 1, 6, '0', 1, 1, 'ACTIVE'),
('Client',        'CLI', '', 0, 1, 6, '0', 1, 1, 'ACTIVE'),
('Contract',      'CTR', '', 0, 1, 6, '0', 1, 1, 'ACTIVE'),
('Transaction',   'TRX', '', 0, 1, 8, '0', 1, 1, 'ACTIVE'),
('Receipt',       'RCP', '', 0, 1, 8, '0', 1, 1, 'ACTIVE'),
('Commission',    'CT',  '', 0, 1, 3, '0', 1, 1, 'ACTIVE'),
('Collection',    'CLC', '', 0, 1, 8, '0', 1, 1, 'ACTIVE'),
('SavingAccount',  'SAV', '', 0, 1, 8, '0', 1, 1, 'ACTIVE');
GO
