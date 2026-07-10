-- Patch: Loan Management module (Products, Applications, Loans, Installments, Repayments).
-- IDEMPOTENT: safe to re-run.

IF OBJECT_ID('dbo.LoanProduct', 'U') IS NULL
BEGIN
    CREATE TABLE LoanProduct (
        LoanProductID     INT IDENTITY(1,1) PRIMARY KEY,
        Code              NVARCHAR(20) NOT NULL UNIQUE,
        Name              NVARCHAR(100) NOT NULL,
        InterestMethod    NVARCHAR(15) NOT NULL DEFAULT 'REDUCING',
        AnnualInterestRate DECIMAL(9,4) NOT NULL,
        MinAmount         DECIMAL(18,2) NOT NULL,
        MaxAmount         DECIMAL(18,2) NOT NULL,
        MinTermMonths     INT NOT NULL,
        MaxTermMonths     INT NOT NULL,
        PenaltyRatePerDay DECIMAL(9,4) NOT NULL DEFAULT 0,
        GracePeriodDays   INT NOT NULL DEFAULT 0,
        Statut            NVARCHAR(15) NOT NULL DEFAULT 'ACTIVE',
        CreatedBy         NVARCHAR(50) NULL,
        CreatedDate       DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
END
GO

IF OBJECT_ID('dbo.LoanApplication', 'U') IS NULL
BEGIN
    CREATE TABLE LoanApplication (
        LoanApplicationID INT IDENTITY(1,1) PRIMARY KEY,
        ClientID          NVARCHAR(20) NOT NULL,
        LoanProductID     INT NOT NULL,
        AgenceID          INT NOT NULL,
        CollectorID       NVARCHAR(20) NULL,
        RequestedAmount   DECIMAL(18,2) NOT NULL,
        RequestedTermMonths INT NOT NULL,
        Purpose           NVARCHAR(300) NULL,
        ApprovedAmount    DECIMAL(18,2) NULL,
        ApprovedTermMonths INT NULL,
        Status            NVARCHAR(15) NOT NULL DEFAULT 'PENDING',
        RequestedBy       NVARCHAR(50) NOT NULL,
        RequestDate       DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        ApprovedBy        NVARCHAR(50) NULL,
        ApprovalDate      DATETIME2 NULL,
        RejectionReason   NVARCHAR(300) NULL,
        CONSTRAINT FK_LoanApplication_Client FOREIGN KEY (ClientID) REFERENCES Client(ClientID),
        CONSTRAINT FK_LoanApplication_Product FOREIGN KEY (LoanProductID) REFERENCES LoanProduct(LoanProductID)
    );
    CREATE INDEX IX_LoanApplication_ClientID ON LoanApplication(ClientID);
    CREATE INDEX IX_LoanApplication_Status ON LoanApplication(Status);
END
GO

IF OBJECT_ID('dbo.Loan', 'U') IS NULL
BEGIN
    CREATE TABLE Loan (
        LoanID              INT IDENTITY(1,1) PRIMARY KEY,
        LoanApplicationID   INT NOT NULL,
        LoanNumber          NVARCHAR(30) NOT NULL UNIQUE,
        ClientID            NVARCHAR(20) NOT NULL,
        LoanProductID       INT NOT NULL,
        AgenceID            INT NOT NULL,
        CollectorID         NVARCHAR(20) NULL,
        PrincipalAmount     DECIMAL(18,2) NOT NULL,
        AnnualInterestRate  DECIMAL(9,4) NOT NULL,
        InterestMethod      NVARCHAR(15) NOT NULL,
        TermMonths          INT NOT NULL,
        DisbursementDate    DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        DisbursedToAccountID NVARCHAR(20) NULL,
        DisbursedBy         NVARCHAR(50) NOT NULL,
        TotalPrincipal      DECIMAL(18,2) NOT NULL,
        TotalInterest       DECIMAL(18,2) NOT NULL,
        OutstandingPrincipal DECIMAL(18,2) NOT NULL,
        OutstandingInterest  DECIMAL(18,2) NOT NULL,
        OutstandingPenalty   DECIMAL(18,2) NOT NULL DEFAULT 0,
        NextDueDate         DATETIME2 NULL,
        Status              NVARCHAR(15) NOT NULL DEFAULT 'ACTIVE',
        WriteOffReason      NVARCHAR(300) NULL,
        ClosedDate          DATETIME2 NULL,
        CONSTRAINT FK_Loan_Application FOREIGN KEY (LoanApplicationID) REFERENCES LoanApplication(LoanApplicationID),
        CONSTRAINT FK_Loan_Client FOREIGN KEY (ClientID) REFERENCES Client(ClientID),
        CONSTRAINT FK_Loan_Product FOREIGN KEY (LoanProductID) REFERENCES LoanProduct(LoanProductID)
    );
    CREATE INDEX IX_Loan_ClientID ON Loan(ClientID);
    CREATE INDEX IX_Loan_Status ON Loan(Status);
END
GO

IF OBJECT_ID('dbo.LoanInstallment', 'U') IS NULL
BEGIN
    CREATE TABLE LoanInstallment (
        LoanInstallmentID INT IDENTITY(1,1) PRIMARY KEY,
        LoanID            INT NOT NULL,
        InstallmentNumber INT NOT NULL,
        DueDate           DATETIME2 NOT NULL,
        PrincipalDue      DECIMAL(18,2) NOT NULL,
        InterestDue       DECIMAL(18,2) NOT NULL,
        PenaltyDue        DECIMAL(18,2) NOT NULL DEFAULT 0,
        PrincipalPaid     DECIMAL(18,2) NOT NULL DEFAULT 0,
        InterestPaid      DECIMAL(18,2) NOT NULL DEFAULT 0,
        PenaltyPaid       DECIMAL(18,2) NOT NULL DEFAULT 0,
        PaidDate          DATETIME2 NULL,
        Status            NVARCHAR(15) NOT NULL DEFAULT 'PENDING',
        CONSTRAINT FK_LoanInstallment_Loan FOREIGN KEY (LoanID) REFERENCES Loan(LoanID)
    );
    CREATE INDEX IX_LoanInstallment_LoanID ON LoanInstallment(LoanID);
    CREATE INDEX IX_LoanInstallment_DueDate ON LoanInstallment(DueDate);
END
GO

IF OBJECT_ID('dbo.LoanRepayment', 'U') IS NULL
BEGIN
    CREATE TABLE LoanRepayment (
        LoanRepaymentID INT IDENTITY(1,1) PRIMARY KEY,
        LoanID          INT NOT NULL,
        Amount          DECIMAL(18,2) NOT NULL,
        PrincipalPaid   DECIMAL(18,2) NOT NULL DEFAULT 0,
        InterestPaid    DECIMAL(18,2) NOT NULL DEFAULT 0,
        PenaltyPaid     DECIMAL(18,2) NOT NULL DEFAULT 0,
        ReceiptNumber   NVARCHAR(50) NOT NULL,
        PaymentDate     DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        ReceivedBy      NVARCHAR(50) NOT NULL,
        CONSTRAINT FK_LoanRepayment_Loan FOREIGN KEY (LoanID) REFERENCES Loan(LoanID)
    );
    CREATE INDEX IX_LoanRepayment_LoanID ON LoanRepayment(LoanID);
END
GO

-- Guarantor / collateral fields (added after initial Loan Management release).
IF COL_LENGTH('LoanApplication', 'GuarantorName') IS NULL
    ALTER TABLE LoanApplication ADD GuarantorName NVARCHAR(150) NULL;
IF COL_LENGTH('LoanApplication', 'GuarantorPhone') IS NULL
    ALTER TABLE LoanApplication ADD GuarantorPhone NVARCHAR(30) NULL;
IF COL_LENGTH('LoanApplication', 'GuarantorAddress') IS NULL
    ALTER TABLE LoanApplication ADD GuarantorAddress NVARCHAR(300) NULL;
IF COL_LENGTH('LoanApplication', 'GuarantorIDNumber') IS NULL
    ALTER TABLE LoanApplication ADD GuarantorIDNumber NVARCHAR(50) NULL;
IF COL_LENGTH('LoanApplication', 'GuarantorPhotoUrl') IS NULL
    ALTER TABLE LoanApplication ADD GuarantorPhotoUrl NVARCHAR(300) NULL;
IF COL_LENGTH('LoanApplication', 'GuarantorSignatureUrl') IS NULL
    ALTER TABLE LoanApplication ADD GuarantorSignatureUrl NVARCHAR(300) NULL;
IF COL_LENGTH('LoanApplication', 'CollateralDescription') IS NULL
    ALTER TABLE LoanApplication ADD CollateralDescription NVARCHAR(300) NULL;
GO
