-- Patch: the overlap trigger was re-validating on every UPDATE, even when
-- unrelated columns (e.g. Description) were the only ones changed. If any
-- pre-existing rows already overlap (e.g. from earlier testing before the
-- trigger fixes), this blocked completely unrelated edits. Only re-check
-- overlap when Inf, Sup, Statut, or CommissionTypeID actually changed.

IF EXISTS (SELECT 1 FROM sys.triggers WHERE name = 'TRG_CommissionRange_NoOverlap')
    DROP TRIGGER TRG_CommissionRange_NoOverlap;
GO

CREATE TRIGGER TRG_CommissionRange_NoOverlap
ON CommissionRange
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- Skip the check entirely for edits that don't touch the range
    -- boundaries, its status, or its Commission Type (e.g. Description-only
    -- edits, or audit-field-only updates from Maker-Checker approvals).
    IF NOT (UPDATE(Inf) OR UPDATE(Sup) OR UPDATE(Statut) OR UPDATE(CommissionTypeID))
        RETURN;

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

-- Sanity check: find any ranges that ALREADY overlap right now (leftover
-- from earlier testing before this trigger existed). If any come back,
-- you'll want to deactivate/adjust one of each conflicting pair manually.
SELECT a.CommissionRangeID AS RangeA, b.CommissionRangeID AS RangeB,
       a.CommissionTypeID, a.Inf AS Inf_A, a.Sup AS Sup_A, b.Inf AS Inf_B, b.Sup AS Sup_B
FROM CommissionRange a
JOIN CommissionRange b
    ON a.CommissionTypeID = b.CommissionTypeID
    AND a.CommissionRangeID < b.CommissionRangeID
    AND a.Statut = 'ACTIVE' AND b.Statut = 'ACTIVE'
    AND a.Inf <= b.Sup AND a.Sup >= b.Inf;
