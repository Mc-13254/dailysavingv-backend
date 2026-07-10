-- Patch: Fraud Detection (rule-based transaction risk scoring).
-- IDEMPOTENT: safe to re-run.

IF OBJECT_ID('dbo.FraudDetection', 'U') IS NULL
BEGIN
    CREATE TABLE FraudDetection (
        FraudDetectionID INT IDENTITY(1,1) PRIMARY KEY,
        TransactionID     BIGINT NOT NULL,
        Score             INT NOT NULL,
        RiskLevel         NVARCHAR(15) NOT NULL DEFAULT 'LOW',
        FactorsJson       NVARCHAR(MAX) NOT NULL,
        FlaggedForReview  BIT NOT NULL DEFAULT 0,
        ReviewStatus      NVARCHAR(20) NOT NULL DEFAULT 'NONE',
        ReviewedBy        NVARCHAR(50) NULL,
        ReviewDate        DATETIME2 NULL,
        ReviewComment     NVARCHAR(300) NULL,
        CreatedDate       DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_FraudDetection_Transaction FOREIGN KEY (TransactionID) REFERENCES Transactions(TransactionID)
    );
    CREATE INDEX IX_FraudDetection_TransactionID ON FraudDetection(TransactionID);
    CREATE INDEX IX_FraudDetection_RiskLevel ON FraudDetection(RiskLevel);
    CREATE INDEX IX_FraudDetection_FlaggedForReview ON FraudDetection(FlaggedForReview);
END
GO
