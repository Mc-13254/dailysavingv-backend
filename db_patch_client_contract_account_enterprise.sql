-- Patch: Enterprise Client Onboarding + Client Contracts + Client Accounts.
-- Extends the three existing modules (Client/ClientTmp, Contract/ContractTmp,
-- Accounts/AccountsTMP) with the full banking-grade field set.
--
-- IDEMPOTENT: every ADD COLUMN / CREATE INDEX / ADD CONSTRAINT is guarded, so
-- this script is always safe to run again from scratch (e.g. after a partial
-- failure), no matter how far a previous run got.

-- ============================================================
-- 1. CLIENT  (+ ClientTmp mirrors every nullable column)
-- ============================================================
IF COL_LENGTH('Client', 'MiddleName') IS NULL ALTER TABLE Client ADD MiddleName NVARCHAR(100) NULL;
IF COL_LENGTH('Client', 'DateOfBirth') IS NULL ALTER TABLE Client ADD DateOfBirth DATETIME2 NULL;
IF COL_LENGTH('Client', 'PlaceOfBirth') IS NULL ALTER TABLE Client ADD PlaceOfBirth NVARCHAR(150) NULL;
IF COL_LENGTH('Client', 'Nationality') IS NULL ALTER TABLE Client ADD Nationality NVARCHAR(100) NULL;
IF COL_LENGTH('Client', 'MaritalStatus') IS NULL ALTER TABLE Client ADD MaritalStatus NVARCHAR(30) NULL;
IF COL_LENGTH('Client', 'Profession') IS NULL ALTER TABLE Client ADD Profession NVARCHAR(150) NULL;
IF COL_LENGTH('Client', 'Occupation') IS NULL ALTER TABLE Client ADD Occupation NVARCHAR(150) NULL;
IF COL_LENGTH('Client', 'Employer') IS NULL ALTER TABLE Client ADD Employer NVARCHAR(150) NULL;
IF COL_LENGTH('Client', 'EducationLevel') IS NULL ALTER TABLE Client ADD EducationLevel NVARCHAR(50) NULL;
IF COL_LENGTH('Client', 'MonthlyIncome') IS NULL ALTER TABLE Client ADD MonthlyIncome DECIMAL(18,2) NULL;

IF COL_LENGTH('Client', 'SecondaryPhone') IS NULL ALTER TABLE Client ADD SecondaryPhone NVARCHAR(30) NULL;
IF COL_LENGTH('Client', 'WhatsApp') IS NULL ALTER TABLE Client ADD WhatsApp NVARCHAR(30) NULL;
IF COL_LENGTH('Client', 'Country') IS NULL ALTER TABLE Client ADD Country NVARCHAR(100) NULL;
IF COL_LENGTH('Client', 'City') IS NULL ALTER TABLE Client ADD City NVARCHAR(100) NULL;
IF COL_LENGTH('Client', 'District') IS NULL ALTER TABLE Client ADD District NVARCHAR(100) NULL;
IF COL_LENGTH('Client', 'Neighborhood') IS NULL ALTER TABLE Client ADD Neighborhood NVARCHAR(100) NULL;
IF COL_LENGTH('Client', 'Street') IS NULL ALTER TABLE Client ADD Street NVARCHAR(150) NULL;
IF COL_LENGTH('Client', 'HouseNumber') IS NULL ALTER TABLE Client ADD HouseNumber NVARCHAR(30) NULL;
IF COL_LENGTH('Client', 'PostalCode') IS NULL ALTER TABLE Client ADD PostalCode NVARCHAR(20) NULL;
IF COL_LENGTH('Client', 'Latitude') IS NULL ALTER TABLE Client ADD Latitude DECIMAL(9,6) NULL;
IF COL_LENGTH('Client', 'Longitude') IS NULL ALTER TABLE Client ADD Longitude DECIMAL(9,6) NULL;

