namespace DailySavingV.API.DTOs;

public record ClientDto(
    string ClientID, string Nom, string? Prenom, string? PhoneNumber, string? Email,
    string ClientType, int AgenceID, string ValidationStatus, int CompletenessPercent
);

// Full onboarding payload — used by both Create (wizard submit) and Update.
// Every field beyond the original core set is optional so partial saves
// (e.g. saving a draft mid-wizard is NOT supported server-side yet — the
// wizard keeps step state client-side and submits once at the end) still work.
public record CreateClientRequest(
    // Step 1 — Personal
    string Nom, string? Prenom, string? MiddleName, string? Sexe, DateTime? DateOfBirth,
    string? PlaceOfBirth, string? Nationality, string? MaritalStatus, string? Profession,
    string? Occupation, string? Employer, string? EducationLevel, decimal? MonthlyIncome,
    // Step 2 — Contact
    string? PhoneNumber, string? SecondaryPhone, string? WhatsApp, string? Email,
    string? Country, string? City, string? District, string? Neighborhood, string? Street,
    string? HouseNumber, string? PostalCode, decimal? Latitude, decimal? Longitude, string? Address,
    // Step 3 — KYC
    int? TypeCNIID, string? NumeroCNI, DateTime? NationalIDIssueDate, DateTime? NationalIDExpiryDate,
    string? PassportNumber, string? DriverLicenseNumber, string? TaxIdentificationNumber,
    string? SocialSecurityNumber, string? DocumentType, string? IssuedBy,
    // Step 4 — Documents (URLs already uploaded via /api/client/upload)
    string? Image, string? NationalIDFrontUrl, string? NationalIDBackUrl, string? PassportUrl,
    string? ProofOfAddressUrl, string? SignatureUrl,
    // Step 5 — Business
    string? CompanyName, string? BusinessName, string? BusinessAddress, string? BusinessType,
    int? YearsInBusiness, decimal? MonthlyRevenue, decimal? MonthlyExpenses,
    // Step 6 — Banking
    string ClientType, string? ClientCategory, string? CollectorID, string? AccountOfficer,
    // Step 7 — Emergency contact
    string? EmergencyContactName, string? EmergencyContactRelationship, string? EmergencyContactPhone,
    string? EmergencyContactAddress,
    // Step 8 — Guarantor
    string? GuarantorName, string? GuarantorRelationship, string? GuarantorPhone,
    string? GuarantorOccupation, string? GuarantorEmployer, string? GuarantorAddress,
    // Step 9 — Risk & compliance
    string? RiskLevel, bool? IsPoliticallyExposed, bool? IsBlacklisted, string? AMLStatus
);

public record ClientDetailDto(
    string ClientID, string Nom, string? Prenom, string? MiddleName, string? Sexe, DateTime? DateOfBirth,
    string? PlaceOfBirth, string? Nationality, string? MaritalStatus, string? Profession,
    string? Occupation, string? Employer, string? EducationLevel, decimal? MonthlyIncome,
    string? PhoneNumber, string? SecondaryPhone, string? WhatsApp, string? Email,
    string? Country, string? City, string? District, string? Neighborhood, string? Street,
    string? HouseNumber, string? PostalCode, decimal? Latitude, decimal? Longitude, string? Address,
    string? NumeroCNI, DateTime? NationalIDIssueDate, DateTime? NationalIDExpiryDate,
    string? PassportNumber, string? DriverLicenseNumber, string? TaxIdentificationNumber,
    string? SocialSecurityNumber, string? DocumentType, string? IssuedBy,
    string? Image, string? NationalIDFrontUrl, string? NationalIDBackUrl, string? PassportUrl,
    string? ProofOfAddressUrl, string? SignatureUrl,
    string? CompanyName, string? BusinessName, string? BusinessAddress, string? BusinessType,
    int? YearsInBusiness, decimal? MonthlyRevenue, decimal? MonthlyExpenses,
    string ClientType, string ClientCategory, string? CollectorID, string? CollectorName, string? AccountOfficer,
    string? EmergencyContactName, string? EmergencyContactRelationship, string? EmergencyContactPhone,
    string? EmergencyContactAddress,
    string? GuarantorName, string? GuarantorRelationship, string? GuarantorPhone,
    string? GuarantorOccupation, string? GuarantorEmployer, string? GuarantorAddress,
    string RiskLevel, bool IsPoliticallyExposed, bool IsBlacklisted, string AMLStatus,
    string ValidationStatus, string? RejectionReason, int AgenceID,
    string? CreatedBy, DateTime CreatedDate, string? ValidatedBy, DateTime? ValidationDate,
    string? UpdatedBy, DateTime? UpdatedDate, int CompletenessPercent, string CompletenessLabel
);

public record FileUploadResultDto(string Url);
