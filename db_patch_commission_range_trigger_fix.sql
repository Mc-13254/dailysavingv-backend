-- Patch: the overlap-prevention trigger was checking for overlap even when
-- the row being updated was being set to INACTIVE (e.g. during a Maker-
-- Checker soft-delete approval). Deactivating a range can never legitimately
-- overlap-conflict, so the trigger must only run its check when the updated
-- row is itself ACTIVE.

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
            AND i.Statut = 'ACTIVE'
            AND i.Inf <= cr.Sup
            AND i.Sup >= cr.Inf
    )
    BEGIN
        RAISERROR('Overlapping Commission Range detected for this Commission Type.', 16, 1);
        ROLLBACK TRANSACTION;
    END
END;
GO
