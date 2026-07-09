using DailySavingV.API.Data;
using DailySavingV.API.DTOs;
using DailySavingV.API.Entities;
using DailySavingV.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DailySavingV.API.Controllers;

/// <summary>
/// Teller Management: the agency-level cash Vault that supplies/receives cash
/// to/from individual tellers (collectors/cashiers). Complements — does not
/// replace — the per-user Cash Session reconciliation that already exists.
/// </summary>
[ApiController]
[Route("api/teller")]
[Authorize]
public class TellerController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public TellerController(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    private async Task<Vault> GetOrCreateVaultAsync(int agenceId)
    {
        var vault = await _db.Vaults.FirstOrDefaultAsync(v => v.AgenceID == agenceId);
        if (vault == null)
        {
            vault = new Vault { AgenceID = agenceId, Balance = 0 };
            _db.Vaults.Add(vault);
            await _db.SaveChangesAsync();
        }
        return vault;
    }

    [HttpGet("vault")]
    public async Task<ActionResult<VaultDto>> GetVault()
    {
        var agenceId = _currentUser.AgenceID ?? 0;
        var vault = await GetOrCreateVaultAsync(agenceId);
        var agence = await _db.Agences.IgnoreQueryFilters().FirstOrDefaultAsync(a => a.AgenceID == agenceId);
        return Ok(new VaultDto(vault.VaultID, vault.AgenceID, agence?.Nom ?? "—", vault.Balance, vault.MinimumBalance, vault.MaximumBalance));
    }

    [HttpPut("vault/limits")]
    [Authorize(Policy = "SupervisorOrAdmin")]
    public async Task<ActionResult> SetVaultLimits([FromQuery] decimal? minimumBalance, [FromQuery] decimal? maximumBalance)
    {
        var vault = await GetOrCreateVaultAsync(_currentUser.AgenceID ?? 0);
        vault.MinimumBalance = minimumBalance;
        vault.MaximumBalance = maximumBalance;
        vault.UpdatedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Limites du coffre mises à jour." });
    }

    [HttpGet("movements")]
    public async Task<ActionResult<IEnumerable<CashMovementRowDto>>> GetMovements([FromQuery] string? status, [FromQuery] string? movementType)
    {
        var query = _db.CashMovements.AsQueryable();
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(m => m.Status == status);
        if (!string.IsNullOrWhiteSpace(movementType)) query = query.Where(m => m.MovementType == movementType);

        var movements = await query.OrderByDescending(m => m.RequestDate).Take(300).ToListAsync();
        var codeUsers = movements.SelectMany(m => new[] { m.FromCodeUser, m.ToCodeUser }).Where(c => c != null).Distinct().ToList();
        var users = await _db.Users.IgnoreQueryFilters().Where(u => codeUsers.Contains(u.CodeUser)).ToListAsync();
        var agencies = await _db.Agences.IgnoreQueryFilters().Where(a => movements.Select(m => m.AgenceID).Contains(a.AgenceID)).ToListAsync();

        string? NameOf(string? code) => code == null ? null : users.FirstOrDefault(u => u.CodeUser == code) is { } u ? $"{u.FirstName} {u.LastName}".Trim() : code;

        return Ok(movements.Select(m => new CashMovementRowDto(
            m.CashMovementID, m.MovementNumber, agencies.FirstOrDefault(a => a.AgenceID == m.AgenceID)?.Nom ?? "—", m.MovementType,
            m.FromCodeUser, NameOf(m.FromCodeUser) ?? "Coffre (Vault)", m.ToCodeUser, NameOf(m.ToCodeUser) ?? "Coffre (Vault)",
            m.Amount, m.Reason, m.Status, m.RequestedBy, m.RequestDate, m.ApprovedBy, m.ApprovalDate
        )));
    }

    [HttpPost("movements")]
    public async Task<ActionResult> RequestMovement(CreateCashMovementRequest request)
    {
        if (request.Amount <= 0) throw new InvalidOperationException("Le montant doit être supérieur à zéro.");
        if (request.MovementType is not ("SUPPLY" or "RETURN" or "TRANSFER"))
            throw new InvalidOperationException("Type de mouvement invalide.");
        if (request.MovementType == "TRANSFER" && (request.FromCodeUser == null || request.ToCodeUser == null))
            throw new InvalidOperationException("Un transfert nécessite un caissier source et un caissier destination.");

        var agenceId = _currentUser.AgenceID ?? 0;
        var count = await _db.CashMovements.CountAsync();

        var movement = new CashMovement
        {
            MovementNumber = $"CM-{DateTime.UtcNow:yyyyMMdd}-{(count + 1):D5}",
            AgenceID = agenceId,
            MovementType = request.MovementType,
            FromCodeUser = request.MovementType == "SUPPLY" ? null : request.FromCodeUser,
            ToCodeUser = request.MovementType == "RETURN" ? null : request.ToCodeUser,
            Amount = request.Amount,
            Reason = request.Reason,
            RequestedBy = _currentUser.CodeUser!
        };
        _db.CashMovements.Add(movement);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Mouvement de caisse soumis pour approbation.", movementNumber = movement.MovementNumber });
    }

    [HttpPost("movements/{id:int}/approve")]
    [Authorize(Policy = "SupervisorOrAdmin")]
    public async Task<ActionResult> ApproveMovement(int id)
    {
        var movement = await _db.CashMovements.FirstOrDefaultAsync(m => m.CashMovementID == id)
            ?? throw new KeyNotFoundException("Mouvement introuvable.");
        if (movement.Status != "PENDING") throw new InvalidOperationException("Ce mouvement a déjà été traité.");

        var vault = await GetOrCreateVaultAsync(movement.AgenceID);

        // SUPPLY: vault -> teller (vault decreases). RETURN: teller -> vault (vault increases).
        // TRANSFER: teller -> teller (vault untouched).
        if (movement.MovementType == "SUPPLY")
        {
            if (vault.Balance < movement.Amount)
                throw new InvalidOperationException("Solde du coffre insuffisant pour cet approvisionnement.");
            vault.Balance -= movement.Amount;
        }
        else if (movement.MovementType == "RETURN")
        {
            vault.Balance += movement.Amount;
        }
        vault.UpdatedDate = DateTime.UtcNow;

        movement.Status = "COMPLETED";
        movement.ApprovedBy = _currentUser.CodeUser;
        movement.ApprovalDate = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Mouvement de caisse approuvé et exécuté.", vaultBalance = vault.Balance });
    }

    [HttpPost("movements/{id:int}/reject")]
    [Authorize(Policy = "SupervisorOrAdmin")]
    public async Task<ActionResult> RejectMovement(int id, RejectCashMovementRequest request)
    {
        var movement = await _db.CashMovements.FirstOrDefaultAsync(m => m.CashMovementID == id)
            ?? throw new KeyNotFoundException("Mouvement introuvable.");
        if (movement.Status != "PENDING") throw new InvalidOperationException("Ce mouvement a déjà été traité.");

        movement.Status = "REJECTED";
        movement.RejectionReason = request.Reason;
        movement.ApprovedBy = _currentUser.CodeUser;
        movement.ApprovalDate = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Mouvement de caisse rejeté." });
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<TellerDashboardDto>> Dashboard()
    {
        var agenceId = _currentUser.AgenceID ?? 0;
        var vault = await GetOrCreateVaultAsync(agenceId);
        var today = DateTime.UtcNow.Date;

        var movements = await _db.CashMovements
            .Where(m => m.AgenceID == agenceId && m.Status == "COMPLETED" && m.ApprovalDate >= today)
            .ToListAsync();
        var pendingCount = await _db.CashMovements.CountAsync(m => m.AgenceID == agenceId && m.Status == "PENDING");

        return Ok(new TellerDashboardDto(
            vault.Balance, pendingCount,
            movements.Where(m => m.MovementType == "SUPPLY").Sum(m => m.Amount),
            movements.Where(m => m.MovementType == "RETURN").Sum(m => m.Amount),
            movements.Where(m => m.MovementType == "TRANSFER").Sum(m => m.Amount)
        ));
    }
}
