using DailySavingV.API.Data;
using DailySavingV.API.DTOs;
using DailySavingV.API.Entities;
using DailySavingV.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DailySavingV.API.Controllers;

[ApiController]
[Route("api/documents")]
[Authorize]
public class DocumentController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IWebHostEnvironment _env;

    private static readonly HashSet<string> AllowedExtensions = new() { ".pdf", ".png", ".jpg", ".jpeg", ".docx", ".xlsx" };
    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

    public DocumentController(AppDbContext db, ICurrentUserService currentUser, IWebHostEnvironment env)
    {
        _db = db;
        _currentUser = currentUser;
        _env = env;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DocumentRowDto>>> List(
        [FromQuery] string? entityType, [FromQuery] string? entityId, [FromQuery] string? search)
    {
        var query = _db.Documents.Where(d => !d.IsDeleted).AsQueryable();
        if (!string.IsNullOrWhiteSpace(entityType)) query = query.Where(d => d.EntityType == entityType);
        if (!string.IsNullOrWhiteSpace(entityId)) query = query.Where(d => d.EntityID == entityId);
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(d => d.FileName.Contains(search) || (d.Tags != null && d.Tags.Contains(search)) || (d.Description != null && d.Description.Contains(search)));

        var docs = await query.OrderByDescending(d => d.UploadDate).Take(300).ToListAsync();
        return Ok(docs.Select(ToDto));
    }

    private static DocumentRowDto ToDto(DocumentRecord d) => new(
        d.DocumentID, d.EntityType, d.EntityID, d.FileName, d.FileType, d.FileSizeBytes,
        d.Description, d.Tags, d.UploadedBy, d.UploadDate, $"/uploads/documents/{Path.GetFileName(d.FilePath)}"
    );

    [HttpPost("upload")]
    public async Task<ActionResult<DocumentRowDto>> Upload(
        IFormFile file, [FromForm] string entityType, [FromForm] string? entityId,
        [FromForm] string? description, [FromForm] string? tags)
    {
        if (file == null || file.Length == 0) throw new InvalidOperationException("Aucun fichier fourni.");
        if (file.Length > MaxFileSizeBytes) throw new InvalidOperationException("Le fichier dépasse la taille maximale autorisée (10 Mo).");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            throw new InvalidOperationException($"Type de fichier non autorisé ({ext}). Formats acceptés : {string.Join(", ", AllowedExtensions)}.");

        var uploadsDir = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "documents");
        Directory.CreateDirectory(uploadsDir);

        var storedName = $"{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(uploadsDir, storedName);
        using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var doc = new DocumentRecord
        {
            EntityType = entityType,
            EntityID = entityId,
            FileName = file.FileName,
            FilePath = storedName,
            FileType = ext.TrimStart('.'),
            FileSizeBytes = file.Length,
            Description = description,
            Tags = tags,
            AgenceID = _currentUser.AgenceID ?? 0,
            UploadedBy = _currentUser.CodeUser!
        };
        _db.Documents.Add(doc);
        await _db.SaveChangesAsync();

        return Ok(ToDto(doc));
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        var doc = await _db.Documents.FirstOrDefaultAsync(d => d.DocumentID == id)
            ?? throw new KeyNotFoundException("Document introuvable.");

        doc.IsDeleted = true;
        doc.DeletedBy = _currentUser.CodeUser;
        doc.DeletedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Document supprimé." });
    }
}
