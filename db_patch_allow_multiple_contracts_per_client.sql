-- Patch: allow a client to hold multiple active contracts (and therefore
-- multiple accounts, one per contract). Previously a unique index enforced
-- at most one ACTIVE contract per client, which was too restrictive.
--
-- IDEMPOTENT: safe to re-run.

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_Contract_Client_Active' AND object_id = OBJECT_ID('Contract'))
    DROP INDEX UQ_Contract_Client_Active ON Contract;
GO
