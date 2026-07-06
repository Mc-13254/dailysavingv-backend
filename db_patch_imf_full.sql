-- Patch: extend IMF (and its Pending counterpart) with the full field set
-- required by the detailed IMF module spec (contact, location, business
-- settings, audit, logo).

ALTER TABLE IMF ADD
    ShortName           NVARCHAR(50)    NULL,
    RegistrationNumber  NVARCHAR(50)    NULL,
    TaxNumber           NVARCHAR(50)    NULL,
    PrimaryPhone        NVARCHAR(30)    NULL,
    SecondaryPhone      NVARCHAR(30)    NULL,
    Email               NVARCHAR(150)   NULL,
    Website             NVARCHAR(150)   NULL,
    PaysID              INT             NULL,
    VilleID             INT             NULL,
    Address             NVARCHAR(200)   NULL,
    PostalCode          NVARCHAR(20)    NULL,
    CurrencyCode        NVARCHAR(10)    NULL,
    Language            NVARCHAR(10)    NULL,
    Timezone            NVARCHAR(50)    NULL,
    LogoBase64          NVARCHAR(MAX)   NULL,
    Description         NVARCHAR(500)   NULL,
    UpdatedBy           NVARCHAR(50)    NULL,
    UpdatedDate         DATETIME2       NULL;
GO

ALTER TABLE IMF ADD CONSTRAINT FK_IMF_Pays FOREIGN KEY (PaysID) REFERENCES Pays(PaysID);
ALTER TABLE IMF ADD CONSTRAINT FK_IMF_Ville FOREIGN KEY (VilleID) REFERENCES Ville(VilleID);
GO

ALTER TABLE IMFTmp ADD
    ShortName           NVARCHAR(50)    NULL,
    RegistrationNumber  NVARCHAR(50)    NULL,
    TaxNumber           NVARCHAR(50)    NULL,
    PrimaryPhone        NVARCHAR(30)    NULL,
    SecondaryPhone      NVARCHAR(30)    NULL,
    Email               NVARCHAR(150)   NULL,
    Website             NVARCHAR(150)   NULL,
    PaysID              INT             NULL,
    VilleID             INT             NULL,
    Address             NVARCHAR(200)   NULL,
    PostalCode          NVARCHAR(20)    NULL,
    CurrencyCode        NVARCHAR(10)    NULL,
    Language            NVARCHAR(10)    NULL,
    Timezone            NVARCHAR(50)    NULL,
    LogoBase64          NVARCHAR(MAX)   NULL,
    Description         NVARCHAR(500)   NULL;
GO