IF COL_LENGTH('Client', 'NationalIDIssueDate') IS NULL ALTER TABLE Client ADD NationalIDIssueDate DATETIME2 NULL;
IF COL_LENGTH('Client', 'NationalIDExpiryDate') IS NULL ALTER TABLE Client ADD NationalIDExpiryDate DATETIME2 NULL;
IF COL_LENGTH('Client', 'PassportNumber') IS NULL ALTER TABLE Client ADD PassportNumber NVARCHAR(50) NULL;
IF COL_LENGTH('Client', 'DriverLicenseNumber') IS NULL ALTER TABLE Client ADD DriverLicenseNumber NVARCHAR(50) NULL;
IF COL_LENGTH('Client', 'TaxIdentificationNumber') IS NULL ALTER TABLE Client ADD TaxIdentificationNumber NVARCHAR(50) NULL;
IF COL_LENGTH('Client', 'SocialSecurityNumber') IS NULL ALTER TABLE Client ADD SocialSecurityNumber NVARCHAR(50) NULL;
IF COL_LENGTH('Client', 'DocumentType') IS NULL ALTER TABLE Client ADD DocumentType NVARCHAR(50) NULL;
IF COL_LENGTH('Client', 'IssuedBy') IS NULL ALTER TABLE Client ADD IssuedBy NVARCHAR(100) NULL;

IF COL_LENGTH('Client', 'NationalIDFrontUrl') IS NULL ALTER TABLE Client ADD NationalIDFrontUrl NVARCHAR(300) NULL;
IF COL_LENGTH('Client', 'NationalIDBackUrl') IS NULL ALTER TABLE Client ADD NationalIDBackUrl NVARCHAR(300) NULL;
IF COL_LENGTH('Client', 'PassportUrl') IS NULL ALTER TABLE Client ADD PassportUrl NVARCHAR(300) NULL;
IF COL_LENGTH('Client', 'ProofOfAddressUrl') IS NULL ALTER TABLE Client ADD ProofOfAddressUrl NVARCHAR(300) NULL;
IF COL_LENGTH('Client', 'SignatureUrl') IS NULL ALTER TABLE Client ADD SignatureUrl NVARCHAR(300) NULL;

IF COL_LENGTH('Client', 'BusinessName') IS NULL ALTER TABLE Client ADD BusinessName NVARCHAR(150) NULL;
IF COL_LENGTH('Client', 'BusinessAddress') IS NULL ALTER TABLE Client ADD BusinessAddress NVARCHAR(300) NULL;
IF COL_LENGTH('Client', 'BusinessType') IS NULL ALTER TABLE Client ADD BusinessType NVARCHAR(100) NULL;
IF COL_LENGTH('Client', 'YearsInBusiness') IS NULL ALTER TABLE Client ADD YearsInBusiness INT NULL;
IF COL_LENGTH('Client', 'MonthlyRevenue') IS NULL ALTER TABLE Client ADD MonthlyRevenue DECIMAL(18,2) NULL;
IF COL_LENGTH('Client', 'MonthlyExpenses') IS NULL ALTER TABLE Client ADD MonthlyExpenses DECIMAL(18,2) NULL;

IF COL_LENGTH('Client', 'ClientCategory') IS NULL ALTER TABLE Client ADD ClientCategory NVARCHAR(30) NOT NULL DEFAULT 'INDIVIDUAL';
IF COL_LENGTH('Client', 'AccountOfficer') IS NULL ALTER TABLE Client ADD AccountOfficer NVARCHAR(50) NULL;

IF COL_LENGTH('Client', 'EmergencyContactName') IS NULL ALTER TABLE Client ADD EmergencyContactName NVARCHAR(150) NULL;
IF COL_LENGTH('Client', 'EmergencyContactRelationship') IS NULL ALTER TABLE Client ADD EmergencyContactRelationship NVARCHAR(50) NULL;
IF COL_LENGTH('Client', 'EmergencyContactPhone') IS NULL ALTER TABLE Client ADD EmergencyContactPhone NVARCHAR(30) NULL;
IF COL_LENGTH('Client', 'EmergencyContactAddress') IS NULL ALTER TABLE Client ADD EmergencyContactAddress NVARCHAR(300) NULL;

IF COL_LENGTH('Client', 'GuarantorName') IS NULL ALTER TABLE Client ADD GuarantorName NVARCHAR(150) NULL;
IF COL_LENGTH('Client', 'GuarantorRelationship') IS NULL ALTER TABLE Client ADD GuarantorRelationship NVARCHAR(50) NULL;
IF COL_LENGTH('Client', 'GuarantorPhone') IS NULL ALTER TABLE Client ADD GuarantorPhone NVARCHAR(30) NULL;
IF COL_LENGTH('Client', 'GuarantorOccupation') IS NULL ALTER TABLE Client ADD GuarantorOccupation NVARCHAR(150) NULL;
IF COL_LENGTH('Client', 'GuarantorEmployer') IS NULL ALTER TABLE Client ADD GuarantorEmployer NVARCHAR(150) NULL;
IF COL_LENGTH('Client', 'GuarantorAddress') IS NULL ALTER TABLE Client ADD GuarantorAddress NVARCHAR(300) NULL;

