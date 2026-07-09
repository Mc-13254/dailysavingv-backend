-- Patch: Failed Login Attempts, Account Lockout, and Active Sessions metadata.
-- IDEMPOTENT: safe to re-run.

IF COL_LENGTH('Users', 'FailedLoginAttempts') IS NULL
    ALTER TABLE Users ADD FailedLoginAttempts INT NOT NULL DEFAULT 0;
IF COL_LENGTH('Users', 'AccountLocked') IS NULL
    ALTER TABLE Users ADD AccountLocked BIT NOT NULL DEFAULT 0;
IF COL_LENGTH('Users', 'LockReason') IS NULL
    ALTER TABLE Users ADD LockReason NVARCHAR(200) NULL;
IF COL_LENGTH('Users', 'LockedDate') IS NULL
    ALTER TABLE Users ADD LockedDate DATETIME2 NULL;
IF COL_LENGTH('Users', 'LockedBy') IS NULL
    ALTER TABLE Users ADD LockedBy NVARCHAR(50) NULL;
GO

IF COL_LENGTH('RefreshTokens', 'IPAddress') IS NULL
    ALTER TABLE RefreshTokens ADD IPAddress NVARCHAR(50) NULL;
IF COL_LENGTH('RefreshTokens', 'UserAgent') IS NULL
    ALTER TABLE RefreshTokens ADD UserAgent NVARCHAR(300) NULL;
IF COL_LENGTH('RefreshTokens', 'TerminationReason') IS NULL
    ALTER TABLE RefreshTokens ADD TerminationReason NVARCHAR(200) NULL;
IF COL_LENGTH('RefreshTokens', 'TerminatedBy') IS NULL
    ALTER TABLE RefreshTokens ADD TerminatedBy NVARCHAR(50) NULL;
GO

IF OBJECT_ID('dbo.FailedLoginAttempts', 'U') IS NULL
BEGIN
    CREATE TABLE FailedLoginAttempts (
        AttemptID      INT IDENTITY(1,1) PRIMARY KEY,
        Username       NVARCHAR(50) NOT NULL,
        CodeUser       NVARCHAR(20) NULL,
        FailureReason  NVARCHAR(30) NOT NULL,
        RiskLevel      NVARCHAR(15) NOT NULL DEFAULT 'LOW',
        IPAddress      NVARCHAR(50) NULL,
        UserAgent      NVARCHAR(300) NULL,
        AttemptDate    DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
    CREATE INDEX IX_FailedLoginAttempts_Username ON FailedLoginAttempts(Username);
    CREATE INDEX IX_FailedLoginAttempts_AttemptDate ON FailedLoginAttempts(AttemptDate);
END
GO
