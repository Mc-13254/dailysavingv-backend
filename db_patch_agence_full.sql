-- Patch: extend Agence (and its Pending counterpart) with the full field set
-- required by the detailed Agency module spec.

ALTER TABLE Agence ADD
    ShortName       NVARCHAR(50)    NULL,
    Description     NVARCHAR(500)   NULL,
    LogoBase64      NVARCHAR(MAX)   NULL,
    PrimaryPhone    NVARCHAR(30)    NULL,
    SecondaryPhone  NVARCHAR(30)    NULL,
    Email           NVARCHAR(150)   NULL,
    Website         NVARCHAR(150)   NULL,
    PaysID          INT             NULL,
    Address         NVARCHAR(200)   NULL,
    PostalCode      NVARCHAR(20)    NULL,
    ManagerId       NVARCHAR(20)    NULL,
    OpeningDate     DATE            NULL,
    UpdatedBy       NVARCHAR(50)    NULL,
    UpdatedDate     DATETIME2       NULL;
GO

ALTER TABLE Agence ADD CONSTRAINT FK_Agence_Pays FOREIGN KEY (PaysID) REFERENCES Pays(PaysID);
ALTER TABLE Agence ADD CONSTRAINT FK_Agence_Manager FOREIGN KEY (ManagerId) REFERENCES Users(CodeUser);
GO

ALTER TABLE AgenceTmp ADD
    ShortName       NVARCHAR(50)    NULL,
    Description     NVARCHAR(500)   NULL,
    LogoBase64      NVARCHAR(MAX)   NULL,
    PrimaryPhone    NVARCHAR(30)    NULL,
    SecondaryPhone  NVARCHAR(30)    NULL,
    Email           NVARCHAR(150)   NULL,
    Website         NVARCHAR(150)   NULL,
    PaysID          INT             NULL,
    Address         NVARCHAR(200)   NULL,
    PostalCode      NVARCHAR(20)    NULL,
    ManagerId       NVARCHAR(20)    NULL,
    OpeningDate     DATE            NULL;
GO