IF COL_LENGTH('Client', 'RiskLevel') IS NULL ALTER TABLE Client ADD RiskLevel NVARCHAR(10) NOT NULL DEFAULT 'LOW';
IF COL_LENGTH('Client', 'IsPoliticallyExposed') IS NULL ALTER TABLE Client ADD IsPoliticallyExposed BIT NOT NULL DEFAULT 0;
IF COL_LENGTH('Client', 'IsBlacklisted') IS NULL ALTER TABLE Client ADD IsBlacklisted BIT NOT NULL DEFAULT 0;
IF COL_LENGTH('Client', 'AMLStatus') IS NULL ALTER TABLE Client ADD AMLStatus NVARCHAR(20) NOT NULL DEFAULT 'PENDING';

IF COL_LENGTH('Client', 'RejectionReason') IS NULL ALTER TABLE Client ADD RejectionReason NVARCHAR(500) NULL;
IF COL_LENGTH('Client', 'ValidatedBy') IS NULL ALTER TABLE Client ADD ValidatedBy NVARCHAR(50) NULL;
IF COL_LENGTH('Client', 'ValidationDate') IS NULL ALTER TABLE Client ADD ValidationDate DATETIME2 NULL;
IF COL_LENGTH('Client', 'UpdatedBy') IS NULL ALTER TABLE Client ADD UpdatedBy NVARCHAR(50) NULL;
IF COL_LENGTH('Client', 'UpdatedDate') IS NULL ALTER TABLE Client ADD UpdatedDate DATETIME2 NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_Client_NumeroCNI' AND object_id = OBJECT_ID('Client'))
    CREATE UNIQUE INDEX UQ_Client_NumeroCNI ON Client(NumeroCNI) WHERE NumeroCNI IS NOT NULL;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_Client_PhoneNumber' AND object_id = OBJECT_ID('Client'))
    CREATE UNIQUE INDEX UQ_Client_PhoneNumber ON Client(PhoneNumber) WHERE PhoneNumber IS NOT NULL;
GO

