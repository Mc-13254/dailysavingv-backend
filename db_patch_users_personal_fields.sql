-- Patch: extend Users (and its Pending counterpart) with the remaining
-- personal/location fields from the detailed User module spec.

ALTER TABLE Users ADD
    Gender          NVARCHAR(10)  NULL,
    DateOfBirth     DATE          NULL,
    Nationality     NVARCHAR(50)  NULL,
    MaritalStatus   NVARCHAR(20)  NULL,
    Department      NVARCHAR(50)  NULL,
    JobTitle        NVARCHAR(50)  NULL,
    PaysID          INT           NULL,
    VilleID         INT           NULL;
GO

ALTER TABLE Users ADD CONSTRAINT FK_Users_Pays FOREIGN KEY (PaysID) REFERENCES Pays(PaysID);
ALTER TABLE Users ADD CONSTRAINT FK_Users_Ville FOREIGN KEY (VilleID) REFERENCES Ville(VilleID);
GO

ALTER TABLE UsersTmp ADD
    Gender          NVARCHAR(10)  NULL,
    DateOfBirth     DATE          NULL,
    Nationality     NVARCHAR(50)  NULL,
    MaritalStatus   NVARCHAR(20)  NULL,
    Department      NVARCHAR(50)  NULL,
    JobTitle        NVARCHAR(50)  NULL,
    PaysID          INT           NULL,
    VilleID         INT           NULL;
GO
