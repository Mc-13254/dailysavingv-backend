-- Patch (v4 - idempotent, complete): also handles CK_CommissionRange_Fixed,
-- the mutual-exclusivity constraint between FixedAmount/PercentageRate that
-- was blocking their rename. Safe to re-run regardless of previous state.

-- ---- 1) Drop dependent objects first (safe if already gone) ----
IF EXISTS (SELECT 1 FROM sys.triggers WHERE name = 'TRG_CommissionRange_NoOverlap')
    DROP TRIGGER TRG_CommissionRange_NoOverlap;
GO

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_CommissionRange_MinMax')
    ALTER TABLE CommissionRange DROP CONSTRAINT CK_CommissionRange_MinMax;
GO

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_CommissionRange_Fixed')
    ALTER TABLE CommissionRange DROP CONSTRAINT CK_CommissionRange_Fixed;
GO

-- ---- 2) Rename CommissionRange columns (only if old name still exists) ----
IF COL_LENGTH('CommissionRange','MinAmount') IS NOT NULL
    EXEC sp_rename 'CommissionRange.MinAmount', 'Inf', 'COLUMN';
IF COL_LENGTH('CommissionRange','MaxAmount') IS NOT NULL
    EXEC sp_rename 'CommissionRange.MaxAmount', 'Sup', 'COLUMN';
IF COL_LENGTH('CommissionRange','FixedAmount') IS NOT NULL
    EXEC sp_rename 'CommissionRange.FixedAmount', 'Fixe', 'COLUMN';
IF COL_LENGTH('CommissionRange','PercentageRate') IS NOT NULL
    EXEC sp_rename 'CommissionRange.PercentageRate', 'TAUX', 'COLUMN';
IF COL_LENGTH('CommissionRange','Currency') IS NOT NULL
    EXEC sp_rename 'CommissionRange.Currency', 'CodeU', 'COLUMN';
IF COL_LENGTH('CommissionRange','CreatedBy') IS NOT NULL
    EXEC sp_rename 'CommissionRange.CreatedBy', 'UserCreate', 'COLUMN';
IF COL_LENGTH('CommissionRange','CreatedDate') IS NOT NULL
    EXEC sp_rename 'CommissionRange.CreatedDate', 'CreateDate', 'COLUMN';
IF COL_LENGTH('CommissionRange','ValidatedBy') IS NOT NULL
    EXEC sp_rename 'CommissionRange.ValidatedBy', 'UserVal', 'COLUMN';
GO

IF COL_LENGTH('CommissionRange','Description') IS NULL
    ALTER TABLE CommissionRange ADD Description NVARCHAR(300) NULL;
IF COL_LENGTH('CommissionRange','Minimum') IS NULL
    ALTER TABLE CommissionRange ADD Minimum DECIMAL(18,2) NULL;
IF COL_LENGTH('CommissionRange','Maximum') IS NULL
    ALTER TABLE CommissionRange ADD Maximum DECIMAL(18,2) NULL;
IF COL_LENGTH('CommissionRange','LastUserModif') IS NULL
    ALTER TABLE CommissionRange ADD LastUserModif NVARCHAR(20) NULL;
IF COL_LENGTH('CommissionRange','DateModification') IS NULL
    ALTER TABLE CommissionRange ADD DateModification DATETIME2 NULL;
GO

-- ---- 3) Rename CommissionRangeTmp columns (only if old name still exists) ----
IF COL_LENGTH('CommissionRangeTmp','MinAmount') IS NOT NULL
    EXEC sp_rename 'CommissionRangeTmp.MinAmount', 'Inf', 'COLUMN';
IF COL_LENGTH('CommissionRangeTmp','MaxAmount') IS NOT NULL
    EXEC sp_rename 'CommissionRangeTmp.MaxAmount', 'Sup', 'COLUMN';
IF COL_LENGTH('CommissionRangeTmp','FixedAmount') IS NOT NULL
    EXEC sp_rename 'CommissionRangeTmp.FixedAmount', 'Fixe', 'COLUMN';
IF COL_LENGTH('CommissionRangeTmp','PercentageRate') IS NOT NULL
    EXEC sp_rename 'CommissionRangeTmp.PercentageRate', 'TAUX', 'COLUMN';
IF COL_LENGTH('CommissionRangeTmp','Currency') IS NOT NULL
    EXEC sp_rename 'CommissionRangeTmp.Currency', 'CodeU', 'COLUMN';
GO

IF COL_LENGTH('CommissionRangeTmp','Description') IS NULL
    ALTER TABLE CommissionRangeTmp ADD Description NVARCHAR(300) NULL;
IF COL_LENGTH('CommissionRangeTmp','Minimum') IS NULL
    ALTER TABLE CommissionRangeTmp ADD Minimum DECIMAL(18,2) NULL;
IF COL_LENGTH('CommissionRangeTmp','Maximum') IS NULL
    ALTER TABLE CommissionRangeTmp ADD Maximum DECIMAL(18,2) NULL;
GO

-- ---- 4) Recreate the check constraints + overlap trigger with the new column names ----
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_CommissionRange_MinMax')
    ALTER TABLE CommissionRange ADD CONSTRAINT CK_CommissionRange_MinMax CHECK (Inf < Sup);
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_CommissionRange_Fixed')
    ALTER TABLE CommissionRange ADD CONSTRAINT CK_CommissionRange_Fixed CHECK (
        (CalculationMethod = 'FIXED'      AND Fixe IS NOT NULL AND TAUX IS NULL) OR
        (CalculationMethod = 'PERCENTAGE' AND TAUX IS NOT NULL AND Fixe IS NULL)
    );
GO

IF NOT EXISTS (SELECT 1 FROM sys.triggers WHERE name = 'TRG_CommissionRange_NoOverlap')
EXEC('
CREATE TRIGGER TRG_CommissionRange_NoOverlap
ON CommissionRange
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN CommissionRange cr
            ON cr.CommissionTypeID = i.CommissionTypeID
            AND cr.CommissionRangeID <> i.CommissionRangeID
            AND cr.Statut = ''ACTIVE''
            AND i.Inf <= cr.Sup
            AND i.Sup >= cr.Inf
    )
    BEGIN
        RAISERROR(''Overlapping Commission Range detected for this Commission Type.'', 16, 1);
        ROLLBACK TRANSACTION;
    END
END;
');
GO

-- ---- 5) Sanity check: confirm the final column list and constraints ----
SELECT name FROM sys.columns WHERE object_id = OBJECT_ID('CommissionRange') ORDER BY column_id;
SELECT name, definition FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID('CommissionRange');