-- ClientTmp mirrors the same columns (all nullable — draft data).
IF COL_LENGTH('ClientTmp', 'MiddleName') IS NULL ALTER TABLE ClientTmp ADD MiddleName NVARCHAR(100) NULL;
IF COL_LENGTH('ClientTmp', 'DateOfBirth') IS NULL ALTER TABLE ClientTmp ADD DateOfBirth DATETIME2 NULL;
IF COL_LENGTH('ClientTmp', 'PlaceOfBirth') IS NULL ALTER TABLE ClientTmp ADD PlaceOfBirth NVARCHAR(150) NULL;
IF COL_LENGTH('ClientTmp', 'Nationality') IS NULL ALTER TABLE ClientTmp ADD Nationality NVARCHAR(100) NULL;
IF COL_LENGTH('ClientTmp', 'MaritalStatus') IS NULL ALTER TABLE ClientTmp ADD MaritalStatus NVARCHAR(30) NULL;
IF COL_LENGTH('ClientTmp', 'Profession') IS NULL ALTER TABLE ClientTmp ADD Profession NVARCHAR(150) NULL;
IF COL_LENGTH('ClientTmp', 'Occupation') IS NULL ALTER TABLE ClientTmp ADD Occupation NVARCHAR(150) NULL;
IF COL_LENGTH('ClientTmp', 'Employer') IS NULL ALTER TABLE ClientTmp ADD Employer NVARCHAR(150) NULL;
IF COL_LENGTH('ClientTmp', 'EducationLevel') IS NULL ALTER TABLE ClientTmp ADD EducationLevel NVARCHAR(50) NULL;
IF COL_LENGTH('ClientTmp', 'MonthlyIncome') IS NULL ALTER TABLE ClientTmp ADD MonthlyIncome DECIMAL(18,2) NULL;
IF COL_LENGTH('ClientTmp', 'SecondaryPhone') IS NULL ALTER TABLE ClientTmp ADD SecondaryPhone NVARCHAR(30) NULL;
IF COL_LENGTH('ClientTmp', 'WhatsApp') IS NULL ALTER TABLE ClientTmp ADD WhatsApp NVARCHAR(30) NULL;
IF COL_LENGTH('ClientTmp', 'Country') IS NULL ALTER TABLE ClientTmp ADD Country NVARCHAR(100) NULL;
IF COL_LENGTH('ClientTmp', 'City') IS NULL ALTER TABLE ClientTmp ADD City NVARCHAR(100) NULL;
IF COL_LENGTH('ClientTmp', 'District') IS NULL ALTER TABLE ClientTmp ADD District NVARCHAR(100) NULL;
IF COL_LENGTH('ClientTmp', 'Neighborhood') IS NULL ALTER TABLE ClientTmp ADD Neighborhood NVARCHAR(100) NULL;
IF COL_LENGTH('ClientTmp', 'Street') IS NULL ALTER TABLE ClientTmp ADD Street NVARCHAR(150) NULL;
IF COL_LENGTH('ClientTmp', 'HouseNumber') IS NULL ALTER TABLE ClientTmp ADD HouseNumber NVARCHAR(30) NULL;
IF COL_LENGTH('ClientTmp', 'PostalCode') IS NULL ALTER TABLE ClientTmp ADD PostalCode NVARCHAR(20) NULL;
IF COL_LENGTH('ClientTmp', 'Latitude') IS NULL ALTER TABLE ClientTmp ADD Latitude DECIMAL(9,6) NULL;
IF COL_LENGTH('ClientTmp', 'Longitude') IS NULL ALTER TABLE ClientTmp ADD Longitude DECIMAL(9,6) NULL;
IF COL_LENGTH('ClientTmp', 'NationalIDIssueDate') IS NULL ALTER TABLE ClientTmp ADD NationalIDIssueDate DATETIME2 NULL;
IF COL_LENGTH('ClientTmp', 'NationalIDExpiryDate') IS NULL ALTER TABLE ClientTmp ADD NationalIDExpiryDate DATETIME2 NULL;
IF COL_LENGTH('ClientTmp', 'PassportNumber') IS NULL ALTER TABLE ClientTmp ADD PassportNumber NVARCHAR(50) NULL;
IF COL_LENGTH('ClientTmp', 'DriverLicenseNumber') IS NULL ALTER TABLE ClientTmp ADD DriverLicenseNumber NVARCHAR(50) NULL;
IF COL_LENGTH('ClientTmp', 'TaxIdentificationNumber') IS NULL ALTER TABLE ClientTmp ADD TaxIdentificationNumber NVARCHAR(50) NULL;
IF COL_LENGTH('ClientTmp', 'SocialSecurityNumber') IS NULL ALTER TABLE ClientTmp ADD SocialSecurityNumber NVARCHAR(50) NULL;
IF COL_LENGTH('ClientTmp', 'DocumentType') IS NULL ALTER TABLE ClientTmp ADD DocumentType NVARCHAR(50) NULL;
IF COL_LENGTH('ClientTmp', 'IssuedBy') IS NULL ALTER TABLE ClientTmp ADD IssuedBy NVARCHAR(100) NULL;
IF COL_LENGTH('ClientTmp', 'NationalIDFrontUrl') IS NULL ALTER TABLE ClientTmp ADD NationalIDFrontUrl NVARCHAR(300) NULL;
IF COL_LENGTH('ClientTmp', 'NationalIDBackUrl') IS NULL ALTER TABLE ClientTmp ADD NationalIDBackUrl NVARCHAR(300) NULL;
IF COL_LENGTH('ClientTmp', 'PassportUrl') IS NULL ALTER TABLE ClientTmp ADD PassportUrl NVARCHAR(300) NULL;
IF COL_LENGTH('ClientTmp', 'ProofOfAddressUrl') IS NULL ALTER TABLE ClientTmp ADD ProofOfAddressUrl NVARCHAR(300) NULL;
IF COL_LENGTH('ClientTmp', 'SignatureUrl') IS NULL ALTER TABLE ClientTmp ADD SignatureUrl NVARCHAR(300) NULL;
IF COL_LENGTH('ClientTmp', 'BusinessName') IS NULL ALTER TABLE ClientTmp ADD BusinessName NVARCHAR(150) NULL;
IF COL_LENGTH('ClientTmp', 'BusinessAddress') IS NULL ALTER TABLE ClientTmp ADD BusinessAddress NVARCHAR(300) NULL;
IF COL_LENGTH('ClientTmp', 'BusinessType') IS NULL ALTER TABLE ClientTmp ADD BusinessType NVARCHAR(100) NULL;
IF COL_LENGTH('ClientTmp', 'YearsInBusiness') IS NULL ALTER TABLE ClientTmp ADD YearsInBusiness INT NULL;
IF COL_LENGTH('ClientTmp', 'MonthlyRevenue') IS NULL ALTER TABLE ClientTmp ADD MonthlyRevenue DECIMAL(18,2) NULL;
IF COL_LENGTH('ClientTmp', 'MonthlyExpenses') IS NULL ALTER TABLE ClientTmp ADD MonthlyExpenses DECIMAL(18,2) NULL;
IF COL_LENGTH('ClientTmp', 'ClientCategory') IS NULL ALTER TABLE ClientTmp ADD ClientCategory NVARCHAR(30) NULL;
IF COL_LENGTH('ClientTmp', 'AccountOfficer') IS NULL ALTER TABLE ClientTmp ADD AccountOfficer NVARCHAR(50) NULL;
IF COL_LENGTH('ClientTmp', 'EmergencyContactName') IS NULL ALTER TABLE ClientTmp ADD EmergencyContactName NVARCHAR(150) NULL;
IF COL_LENGTH('ClientTmp', 'EmergencyContactRelationship') IS NULL ALTER TABLE ClientTmp ADD EmergencyContactRelationship NVARCHAR(50) NULL;
IF COL_LENGTH('ClientTmp', 'EmergencyContactPhone') IS NULL ALTER TABLE ClientTmp ADD EmergencyContactPhone NVARCHAR(30) NULL;
IF COL_LENGTH('ClientTmp', 'EmergencyContactAddress') IS NULL ALTER TABLE ClientTmp ADD EmergencyContactAddress NVARCHAR(300) NULL;
IF COL_LENGTH('ClientTmp', 'GuarantorName') IS NULL ALTER TABLE ClientTmp ADD GuarantorName NVARCHAR(150) NULL;
IF COL_LENGTH('ClientTmp', 'GuarantorRelationship') IS NULL ALTER TABLE ClientTmp ADD GuarantorRelationship NVARCHAR(50) NULL;
IF COL_LENGTH('ClientTmp', 'GuarantorPhone') IS NULL ALTER TABLE ClientTmp ADD GuarantorPhone NVARCHAR(30) NULL;
IF COL_LENGTH('ClientTmp', 'GuarantorOccupation') IS NULL ALTER TABLE ClientTmp ADD GuarantorOccupation NVARCHAR(150) NULL;
IF COL_LENGTH('ClientTmp', 'GuarantorEmployer') IS NULL ALTER TABLE ClientTmp ADD GuarantorEmployer NVARCHAR(150) NULL;
IF COL_LENGTH('ClientTmp', 'GuarantorAddress') IS NULL ALTER TABLE ClientTmp ADD GuarantorAddress NVARCHAR(300) NULL;
IF COL_LENGTH('ClientTmp', 'RiskLevel') IS NULL ALTER TABLE ClientTmp ADD RiskLevel NVARCHAR(10) NULL;
IF COL_LENGTH('ClientTmp', 'IsPoliticallyExposed') IS NULL ALTER TABLE ClientTmp ADD IsPoliticallyExposed BIT NULL;
IF COL_LENGTH('ClientTmp', 'IsBlacklisted') IS NULL ALTER TABLE ClientTmp ADD IsBlacklisted BIT NULL;
IF COL_LENGTH('ClientTmp', 'AMLStatus') IS NULL ALTER TABLE ClientTmp ADD AMLStatus NVARCHAR(20) NULL;
GO

