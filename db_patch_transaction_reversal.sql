-- Patch: Transaction Reversal requests (Maker-Checker) for Deposits,
-- Withdrawals, Transfers, and Daily Collections.
-- IDEMPOTENT: safe to re-run.

IF OBJECT_ID('dbo.TransactionReversalRequest', 'U') IS NULL
BEGIN
    CREATE TABLE TransactionReversalRequest (
        TransactionReversalRequestID INT IDENTITY(1,1) PRIMARY KEY,
        TransactionID       BIGINT NOT NULL,
        Reason              NVARCHAR(300) NOT NULL,
        Status              NVARCHAR(15) NOT NULL DEFAULT 'PENDING',
        RequestedBy         NVARCHAR(50) NOT NULL,
        RequestDate         DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        ApprovedBy          NVARCHAR(50) NULL,
        ApprovalDate        DATETIME2 NULL,
        RejectionReason     NVARCHAR(300) NULL,
        ReversalTransactionID BIGINT NULL,
        CONSTRAINT FK_TransactionReversalRequest_Transaction FOREIGN KEY (TransactionID) REFERENCES Transactions(TransactionID)
    );
    CREATE INDEX IX_TransactionReversalRequest_TransactionID ON TransactionReversalRequest(TransactionID);
    CREATE INDEX IX_TransactionReversalRequest_Status ON TransactionReversalRequest(Status);
END
GO
