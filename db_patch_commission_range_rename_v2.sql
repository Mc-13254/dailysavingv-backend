-- Patch (v2 - corrected ordering): rename CommissionRange columns.
-- SQL Server refuses to rename a column that a CHECK constraint or TRIGGER
-- still references ("enforced dependencies"), so we must drop those first,
-- rename the columns, then recreate them with the new names.

-- ---- 1) Drop dependent objects first ----
IF EXISTS (SELECT 1 FROM sys.triggers WHERE name = 'TRG_CommissionRange_NoOverlap')
    DROP TRIGGER TRG_CommissionRange_NoOverlap;
GO

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_CommissionRange_MinMax')
    ALTER TABLE CommissionRange DROP CONSTRAINT CK_CommissionRange_MinMax;
GO

-- ---- 2) Rename CommissionRange columns ----
EXEC sp_rename 'CommissionRange.MinAmount', 'Inf', 'COLUMN';
EXEC sp_rename 'CommissionRange.MaxAmount', 'Sup', 'COLUMN';
EXEC sp_rename 'CommissionRange.FixedAmount', 'Fixe', 'COLUMN';
EXEC sp_rename 'CommissionRange.PercentageRate', 'TAUX', 'COLUMN';
EXEC sp_rename 'CommissionRange.Currency', 'CodeU', 'COLUMN';
EXEC sp_rename 'CommissionRange.CreatedBy', 'UserCreate', 'COLUMN';
EXEC sp_rename 'CommissionRange.CreatedDate', 'CreateDate', 'COLUMN';
EXEC sp_rename 'CommissionRange.ValidatedBy', 'UserVal', 'COLUMN';
GO

ALTER TABLE CommissionRange ADD
    Description       NVARCHAR(300)  NULL,
    Minimum            DECIMAL(18,2)  NULL,
    Maximum            DECIMAL(18,2)  NULL,
    LastUserModif      NVARCHAR(20)   NULL,
    DateModification   DATETIME2      NULL;
GO

-- ---- 3) Rename CommissionRangeTmp columns (no constraints/triggers on this one) ----
EXEC sp_rename 'CommissionRangeTmp.MinAmount', 'Inf', 'COLUMN';
EXEC sp_rename 'CommissionRangeTmp.MaxAmount', 'Sup', 'COLUMN';
EXEC sp_rename 'CommissionRangeTmp.FixedAmount', 'Fixe', 'COLUMN';
EXEC sp_rename 'CommissionRangeTmp.PercentageRate', 'TAUX', 'COLUMN';
EXEC sp_rename 'CommissionRangeTmp.Currency', 'CodeU', 'COLUMN';
GO

ALTER TABLE CommissionRangeTmp ADD
    Description   NVARCHAR(300)  NULL,
    Minimum       DECIMAL(18,2)  NULL,
    Maximum       DECIMAL(18,2)  NULL;
GO

-- ---- 4) Recreate the check constraint + overlap trigger with the new column names ----
ALTER TABLE CommissionRange ADD CONSTRAINT CK_CommissionRange_MinMax CHECK (Inf < Sup);
GO

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
            AND cr.Statut = 'ACTIVE'
            AND i.Inf <= cr.Sup
            AND i.Sup >= cr.Inf
    )
    BEGIN
        RAISERROR('Overlapping Commission Range detected for this Commission Type.', 16, 1);
        ROLLBACK TRANSACTION;
    END
END;
GO