-- ============================================================
-- 2. CONTRACT (+ ContractTmp)
-- NOTE: ContractTypeID already existed pre-patch (original schema) — skipped.
-- ============================================================
IF COL_LENGTH('Contract', 'CollectorID') IS NULL ALTER TABLE Contract ADD CollectorID NVARCHAR(20) NULL;
IF COL_LENGTH('Contract', 'CommissionTypeID') IS NULL ALTER TABLE Contract ADD CommissionTypeID INT NULL;
IF COL_LENGTH('Contract', 'CommissionRangeID') IS NULL ALTER TABLE Contract ADD CommissionRangeID INT NULL;
IF COL_LENGTH('Contract', 'CollectionFrequency') IS NULL ALTER TABLE Contract ADD CollectionFrequency NVARCHAR(10) NOT NULL DEFAULT 'DAILY';
IF COL_LENGTH('Contract', 'CollectionDay') IS NULL ALTER TABLE Contract ADD CollectionDay NVARCHAR(20) NULL;
IF COL_LENGTH('Contract', 'OpeningDeposit') IS NULL ALTER TABLE Contract ADD OpeningDeposit DECIMAL(18,2) NULL;
IF COL_LENGTH('Contract', 'MinimumBalance') IS NULL ALTER TABLE Contract ADD MinimumBalance DECIMAL(18,2) NULL;
IF COL_LENGTH('Contract', 'MaximumBalance') IS NULL ALTER TABLE Contract ADD MaximumBalance DECIMAL(18,2) NULL;
IF COL_LENGTH('Contract', 'PenaltyRules') IS NULL ALTER TABLE Contract ADD PenaltyRules NVARCHAR(300) NULL;
IF COL_LENGTH('Contract', 'GracePeriod') IS NULL ALTER TABLE Contract ADD GracePeriod INT NULL;
IF COL_LENGTH('Contract', 'TerminationReason') IS NULL ALTER TABLE Contract ADD TerminationReason NVARCHAR(30) NULL;
IF COL_LENGTH('Contract', 'TerminationDate') IS NULL ALTER TABLE Contract ADD TerminationDate DATETIME2 NULL;
IF COL_LENGTH('Contract', 'PdfPath') IS NULL ALTER TABLE Contract ADD PdfPath NVARCHAR(300) NULL;
IF COL_LENGTH('Contract', 'CustomerSigned') IS NULL ALTER TABLE Contract ADD CustomerSigned BIT NOT NULL DEFAULT 0;
IF COL_LENGTH('Contract', 'OfficerSigned') IS NULL ALTER TABLE Contract ADD OfficerSigned BIT NOT NULL DEFAULT 0;
IF COL_LENGTH('Contract', 'UpdatedBy') IS NULL ALTER TABLE Contract ADD UpdatedBy NVARCHAR(50) NULL;
IF COL_LENGTH('Contract', 'UpdatedDate') IS NULL ALTER TABLE Contract ADD UpdatedDate DATETIME2 NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Contract_Collector')
    ALTER TABLE Contract ADD CONSTRAINT FK_Contract_Collector FOREIGN KEY (CollectorID) REFERENCES Collector(CollectorID);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Contract_CommissionType')
    ALTER TABLE Contract ADD CONSTRAINT FK_Contract_CommissionType FOREIGN KEY (CommissionTypeID) REFERENCES CommissionType(CommissionTypeID);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Contract_CommissionRange')
    ALTER TABLE Contract ADD CONSTRAINT FK_Contract_CommissionRange FOREIGN KEY (CommissionRangeID) REFERENCES CommissionRange(CommissionRangeID);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Contract_ContractTypeRef' AND parent_object_id = OBJECT_ID('Contract'))
    ALTER TABLE Contract ADD CONSTRAINT FK_Contract_ContractTypeRef FOREIGN KEY (ContractTypeID) REFERENCES ContractType(ContractTypeID);
