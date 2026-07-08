-- Patch v2: add PaysID to ZoneCollecte so a zone can be linked to a country
-- (used by the redesigned "Créer une zone" form: Pays -> Zone de collecte ->
-- Description -> Quartier, where Quartier is now suggested from existing
-- zones in the same country instead of free text the admin has to remember).
--
-- SAFE TO RUN even if db_patch_zone_assignment_module.sql was already applied.

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'ZoneCollecte' AND COLUMN_NAME = 'PaysID'
)
BEGIN
    ALTER TABLE ZoneCollecte ADD PaysID INT NULL;
    ALTER TABLE ZoneCollecte ADD CONSTRAINT FK_ZoneCollecte_Pays
        FOREIGN KEY (PaysID) REFERENCES Pays(PaysID);
END
GO
