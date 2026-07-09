-- Patch: Document Management + Notification Center.
-- IDEMPOTENT: safe to re-run.

IF OBJECT_ID('dbo.Document', 'U') IS NULL
BEGIN
    CREATE TABLE [Document] (
        DocumentID     INT IDENTITY(1,1) PRIMARY KEY,
        EntityType     NVARCHAR(30) NOT NULL,
        EntityID       NVARCHAR(30) NULL,
        FileName       NVARCHAR(260) NOT NULL,
        FilePath       NVARCHAR(400) NOT NULL,
        FileType       NVARCHAR(20) NULL,
        FileSizeBytes  BIGINT NOT NULL DEFAULT 0,
        Description    NVARCHAR(300) NULL,
        Tags           NVARCHAR(300) NULL,
        AgenceID       INT NOT NULL,
        UploadedBy     NVARCHAR(50) NOT NULL,
        UploadDate     DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        IsDeleted      BIT NOT NULL DEFAULT 0,
        DeletedBy      NVARCHAR(50) NULL,
        DeletedDate    DATETIME2 NULL
    );
    CREATE INDEX IX_Document_EntityType_EntityID ON [Document](EntityType, EntityID);
END
GO

IF OBJECT_ID('dbo.Notification', 'U') IS NULL
BEGIN
    CREATE TABLE Notification (
        NotificationID INT IDENTITY(1,1) PRIMARY KEY,
        CodeUser       NVARCHAR(20) NOT NULL,
        Title          NVARCHAR(150) NOT NULL,
        Message        NVARCHAR(500) NOT NULL,
        Severity       NVARCHAR(15) NOT NULL DEFAULT 'INFO',
        Link           NVARCHAR(200) NULL,
        IsRead         BIT NOT NULL DEFAULT 0,
        CreatedDate    DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        ReadDate       DATETIME2 NULL
    );
    CREATE INDEX IX_Notification_CodeUser_IsRead ON Notification(CodeUser, IsRead);
END
GO