GO

-- Business rule: only one ACTIVE contract per client.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_Contract_Client_Active' AND object_id = OBJECT_ID('Contract'))
    CREATE UNIQUE INDEX UQ_Contract_Client_Active ON Contract(ClientID) WHERE Statut = 'ACTIVE';
GO

IF COL_LENGTH('ContractTmp', 'CollectorID') IS NULL ALTER TABLE ContractTmp ADD CollectorID NVARCHAR(20) NULL;
IF COL_LENGTH('ContractTmp', 'CommissionTypeID') IS NULL ALTER TABLE ContractTmp ADD CommissionTypeID INT NULL;
IF COL_LENGTH('ContractTmp', 'CommissionRangeID') IS NULL ALTER TABLE ContractTmp ADD CommissionRangeID INT NULL;
IF COL_LENGTH('ContractTmp', 'CollectionFrequency') IS NULL ALTER TABLE ContractTmp ADD CollectionFrequency NVARCHAR(10) NULL;
IF COL_LENGTH('ContractTmp', 'CollectionDay') IS NULL ALTER TABLE ContractTmp ADD CollectionDay NVARCHAR(20) NULL;
IF COL_LENGTH('ContractTmp', 'OpeningDeposit') IS NULL ALTER TABLE ContractTmp ADD OpeningDeposit DECIMAL(18,2) NULL;
IF COL_LENGTH('ContractTmp', 'MinimumBalance') IS NULL ALTER TABLE ContractTmp ADD MinimumBalance DECIMAL(18,2) NULL;
IF COL_LENGTH('ContractTmp', 'MaximumBalance') IS NULL ALTER TABLE ContractTmp ADD MaximumBalance DECIMAL(18,2) NULL;
IF COL_LENGTH('ContractTmp', 'PenaltyRules') IS NULL ALTER TABLE ContractTmp ADD PenaltyRules NVARCHAR(300) NULL;
IF COL_LENGTH('ContractTmp', 'GracePeriod') IS NULL ALTER TABLE ContractTmp ADD GracePeriod INT NULL;
IF COL_LENGTH('ContractTmp', 'ContractTypeID') IS NULL ALTER TABLE ContractTmp ADD ContractTypeID INT NULL;
GO

