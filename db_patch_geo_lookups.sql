-- Patch: reference/lookup tables for Currency, Language, TimeZone, plus a
-- starter seed of Pays / Region / Ville so the Country -> City dropdowns in
-- the IMF module (and any other module) are usable immediately via
-- GET /api/geo/countries, /api/geo/cities, /api/geo/currencies,
-- /api/geo/languages, /api/geo/timezones.

CREATE TABLE Currency (
    CurrencyCode    NVARCHAR(10)  PRIMARY KEY,
    Nom             NVARCHAR(50)  NOT NULL,
    Symbole         NVARCHAR(5)   NULL,
    Statut          BIT           NOT NULL DEFAULT 1
);
GO

CREATE TABLE Language (
    LanguageCode    NVARCHAR(10)  PRIMARY KEY,
    Nom             NVARCHAR(50)  NOT NULL,
    Statut          BIT           NOT NULL DEFAULT 1
);
GO

CREATE TABLE TimeZoneRef (
    TimeZoneID      INT IDENTITY(1,1) PRIMARY KEY,
    Code            NVARCHAR(50)  NOT NULL UNIQUE,   -- IANA id, e.g. Africa/Douala
    Label           NVARCHAR(100) NOT NULL,
    UtcOffset       NVARCHAR(10)  NULL,              -- e.g. "+01:00"
    Statut          BIT           NOT NULL DEFAULT 1
);
GO

INSERT INTO Currency (CurrencyCode, Nom, Symbole) VALUES
('XAF','Franc CFA (BEAC)','FCFA'),
('XOF','Franc CFA (BCEAO)','FCFA'),
('USD','Dollar americain','$'),
('EUR','Euro','EUR'),
('GBP','Livre sterling','GBP'),
('NGN','Naira nigerian','NGN');
GO

INSERT INTO Language (LanguageCode, Nom) VALUES
('fr','Francais'),
('en','English');
GO

INSERT INTO TimeZoneRef (Code, Label, UtcOffset) VALUES
('Africa/Douala','Douala (Cameroun)','+01:00'),
('Africa/Abidjan','Abidjan (Cote d''Ivoire)','+00:00'),
('Africa/Dakar','Dakar (Senegal)','+00:00'),
('Africa/Kinshasa','Kinshasa (RDC)','+01:00'),
('Africa/Lagos','Lagos (Nigeria)','+01:00'),
('Europe/Paris','Paris (France)','+01:00'),
('UTC','UTC','+00:00');
GO

-- ---- Geo seed: Pays -> Region -> Ville ----
INSERT INTO Pays (Code, Nom) VALUES
('CM','Cameroun'),
('CI','Cote d''Ivoire'),
('SN','Senegal'),
('CD','Republique Democratique du Congo'),
('NG','Nigeria'),
('FR','France');
GO

INSERT INTO Region (Nom, PaysID)
SELECT 'Littoral', PaysID FROM Pays WHERE Code = 'CM'
UNION ALL SELECT 'Centre', PaysID FROM Pays WHERE Code = 'CM'
UNION ALL SELECT 'Ouest', PaysID FROM Pays WHERE Code = 'CM'
UNION ALL SELECT 'Abidjan', PaysID FROM Pays WHERE Code = 'CI'
UNION ALL SELECT 'Dakar', PaysID FROM Pays WHERE Code = 'SN'
UNION ALL SELECT 'Kinshasa', PaysID FROM Pays WHERE Code = 'CD'
UNION ALL SELECT 'Lagos State', PaysID FROM Pays WHERE Code = 'NG'
UNION ALL SELECT 'Ile-de-France', PaysID FROM Pays WHERE Code = 'FR';
GO

INSERT INTO Ville (Nom, RegionID)
SELECT 'Douala', RegionID FROM Region WHERE Nom = 'Littoral'
UNION ALL SELECT 'Yaounde', RegionID FROM Region WHERE Nom = 'Centre'
UNION ALL SELECT 'Bafoussam', RegionID FROM Region WHERE Nom = 'Ouest'
UNION ALL SELECT 'Abidjan', RegionID FROM Region WHERE Nom = 'Abidjan'
UNION ALL SELECT 'Dakar', RegionID FROM Region WHERE Nom = 'Dakar'
UNION ALL SELECT 'Kinshasa', RegionID FROM Region WHERE Nom = 'Kinshasa'
UNION ALL SELECT 'Lagos', RegionID FROM Region WHERE Nom = 'Lagos State'
UNION ALL SELECT 'Paris', RegionID FROM Region WHERE Nom = 'Ile-de-France';
GO
