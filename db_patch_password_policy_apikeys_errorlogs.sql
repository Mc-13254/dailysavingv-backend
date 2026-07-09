-- Patch: Password Policy, Password History, API Keys, Error Logs, and
-- MustChangePassword tracking on Users.
-- IDEMPOTENT: safe to re-run.

IF COL_LENGTH('Users', 'MustChangePassword') IS NULL
    ALTER TABLE Users ADD MustChangePassword BIT NOT NULL DEFAULT 0;
IF COL_LENGTH('Users', 'PasswordChangedDate') IS NULL
    ALTER TABLE Users ADD PasswordChangedDate DATETIME2 NULL;
GO

IF OBJECT_ID('dbo.PasswordPolicy', 'U') IS NULL
BEGIN
    CREATE TABLE PasswordPolicy (
        PasswordPolicyID       INT IDENTITY(1,1) PRIMARY KEY,
        MinimumLength           INT NOT NULL DEFAULT 8,
        MaximumLength           INT NOT NULL DEFAULT 64,
        RequireUppercase        BIT NOT NULL DEFAULT 1,
        RequireLowercase        BIT NOT NULL DEFAULT 1,
        RequireNumber           BIT NOT NULL DEFAULT 1,
        RequireSpecialCharacter BIT NOT NULL DEFAULT 1,
        PasswordExpirationDays  INT NOT NULL DEFAULT 90,
        PasswordHistoryCount    INT NOT NULL DEFAULT 5,
        UpdatedBy               NVARCHAR(50) NULL,
        UpdatedDate             DATETIME2 NULL
    );
    -- Seed exactly one default policy row.
    INSERT INTO PasswordPolicy (MinimumLength, MaximumLength, RequireUppercase, RequireLowercase, RequireNumber, RequireSpecialCharacter, PasswordExpirationDays, PasswordHistoryCount)
    VALUES (8, 64, 1, 1, 1, 1, 90, 5);
END
GO

IF OBJECT_ID('dbo.PasswordHistory', 'U') IS NULL
BEGIN
    CREATE TABLE PasswordHistory (
        PasswordHistoryID INT IDENTITY(1,1) PRIMARY KEY,
        CodeUser          NVARCHAR(20) NOT NULL,
        PasswordHash      NVARCHAR(200) NOT NULL,
        ChangedDate       DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        ChangedBy         NVARCHAR(50) NULL,
        CONSTRAINT FK_PasswordHistory_User FOREIGN KEY (CodeUser) REFERENCES Users(CodeUser)
    );
    CREATE INDEX IX_PasswordHistory_CodeUser ON PasswordHistory(CodeUser);
END
GO

IF OBJECT_ID('dbo.ApiKeys', 'U') IS NULL
BEGIN
    CREATE TABLE ApiKeys (
        ApiKeyID    INT IDENTITY(1,1) PRIMARY KEY,
        Name        NVARCHAR(100) NOT NULL,
        KeyHash     NVARCHAR(200) NOT NULL,
        KeyPrefix   NVARCHAR(20) NOT NULL,
        Description NVARCHAR(300) NULL,
        ExpiryDate  DATETIME2 NULL,
        IsActive    BIT NOT NULL DEFAULT 1,
        LastUsedDate DATETIME2 NULL,
        CreatedBy   NVARCHAR(50) NOT NULL,
        CreatedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        RevokedBy   NVARCHAR(50) NULL,
        RevokedDate DATETIME2 NULL
    );
END
GO

IF OBJECT_ID('dbo.ErrorLogs', 'U') IS NULL
BEGIN
    CREATE TABLE ErrorLogs (
        ErrorLogID    BIGINT IDENTITY(1,1) PRIMARY KEY,
        Message       NVARCHAR(1000) NOT NULL,
        ExceptionType NVARCHAR(200) NULL,
        StackTrace    NVARCHAR(MAX) NULL,
        RequestPath   NVARCHAR(300) NULL,
        RequestMethod NVARCHAR(10) NULL,
        CodeUser      NVARCHAR(20) NULL,
        IPAddress     NVARCHAR(50) NULL,
        OccurredDate  DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
    CREATE INDEX IX_ErrorLogs_OccurredDate ON ErrorLogs(OccurredDate);
END
GO
