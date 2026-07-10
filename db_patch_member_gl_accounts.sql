-- Patch: Member/GL companion accounts (auto-created on a client's first
-- account), with annual interest and a withdrawal threshold.
-- IDEMPOTENT: safe to re-run.

IF COL_LENGTH('Accounts', 'AnnualInterestRate') IS NULL
    ALTER TABLE Accounts ADD AnnualInterestRate DECIMAL(9,4) NULL;
IF COL_LENGTH('Accounts', 'WithdrawalThreshold') IS NULL
    ALTER TABLE Accounts ADD WithdrawalThreshold DECIMAL(18,2) NULL;
IF COL_LENGTH('Accounts', 'LastInterestAppliedDate') IS NULL
    ALTER TABLE Accounts ADD LastInterestAppliedDate DATETIME2 NULL;
GO
