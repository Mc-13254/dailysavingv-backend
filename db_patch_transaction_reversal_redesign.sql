-- Patch: redesign Transaction Reversal as its own two-phase module.
-- Phase 1: maker requests a reversal by Client/Collector/Amount/Reason (no
-- transaction ID needed yet). Phase 2: supervisor approves/rejects the
-- REQUEST itself. Phase 3: once approved, the maker enters the exact
-- TransactionID and executes — the reversal happens immediately, no further
-- approval needed.
-- IDEMPOTENT: safe to re-run.

IF COL_LENGTH('TransactionReversalRequest', 'ClientID') IS NULL
    ALTER TABLE TransactionReversalRequest ADD ClientID NVARCHAR(20) NULL;
IF COL_LENGTH('TransactionReversalRequest', 'CollectorID') IS NULL
    ALTER TABLE TransactionReversalRequest ADD CollectorID NVARCHAR(20) NULL;
IF COL_LENGTH('TransactionReversalRequest', 'Montant') IS NULL
    ALTER TABLE TransactionReversalRequest ADD Montant DECIMAL(18,2) NOT NULL DEFAULT 0;
IF COL_LENGTH('TransactionReversalRequest', 'ExecutedBy') IS NULL
    ALTER TABLE TransactionReversalRequest ADD ExecutedBy NVARCHAR(50) NULL;
IF COL_LENGTH('TransactionReversalRequest', 'ExecutionDate') IS NULL
    ALTER TABLE TransactionReversalRequest ADD ExecutionDate DATETIME2 NULL;
GO

-- TransactionID is now only known once the request is approved and executed.
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('TransactionReversalRequest') AND name = 'TransactionID' AND is_nullable = 0)
    ALTER TABLE TransactionReversalRequest ALTER COLUMN TransactionID BIGINT NULL;
GO