-- ============================================================
-- 3. ACCOUNTS (+ AccountsTMP)
-- ============================================================
IF COL_LENGTH('Accounts', 'ContractID') IS NULL ALTER TABLE Accounts ADD ContractID INT NULL;
IF COL_LENGTH('Accounts', 'CollectorID') IS NULL ALTER TABLE Accounts ADD CollectorID NVARCHAR(20) NULL;
IF COL_LENGTH('Accounts', 'AccountType') IS NULL ALTER TABLE Accounts ADD AccountType NVARCHAR(30) NOT NULL DEFAULT 'DAILY_SAVING';
IF COL_LENGTH('Accounts', 'Currency') IS NULL ALTER TABLE Accounts ADD Currency NVARCHAR(10) NOT NULL DEFAULT 'XAF';
IF COL_LENGTH('Accounts', 'OpeningBalance') IS NULL ALTER TABLE Accounts ADD OpeningBalance DECIMAL(18,2) NOT NULL DEFAULT 0;
IF COL_LENGTH('Accounts', 'AvailableBalance') IS NULL ALTER TABLE Accounts ADD AvailableBalance DECIMAL(18,2) NOT NULL DEFAULT 0;
IF COL_LENGTH('Accounts', 'BlockedBalance') IS NULL ALTER TABLE Accounts ADD BlockedBalance DECIMAL(18,2) NOT NULL DEFAULT 0;
IF COL_LENGTH('Accounts', 'MinimumBalance') IS NULL ALTER TABLE Accounts ADD MinimumBalance DECIMAL(18,2) NULL;
IF COL_LENGTH('Accounts', 'MaximumBalance') IS NULL ALTER TABLE Accounts ADD MaximumBalance DECIMAL(18,2) NULL;
IF COL_LENGTH('Accounts', 'DailyDepositLimit') IS NULL ALTER TABLE Accounts ADD DailyDepositLimit DECIMAL(18,2) NULL;
IF COL_LENGTH('Accounts', 'DailyWithdrawalLimit') IS NULL ALTER TABLE Accounts ADD DailyWithdrawalLimit DECIMAL(18,2) NULL;
IF COL_LENGTH('Accounts', 'DailyTransactionLimit') IS NULL ALTER TABLE Accounts ADD DailyTransactionLimit DECIMAL(18,2) NULL;
IF COL_LENGTH('Accounts', 'OverdraftAllowed') IS NULL ALTER TABLE Accounts ADD OverdraftAllowed BIT NOT NULL DEFAULT 0;
IF COL_LENGTH('Accounts', 'OverdraftLimit') IS NULL ALTER TABLE Accounts ADD OverdraftLimit DECIMAL(18,2) NULL;
IF COL_LENGTH('Accounts', 'Status') IS NULL ALTER TABLE Accounts ADD Status NVARCHAR(15) NOT NULL DEFAULT 'ACTIVE';
IF COL_LENGTH('Accounts', 'FreezeReason') IS NULL ALTER TABLE Accounts ADD FreezeReason NVARCHAR(300) NULL;
IF COL_LENGTH('Accounts', 'CloseReason') IS NULL ALTER TABLE Accounts ADD CloseReason NVARCHAR(300) NULL;
IF COL_LENGTH('Accounts', 'ClosingDate') IS NULL ALTER TABLE Accounts ADD ClosingDate DATETIME2 NULL;
IF COL_LENGTH('Accounts', 'UpdatedBy') IS NULL ALTER TABLE Accounts ADD UpdatedBy NVARCHAR(50) NULL;
IF COL_LENGTH('Accounts', 'UpdatedDate') IS NULL ALTER TABLE Accounts ADD UpdatedDate DATETIME2 NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Accounts_Contract')
    ALTER TABLE Accounts ADD CONSTRAINT FK_Accounts_Contract FOREIGN KEY (ContractID) REFERENCES Contract(ContractID);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Accounts_Collector')
    ALTER TABLE Accounts ADD CONSTRAINT FK_Accounts_Collector FOREIGN KEY (CollectorID) REFERENCES Collector(CollectorID);
