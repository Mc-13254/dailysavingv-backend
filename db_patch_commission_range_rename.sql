-- Patch: rename CommissionRange (and CommissionRangeTmp) columns to match
-- the requested field naming, add the new Description/Minimum/Maximum/
-- LastUserModif/DateModification fields, and update the overlap-prevention
-- trigger + check constraint to use the new column names.

-- ---- CommissionRange ----
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

-- ---- CommissionRangeTmp ----
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

-- ---- Drop and recreate the check constraint + overlap trigger with new column names ----
-- (constraint/trigger names may differ slightly on your server; adjust if needed)
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_CommissionRange_MinMax')
    ALTER TABLE CommissionRange DROP CONSTRAINT CK_CommissionRange_MinMax;
GO
ALTER TABLE CommissionRange ADD CONSTRAINT CK_CommissionRange_MinMax CHECK (Inf < Sup);
GO

IF EXISTS (SELECT 1 FROM sys.triggers WHERE name = 'TRG_CommissionRange_NoOverlap')
    DROP TRIGGER TRG_CommissionRange_NoOverlap;
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
