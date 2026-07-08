-- Patch: Collector Assignment (world map / zones) + Performance Dashboard modules.
--
-- Extends the existing ZoneCollecte table with GPS/hierarchy fields, adds
-- ZoneCollecteID to Client, and creates 3 new tables:
--   CollectorZoneAssignment  (Collector <-> Zone, many zones per collector,
--                             at most one ACTIVE collector per zone)
--   ZoneAssignmentHistory    (append-only audit trail, never deleted)
--   CollectorTarget          (Daily/Weekly/Monthly targets for the Performance
--                             dashboard's "Target Achievement" KPIs)
--
-- The Performance dashboard itself needs NO new collection/commission tables:
-- it reads directly from the existing Transactions table
-- (TransactionType = 'DAILY_COLLECTION'), which already has CollectorID,
-- ClientID, Montant, MontantCommission, DateTransaction, Statut.
--
-- SAFE TO RUN on the existing DailySavingV database (adds columns/tables only).

-- 1. Extend ZoneCollecte -------------------------------------------------
ALTER TABLE ZoneCollecte ADD Description NVARCHAR(500) NULL;
ALTER TABLE ZoneCollecte ADD District NVARCHAR(150) NULL;
ALTER TABLE ZoneCollecte ADD Neighborhood NVARCHAR(150) NULL;
ALTER TABLE ZoneCollecte ADD Village NVARCHAR(150) NULL;
ALTER TABLE ZoneCollecte ADD Latitude DECIMAL(9,6) NULL;
ALTER TABLE ZoneCollecte ADD Longitude DECIMAL(9,6) NULL;
ALTER TABLE ZoneCollecte ADD ShapeType NVARCHAR(20) NULL;          -- Polygon | Circle | Rectangle | Point
ALTER TABLE ZoneCollecte ADD PolygonCoordinates NVARCHAR(MAX) NULL;
ALTER TABLE ZoneCollecte ADD RadiusMeters DECIMAL(10,2) NULL;
ALTER TABLE ZoneCollecte ADD AgenceID INT NULL;
GO

ALTER TABLE ZoneCollecte ADD CONSTRAINT FK_ZoneCollecte_Agence
    FOREIGN KEY (AgenceID) REFERENCES Agence(AgenceID);
GO

-- 2. Client -> Zone ------------------------------------------------------
ALTER TABLE Client ADD ZoneCollecteID INT NULL;
GO

ALTER TABLE Client ADD CONSTRAINT FK_Client_ZoneCollecte
    FOREIGN KEY (ZoneCollecteID) REFERENCES ZoneCollecte(ZoneCollecteID);
GO

-- 3. CollectorZoneAssignment ---------------------------------------------
IF OBJECT_ID('dbo.CollectorZoneAssignment', 'U') IS NULL
BEGIN
    CREATE TABLE CollectorZoneAssignment (
        AssignmentID    INT IDENTITY(1,1) PRIMARY KEY,
        CollectorID     NVARCHAR(20) NOT NULL,
        ZoneCollecteID  INT NOT NULL,
        Status          NVARCHAR(20) NOT NULL DEFAULT 'ACTIVE',   -- ACTIVE | ENDED
        AssignmentDate  DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        EndDate         DATETIME2 NULL,
        AssignedBy      NVARCHAR(50) NULL,
        CONSTRAINT FK_CZA_Collector FOREIGN KEY (CollectorID) REFERENCES Collector(CollectorID),
        CONSTRAINT FK_CZA_Zone FOREIGN KEY (ZoneCollecteID) REFERENCES ZoneCollecte(ZoneCollecteID)
    );

    -- Business rule: at most one ACTIVE collector per zone at a time.
    CREATE UNIQUE INDEX UQ_Zone_Active_Collector
        ON CollectorZoneAssignment(ZoneCollecteID)
        WHERE Status = 'ACTIVE';

    CREATE INDEX IX_CZA_CollectorID ON CollectorZoneAssignment(CollectorID);
END
GO

-- 4. ZoneAssignmentHistory (append-only, never deleted) ------------------
IF OBJECT_ID('dbo.ZoneAssignmentHistory', 'U') IS NULL
BEGIN
    CREATE TABLE ZoneAssignmentHistory (
        HistoryID       BIGINT IDENTITY(1,1) PRIMARY KEY,
        CollectorID     NVARCHAR(20) NOT NULL,
        ZoneCollecteID  INT NOT NULL,
        ClientID        NVARCHAR(20) NULL,
        EventType       NVARCHAR(30) NOT NULL, -- ZONE_ASSIGNED | ZONE_REMOVED | CLIENT_ASSIGNED | CLIENT_TRANSFERRED
        FromCollectorID NVARCHAR(20) NULL,
        EventDate       DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        ActionBy        NVARCHAR(50) NULL
    );
    CREATE INDEX IX_ZAH_CollectorID ON ZoneAssignmentHistory(CollectorID);
    CREATE INDEX IX_ZAH_ZoneID ON ZoneAssignmentHistory(ZoneCollecteID);
END
GO

-- 5. CollectorTarget ------------------------------------------------------
IF OBJECT_ID('dbo.CollectorTarget', 'U') IS NULL
BEGIN
    CREATE TABLE CollectorTarget (
        TargetID       INT IDENTITY(1,1) PRIMARY KEY,
        CollectorID    NVARCHAR(20) NOT NULL,
        PeriodType     NVARCHAR(10) NOT NULL,  -- DAILY | WEEKLY | MONTHLY
        PeriodStart    DATE NOT NULL,
        TargetAmount   DECIMAL(18,2) NOT NULL,
        CreatedBy      NVARCHAR(50) NULL,
        CreatedDate    DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_CollectorTarget_Collector FOREIGN KEY (CollectorID) REFERENCES Collector(CollectorID),
        CONSTRAINT UQ_CollectorTarget UNIQUE (CollectorID, PeriodType, PeriodStart)
    );
END
GO
