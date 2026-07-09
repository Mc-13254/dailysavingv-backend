-- Patch: bill/coin ("denomination") breakdown for cash handling — mirrors real
-- banking cash counting. Stored as JSON (denomination value -> quantity) since
-- it's supplementary proof of counting, not the authoritative amount.
--
-- IDEMPOTENT: safe to re-run.

IF COL_LENGTH('Transactions', 'CashBreakdownJson') IS NULL
    ALTER TABLE Transactions ADD CashBreakdownJson NVARCHAR(MAX) NULL;
GO

IF COL_LENGTH('CashSession', 'PhysicalCashBreakdownJson') IS NULL
    ALTER TABLE CashSession ADD PhysicalCashBreakdownJson NVARCHAR(MAX) NULL;
GO
