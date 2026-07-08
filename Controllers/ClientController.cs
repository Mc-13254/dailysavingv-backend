using System.Text.Json;
using DailySavingV.API.Data;
using DailySavingV.API.DTOs;
using DailySavingV.API.Entities;
using DailySavingV.API.Entities.Pending;
using DailySavingV.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PendingActionType = DailySavingV.API.Entities.Pending.ActionType;
using PendingStatusEnum = DailySavingV.API.Entities.Pending.PendingStatus;

namespace DailySavingV.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ClientController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IWebHostEnvironment _env;

    public ClientController(AppDbContext db, ICurrentUserService currentUser, IWebHostEnvironment env)
    {
        _db = db;
        _currentUser = currentUser;
        _env = env;
    }

    // Auto agency-scoped via the Client global query filter in AppDbContext
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ClientDto>>> GetAll([FromQuery] string? search)
    {
        var query = _db.Clients.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c => c.Nom.Contains(search) || c.ClientID.Contains(search)
                || (c.PhoneNumber != null && c.PhoneNumber.Contains(search))
                || (c.NumeroCNI != null && c.NumeroCNI.Contains(search)));

        var clients = await query.ToListAsync();
        var result = clients.Select(c => new ClientDto(
            c.ClientID, c.Nom, c.Prenom, c.PhoneNumber, c.Email, c.ClientType, c.AgenceID,
            c.ValidationStatus, ComputeCompleteness(c).percent
        ));

        return Ok(result);
    }

    // GET api/client/{id} -> full Customer File (read-only "View")
    [HttpGet("{id}")]
    public async Task<ActionResult<ClientDetailDto>> GetById(string id)
    {
        var c = await _db.Clients.FirstOrDefaultAsync(x => x.ClientID == id);
        if (c == null) return NotFound();

        var collectorName = c.CollectorID == null ? null
            : await _db.Collectors.IgnoreQueryFilters().Where(col => col.CollectorID == c.CollectorID)
                .Select(col => $"{col.Name} {col.Surname}".Trim()).FirstOrDefaultAsync();

        var (percent, label) = ComputeCompleteness(c);

        return Ok(new ClientDetailDto(
            c.ClientID, c.Nom, c.Prenom, c.MiddleName, c.Sexe, c.DateOfBirth,
            c.PlaceOfBirth, c.Nationality, c.MaritalStatus, c.Profession, c.Occupation, c.Employer,
            c.EducationLevel, c.MonthlyIncome,
            c.PhoneNumber, c.SecondaryPhone, c.WhatsApp, c.Email,
            c.Country, c.City, c.District, c.Neighborhood, c.Street, c.HouseNumber, c.PostalCode,
            c.Latitude, c.Longitude, c.Address,
            c.NumeroCNI, c.NationalIDIssueDate, c.NationalIDExpiryDate, c.PassportNumber,
            c.DriverLicenseNumber, c.TaxIdentificationNumber, c.SocialSecurityNumber, c.DocumentType, c.IssuedBy,
            c.Image, c.NationalIDFrontUrl, c.NationalIDBackUrl, c.PassportUrl, c.ProofOfAddressUrl, c.SignatureUrl,
            c.CompanyName, c.BusinessName, c.BusinessAddress, c.BusinessType, c.YearsInBusiness,
            c.MonthlyRevenue, c.MonthlyExpenses,
            c.ClientType, c.ClientCategory, c.CollectorID, collectorName, c.AccountOfficer,
            c.EmergencyContactName, c.EmergencyContactRelationship, c.EmergencyContactPhone, c.EmergencyContactAddress,
            c.GuarantorName, c.GuarantorRelationship, c.GuarantorPhone, c.GuarantorOccupation, c.GuarantorEmployer, c.GuarantorAddress,
            c.RiskLevel, c.IsPoliticallyExposed, c.IsBlacklisted, c.AMLStatus,
            c.ValidationStatus, c.RejectionReason, c.AgenceID,
            c.CreatedBy, c.CreatedDate, c.ValidatedBy, c.ValidationDate, c.UpdatedBy, c.UpdatedDate,
            percent, label
        ));
    }

    // POST api/client/upload -> stores a KYC document/photo and returns its URL.
    // Simple local-disk storage under wwwroot/uploads/clients (no blob storage
    // configured for this project) — swap for Azure Blob/S3 later if needed.
    [HttpPost("upload")]
    [RequestSizeLimit(10_000_000)]
    public async Task<ActionResult<FileUploadResultDto>> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0) return BadRequest(new { message = "Fichier vide." });

        var allowedExt = new[] { ".jpg", ".jpeg", ".png", ".pdf", ".webp" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExt.Contains(ext)) return BadRequest(new { message = "Type de fichier non supporté." });

        var uploadsRoot = Path.Combine(_env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"), "uploads", "clients");
        Directory.CreateDirectory(uploadsRoot);

        var fileName = $"{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(uploadsRoot, fileName);
        await using (var stream = System.IO.File.Create(fullPath))
            await file.CopyToAsync(stream);

        return Ok(new FileUploadResultDto($"/uploads/clients/{fileName}"));
    }

    [HttpPost]
    public async Task<ActionResult> Create(CreateClientRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.NumeroCNI) &&
            await _db.Clients.IgnoreQueryFilters().AnyAsync(c => c.NumeroCNI == request.NumeroCNI))
            return BadRequest(new { message = "Ce numéro de CNI est déjà utilisé par un autre client." });

        if (!string.IsNullOrWhiteSpace(request.PhoneNumber) &&
            await _db.Clients.IgnoreQueryFilters().AnyAsync(c => c.PhoneNumber == request.PhoneNumber))
            return BadRequest(new { message = "Ce numéro de téléphone est déjà utilisé par un autre client." });

        var draft = MapToTmp(new ClientTmp { ActionType = PendingActionType.CREATE }, request);
        draft.AgenceID = _currentUser.AgenceID;
        draft.RequestUser = _currentUser.CodeUser!;
        draft.NewData = JsonSerializer.Serialize(request);

        _db.ClientTmps.Add(draft);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Client soumis pour validation.", pendingId = draft.PendingID });
    }

    [HttpGet("pending")]
    [Authorize(Policy = "SupervisorOrAdmin")]
    public async Task<ActionResult<IEnumerable<ClientTmp>>> GetPending()
    {
        var pending = await _db.ClientTmps
            .Where(t => t.PendingStatus == PendingStatusEnum.PENDING
                        && (_currentUser.IsHeadOffice || t.AgenceID == _currentUser.AgenceID))
            .OrderBy(t => t.RequestDate)
            .ToListAsync();
        return Ok(pending);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(string id, CreateClientRequest request)
    {
        var existing = await _db.Clients.FirstOrDefaultAsync(c => c.ClientID == id)
            ?? throw new KeyNotFoundException("Client not found in your agency.");

        if (!string.IsNullOrWhiteSpace(request.NumeroCNI) &&
            await _db.Clients.IgnoreQueryFilters().AnyAsync(c => c.NumeroCNI == request.NumeroCNI && c.ClientID != id))
            return BadRequest(new { message = "Ce numéro de CNI est déjà utilisé par un autre client." });

        var draft = MapToTmp(new ClientTmp { ActionType = PendingActionType.UPDATE, TargetClientID = id }, request);
        draft.AgenceID = existing.AgenceID;
        draft.RequestUser = _currentUser.CodeUser!;
        draft.PreviousData = JsonSerializer.Serialize(existing);
        draft.NewData = JsonSerializer.Serialize(request);

        _db.ClientTmps.Add(draft);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Modification soumise pour validation.", pendingId = draft.PendingID });
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(string id)
    {
        var existing = await _db.Clients.FirstOrDefaultAsync(c => c.ClientID == id)
            ?? throw new KeyNotFoundException("Client not found in your agency.");

        if (await _db.Accounts.IgnoreQueryFilters().AnyAsync(a => a.ClientID == id))
            return BadRequest(new { message = "Impossible : ce client possède des comptes actifs." });

        var draft = new ClientTmp
        {
            ActionType = PendingActionType.DELETE,
            TargetClientID = id,
            AgenceID = existing.AgenceID,
            RequestUser = _currentUser.CodeUser!,
            PreviousData = JsonSerializer.Serialize(existing)
        };

        _db.ClientTmps.Add(draft);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Suppression soumise pour validation.", pendingId = draft.PendingID });
    }

    [HttpPost("pending/{pendingId:int}/approve")]
    [Authorize(Policy = "SupervisorOrAdmin")]
    public async Task<ActionResult> Approve(int pendingId)
    {
        var draft = await _db.ClientTmps.FirstOrDefaultAsync(t => t.PendingID == pendingId)
            ?? throw new KeyNotFoundException("Pending record not found.");

        if (draft.PendingStatus != PendingStatusEnum.PENDING)
            return BadRequest(new { message = "This request has already been processed." });

        if (draft.ActionType == PendingActionType.CREATE)
        {
            var statusId = await _db.ClientStatuses.Where(s => s.Code == "VALIDATED").Select(s => s.ClientStatusID).FirstAsync();
            var count = await _db.Clients.IgnoreQueryFilters().CountAsync();
            var newId = $"CL-{(count + 1):D5}";

            _db.Clients.Add(new Client
            {
                ClientID = newId,
                Nom = draft.Nom!, Prenom = draft.Prenom, MiddleName = draft.MiddleName, Sexe = draft.Sexe,
                Image = draft.Image, PhoneNumber = draft.PhoneNumber, Address = draft.Address, Email = draft.Email,
                CompanyName = draft.CompanyName, ClientType = draft.ClientType ?? "INDIVIDUAL",
                ClientStatusID = statusId, TypeCNIID = draft.TypeCNIID, NumeroCNI = draft.NumeroCNI,
                CollectorID = draft.CollectorID, AgenceID = draft.AgenceID!.Value,
                DateOfBirth = draft.DateOfBirth, PlaceOfBirth = draft.PlaceOfBirth, Nationality = draft.Nationality,
                MaritalStatus = draft.MaritalStatus, Profession = draft.Profession, Occupation = draft.Occupation,
                Employer = draft.Employer, EducationLevel = draft.EducationLevel, MonthlyIncome = draft.MonthlyIncome,
                SecondaryPhone = draft.SecondaryPhone, WhatsApp = draft.WhatsApp, Country = draft.Country,
                City = draft.City, District = draft.District, Neighborhood = draft.Neighborhood, Street = draft.Street,
                HouseNumber = draft.HouseNumber, PostalCode = draft.PostalCode, Latitude = draft.Latitude, Longitude = draft.Longitude,
                NationalIDIssueDate = draft.NationalIDIssueDate, NationalIDExpiryDate = draft.NationalIDExpiryDate,
                PassportNumber = draft.PassportNumber, DriverLicenseNumber = draft.DriverLicenseNumber,
                TaxIdentificationNumber = draft.TaxIdentificationNumber, SocialSecurityNumber = draft.SocialSecurityNumber,
                DocumentType = draft.DocumentType, IssuedBy = draft.IssuedBy,
                NationalIDFrontUrl = draft.NationalIDFrontUrl, NationalIDBackUrl = draft.NationalIDBackUrl,
                PassportUrl = draft.PassportUrl, ProofOfAddressUrl = draft.ProofOfAddressUrl, SignatureUrl = draft.SignatureUrl,
                BusinessName = draft.BusinessName, BusinessAddress = draft.BusinessAddress, BusinessType = draft.BusinessType,
                YearsInBusiness = draft.YearsInBusiness, MonthlyRevenue = draft.MonthlyRevenue, MonthlyExpenses = draft.MonthlyExpenses,
                ClientCategory = draft.ClientCategory ?? "INDIVIDUAL", AccountOfficer = draft.AccountOfficer,
                EmergencyContactName = draft.EmergencyContactName, EmergencyContactRelationship = draft.EmergencyContactRelationship,
                EmergencyContactPhone = draft.EmergencyContactPhone, EmergencyContactAddress = draft.EmergencyContactAddress,
                GuarantorName = draft.GuarantorName, GuarantorRelationship = draft.GuarantorRelationship,
                GuarantorPhone = draft.GuarantorPhone, GuarantorOccupation = draft.GuarantorOccupation,
                GuarantorEmployer = draft.GuarantorEmployer, GuarantorAddress = draft.GuarantorAddress,
                RiskLevel = draft.RiskLevel ?? "LOW", IsPoliticallyExposed = draft.IsPoliticallyExposed ?? false,
                IsBlacklisted = draft.IsBlacklisted ?? false, AMLStatus = draft.AMLStatus ?? "PENDING",
                ValidationStatus = "VALIDATED", CreatedBy = draft.RequestUser,
                ValidatedBy = _currentUser.CodeUser, ValidationDate = DateTime.UtcNow
            });
        }
        else if (draft.ActionType == PendingActionType.UPDATE && draft.TargetClientID != null)
        {
            var existing = await _db.Clients.FirstOrDefaultAsync(c => c.ClientID == draft.TargetClientID)
                ?? throw new KeyNotFoundException("Target client no longer exists.");
            ApplyTmpToClient(existing, draft);
            existing.UpdatedBy = _currentUser.CodeUser;
            existing.UpdatedDate = DateTime.UtcNow;
        }
        else if (draft.ActionType == PendingActionType.DELETE && draft.TargetClientID != null)
        {
            var existing = await _db.Clients.FirstOrDefaultAsync(c => c.ClientID == draft.TargetClientID)
                ?? throw new KeyNotFoundException("Target client no longer exists.");
            var blockedStatusId = await _db.ClientStatuses.Where(s => s.Code == "BLOCKED").Select(s => s.ClientStatusID).FirstOrDefaultAsync();
            if (blockedStatusId != 0) existing.ClientStatusID = blockedStatusId;
            existing.ValidationStatus = "BLOCKED";
        }

        draft.PendingStatus = PendingStatusEnum.APPROVED;
        draft.ValidationUser = _currentUser.CodeUser;
        draft.ValidationDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Client validé et créé en production." });
    }

    [HttpPost("pending/{pendingId:int}/reject")]
    [Authorize(Policy = "SupervisorOrAdmin")]
    public async Task<ActionResult> Reject(int pendingId, RejectRequest request)
    {
        var draft = await _db.ClientTmps.FirstOrDefaultAsync(t => t.PendingID == pendingId)
            ?? throw new KeyNotFoundException("Pending record not found.");

        draft.PendingStatus = PendingStatusEnum.REJECTED;
        draft.ValidationUser = _currentUser.CodeUser;
        draft.ValidationDate = DateTime.UtcNow;
        draft.RejectionReason = request.Reason;

        if (draft.TargetClientID != null)
        {
            var existing = await _db.Clients.FirstOrDefaultAsync(c => c.ClientID == draft.TargetClientID);
            if (existing != null) existing.RejectionReason = request.Reason;
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = "Client rejeté." });
    }

    // ---- helpers -------------------------------------------------------

    private static ClientTmp MapToTmp(ClientTmp t, CreateClientRequest r)
    {
        t.Nom = r.Nom; t.Prenom = r.Prenom; t.MiddleName = r.MiddleName; t.Sexe = r.Sexe;
        t.DateOfBirth = r.DateOfBirth; t.PlaceOfBirth = r.PlaceOfBirth; t.Nationality = r.Nationality;
        t.MaritalStatus = r.MaritalStatus; t.Profession = r.Profession; t.Occupation = r.Occupation;
        t.Employer = r.Employer; t.EducationLevel = r.EducationLevel; t.MonthlyIncome = r.MonthlyIncome;
        t.PhoneNumber = r.PhoneNumber; t.SecondaryPhone = r.SecondaryPhone; t.WhatsApp = r.WhatsApp; t.Email = r.Email;
        t.Country = r.Country; t.City = r.City; t.District = r.District; t.Neighborhood = r.Neighborhood;
        t.Street = r.Street; t.HouseNumber = r.HouseNumber; t.PostalCode = r.PostalCode;
        t.Latitude = r.Latitude; t.Longitude = r.Longitude; t.Address = r.Address;
        t.TypeCNIID = r.TypeCNIID; t.NumeroCNI = r.NumeroCNI;
        t.NationalIDIssueDate = r.NationalIDIssueDate; t.NationalIDExpiryDate = r.NationalIDExpiryDate;
        t.PassportNumber = r.PassportNumber; t.DriverLicenseNumber = r.DriverLicenseNumber;
        t.TaxIdentificationNumber = r.TaxIdentificationNumber; t.SocialSecurityNumber = r.SocialSecurityNumber;
        t.DocumentType = r.DocumentType; t.IssuedBy = r.IssuedBy;
        t.Image = r.Image; t.NationalIDFrontUrl = r.NationalIDFrontUrl; t.NationalIDBackUrl = r.NationalIDBackUrl;
        t.PassportUrl = r.PassportUrl; t.ProofOfAddressUrl = r.ProofOfAddressUrl; t.SignatureUrl = r.SignatureUrl;
        t.CompanyName = r.CompanyName; t.BusinessName = r.BusinessName; t.BusinessAddress = r.BusinessAddress;
        t.BusinessType = r.BusinessType; t.YearsInBusiness = r.YearsInBusiness;
        t.MonthlyRevenue = r.MonthlyRevenue; t.MonthlyExpenses = r.MonthlyExpenses;
        t.ClientType = r.ClientType; t.ClientCategory = r.ClientCategory; t.CollectorID = r.CollectorID;
        t.AccountOfficer = r.AccountOfficer;
        t.EmergencyContactName = r.EmergencyContactName; t.EmergencyContactRelationship = r.EmergencyContactRelationship;
        t.EmergencyContactPhone = r.EmergencyContactPhone; t.EmergencyContactAddress = r.EmergencyContactAddress;
        t.GuarantorName = r.GuarantorName; t.GuarantorRelationship = r.GuarantorRelationship;
        t.GuarantorPhone = r.GuarantorPhone; t.GuarantorOccupation = r.GuarantorOccupation;
        t.GuarantorEmployer = r.GuarantorEmployer; t.GuarantorAddress = r.GuarantorAddress;
        t.RiskLevel = r.RiskLevel; t.IsPoliticallyExposed = r.IsPoliticallyExposed;
        t.IsBlacklisted = r.IsBlacklisted; t.AMLStatus = r.AMLStatus;
        return t;
    }

    private static void ApplyTmpToClient(Client c, ClientTmp t)
    {
        if (t.Nom != null) c.Nom = t.Nom;
        if (t.Prenom != null) c.Prenom = t.Prenom;
        if (t.MiddleName != null) c.MiddleName = t.MiddleName;
        if (t.Sexe != null) c.Sexe = t.Sexe;
        if (t.DateOfBirth.HasValue) c.DateOfBirth = t.DateOfBirth;
        if (t.PlaceOfBirth != null) c.PlaceOfBirth = t.PlaceOfBirth;
        if (t.Nationality != null) c.Nationality = t.Nationality;
        if (t.MaritalStatus != null) c.MaritalStatus = t.MaritalStatus;
        if (t.Profession != null) c.Profession = t.Profession;
        if (t.Occupation != null) c.Occupation = t.Occupation;
        if (t.Employer != null) c.Employer = t.Employer;
        if (t.EducationLevel != null) c.EducationLevel = t.EducationLevel;
        if (t.MonthlyIncome.HasValue) c.MonthlyIncome = t.MonthlyIncome;
        if (t.PhoneNumber != null) c.PhoneNumber = t.PhoneNumber;
        if (t.SecondaryPhone != null) c.SecondaryPhone = t.SecondaryPhone;
        if (t.WhatsApp != null) c.WhatsApp = t.WhatsApp;
        if (t.Email != null) c.Email = t.Email;
        if (t.Country != null) c.Country = t.Country;
        if (t.City != null) c.City = t.City;
        if (t.District != null) c.District = t.District;
        if (t.Neighborhood != null) c.Neighborhood = t.Neighborhood;
        if (t.Street != null) c.Street = t.Street;
        if (t.HouseNumber != null) c.HouseNumber = t.HouseNumber;
        if (t.PostalCode != null) c.PostalCode = t.PostalCode;
        if (t.Latitude.HasValue) c.Latitude = t.Latitude;
        if (t.Longitude.HasValue) c.Longitude = t.Longitude;
        if (t.Address != null) c.Address = t.Address;
        if (t.NumeroCNI != null) c.NumeroCNI = t.NumeroCNI;
        if (t.NationalIDIssueDate.HasValue) c.NationalIDIssueDate = t.NationalIDIssueDate;
        if (t.NationalIDExpiryDate.HasValue) c.NationalIDExpiryDate = t.NationalIDExpiryDate;
        if (t.PassportNumber != null) c.PassportNumber = t.PassportNumber;
        if (t.DriverLicenseNumber != null) c.DriverLicenseNumber = t.DriverLicenseNumber;
        if (t.TaxIdentificationNumber != null) c.TaxIdentificationNumber = t.TaxIdentificationNumber;
        if (t.SocialSecurityNumber != null) c.SocialSecurityNumber = t.SocialSecurityNumber;
        if (t.DocumentType != null) c.DocumentType = t.DocumentType;
        if (t.IssuedBy != null) c.IssuedBy = t.IssuedBy;
        if (t.Image != null) c.Image = t.Image;
        if (t.NationalIDFrontUrl != null) c.NationalIDFrontUrl = t.NationalIDFrontUrl;
        if (t.NationalIDBackUrl != null) c.NationalIDBackUrl = t.NationalIDBackUrl;
        if (t.PassportUrl != null) c.PassportUrl = t.PassportUrl;
        if (t.ProofOfAddressUrl != null) c.ProofOfAddressUrl = t.ProofOfAddressUrl;
        if (t.SignatureUrl != null) c.SignatureUrl = t.SignatureUrl;
        if (t.CompanyName != null) c.CompanyName = t.CompanyName;
        if (t.BusinessName != null) c.BusinessName = t.BusinessName;
        if (t.BusinessAddress != null) c.BusinessAddress = t.BusinessAddress;
        if (t.BusinessType != null) c.BusinessType = t.BusinessType;
        if (t.YearsInBusiness.HasValue) c.YearsInBusiness = t.YearsInBusiness;
        if (t.MonthlyRevenue.HasValue) c.MonthlyRevenue = t.MonthlyRevenue;
        if (t.MonthlyExpenses.HasValue) c.MonthlyExpenses = t.MonthlyExpenses;
        if (t.ClientType != null) c.ClientType = t.ClientType;
        if (t.ClientCategory != null) c.ClientCategory = t.ClientCategory;
        if (t.CollectorID != null) c.CollectorID = t.CollectorID;
        if (t.AccountOfficer != null) c.AccountOfficer = t.AccountOfficer;
        if (t.EmergencyContactName != null) c.EmergencyContactName = t.EmergencyContactName;
        if (t.EmergencyContactRelationship != null) c.EmergencyContactRelationship = t.EmergencyContactRelationship;
        if (t.EmergencyContactPhone != null) c.EmergencyContactPhone = t.EmergencyContactPhone;
        if (t.EmergencyContactAddress != null) c.EmergencyContactAddress = t.EmergencyContactAddress;
        if (t.GuarantorName != null) c.GuarantorName = t.GuarantorName;
        if (t.GuarantorRelationship != null) c.GuarantorRelationship = t.GuarantorRelationship;
        if (t.GuarantorPhone != null) c.GuarantorPhone = t.GuarantorPhone;
        if (t.GuarantorOccupation != null) c.GuarantorOccupation = t.GuarantorOccupation;
        if (t.GuarantorEmployer != null) c.GuarantorEmployer = t.GuarantorEmployer;
        if (t.GuarantorAddress != null) c.GuarantorAddress = t.GuarantorAddress;
        if (t.RiskLevel != null) c.RiskLevel = t.RiskLevel;
        if (t.IsPoliticallyExposed.HasValue) c.IsPoliticallyExposed = t.IsPoliticallyExposed.Value;
        if (t.IsBlacklisted.HasValue) c.IsBlacklisted = t.IsBlacklisted.Value;
        if (t.AMLStatus != null) c.AMLStatus = t.AMLStatus;
    }

    // Customer Profile Completeness — 20 equally-weighted checkpoints spanning
    // identity, contact, KYC documents, business, emergency contact, guarantor.
    private static (int percent, string label) ComputeCompleteness(Client c)
    {
        var checks = new[]
        {
            !string.IsNullOrWhiteSpace(c.Nom),
            !string.IsNullOrWhiteSpace(c.Prenom),
            c.DateOfBirth.HasValue,
            !string.IsNullOrWhiteSpace(c.Nationality),
            !string.IsNullOrWhiteSpace(c.PhoneNumber),
            !string.IsNullOrWhiteSpace(c.Email),
            !string.IsNullOrWhiteSpace(c.Address) || !string.IsNullOrWhiteSpace(c.City),
            !string.IsNullOrWhiteSpace(c.NumeroCNI),
            c.NationalIDExpiryDate.HasValue,
            !string.IsNullOrWhiteSpace(c.Image),
            !string.IsNullOrWhiteSpace(c.NationalIDFrontUrl),
            !string.IsNullOrWhiteSpace(c.NationalIDBackUrl),
            !string.IsNullOrWhiteSpace(c.SignatureUrl),
            !string.IsNullOrWhiteSpace(c.ProofOfAddressUrl),
            !string.IsNullOrWhiteSpace(c.Profession) || !string.IsNullOrWhiteSpace(c.Occupation),
            c.MonthlyIncome.HasValue,
            !string.IsNullOrWhiteSpace(c.CollectorID),
            !string.IsNullOrWhiteSpace(c.EmergencyContactName) && !string.IsNullOrWhiteSpace(c.EmergencyContactPhone),
            !string.IsNullOrWhiteSpace(c.GuarantorName) && !string.IsNullOrWhiteSpace(c.GuarantorPhone),
            !string.IsNullOrWhiteSpace(c.AMLStatus) && c.AMLStatus != "PENDING",
        };

        var percent = (int)Math.Round(checks.Count(x => x) * 100.0 / checks.Length);
        var label = percent >= 90 ? "Excellent" : percent >= 70 ? "Bon" : percent >= 50 ? "Moyen" : "Incomplet";
        return (percent, label);
    }
}