GO

-- Business rule: only one account per contract.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_Accounts_ContractID' AND object_id = OBJECT_ID('Accounts'))
    CREATE UNIQUE INDEX UQ_Accounts_ContractID ON Accounts(ContractID) WHERE ContractID IS NOT NULL;
GO

IF COL_LENGTH('AccountsTMP', 'ContractID') IS NULL ALTER TABLE AccountsTMP ADD ContractID INT NULL;
IF COL_LENGTH('AccountsTMP', 'CollectorID') IS NULL ALTER TABLE AccountsTMP ADD CollectorID NVARCHAR(20) NULL;
IF COL_LENGTH('AccountsTMP', 'AccountType') IS NULL ALTER TABLE AccountsTMP ADD AccountType NVARCHAR(30) NULL;
IF COL_LENGTH('AccountsTMP', 'Currency') IS NULL ALTER TABLE AccountsTMP ADD Currency NVARCHAR(10) NULL;
IF COL_LENGTH('AccountsTMP', 'OpeningBalance') IS NULL ALTER TABLE AccountsTMP ADD OpeningBalance DECIMAL(18,2) NULL;
IF COL_LENGTH('AccountsTMP', 'AvailableBalance') IS NULL ALTER TABLE AccountsTMP ADD AvailableBalance DECIMAL(18,2) NULL;
IF COL_LENGTH('AccountsTMP', 'BlockedBalance') IS NULL ALTER TABLE AccountsTMP ADD BlockedBalance DECIMAL(18,2) NULL;
IF COL_LENGTH('AccountsTMP', 'MinimumBalance') IS NULL ALTER TABLE AccountsTMP ADD MinimumBalance DECIMAL(18,2) NULL;
IF COL_LENGTH('AccountsTMP', 'MaximumBalance') IS NULL ALTER TABLE AccountsTMP ADD MaximumBalance DECIMAL(18,2) NULL;
IF COL_LENGTH('AccountsTMP', 'DailyDepositLimit') IS NULL ALTER TABLE AccountsTMP ADD DailyDepositLimit DECIMAL(18,2) NULL;
IF COL_LENGTH('AccountsTMP', 'DailyWithdrawalLimit') IS NULL ALTER TABLE AccountsTMP ADD DailyWithdrawalLimit DECIMAL(18,2) NULL;
IF COL_LENGTH('AccountsTMP', 'DailyTransactionLimit') IS NULL ALTER TABLE AccountsTMP ADD DailyTransactionLimit DECIMAL(18,2) NULL;
IF COL_LENGTH('AccountsTMP', 'OverdraftAllowed') IS NULL ALTER TABLE AccountsTMP ADD OverdraftAllowed BIT NULL;
IF COL_LENGTH('AccountsTMP', 'OverdraftLimit') IS NULL ALTER TABLE AccountsTMP ADD OverdraftLimit DECIMAL(18,2) NULL;
IF COL_LENGTH('AccountsTMP', 'Status') IS NULL ALTER TABLE AccountsTMP ADD Status NVARCHAR(15) NULL;
GO
