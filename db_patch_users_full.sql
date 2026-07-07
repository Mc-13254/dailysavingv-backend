-- Patch: extend Users (and its Pending counterpart) with the full field
-- set required by the detailed User Management module spec.

ALTER TABLE Users ADD
    FirstName           NVARCHAR(50)    NULL,
    LastName            NVARCHAR(50)    NULL,
    TypeUser            NVARCHAR(30)    NULL,
    DebitMax            DECIMAL(18,2)   NULL,
    CreditMax           DECIMAL(18,2)   NULL,
    ValidationMax       DECIMAL(18,2)   NULL,
    PlafondCollect      DECIMAL(18,2)   NULL,
    Caution             DECIMAL(18,2)   NULL,
    Signe               NVARCHAR(MAX)   NULL,
    UserValidation      NVARCHAR(20)    NULL,
    DateValidation      DATETIME2       NULL,
    LastUserModif       NVARCHAR(20)    NULL,
    DateModification    DATETIME2       NULL,
    LastDateSupervise   DATETIME2       NULL,
    LastUserSupervise   NVARCHAR(20)    NULL;
GO

ALTER TABLE UsersTmp ADD
    FirstName           NVARCHAR(50)    NULL,
    LastName            NVARCHAR(50)    NULL,
    TypeUser            NVARCHAR(30)    NULL,
    DebitMax            DECIMAL(18,2)   NULL,
    CreditMax           DECIMAL(18,2)   NULL,
    ValidationMax       DECIMAL(18,2)   NULL,
    PlafondCollect      DECIMAL(18,2)   NULL,
    Caution             DECIMAL(18,2)   NULL,
    Signe               NVARCHAR(MAX)   NULL;
GO
