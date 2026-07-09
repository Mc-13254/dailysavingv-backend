using DailySavingV.API.Data;
using DailySavingV.API.DTOs;
using DailySavingV.API.Entities;
using DailySavingV.API.Services;
using DailySavingV.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DailySavingV.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TransactionController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITransactionService _transactionService;
    private readonly ICurrentUserService _currentUser;

    public TransactionController(AppDbContext db, ITransactionService transactionService, ICurrentUserService currentUser)
    {
        _db = db;
        _transactionService = transactionService;
        _currentUser = currentUser;
    }

    // GET api/transaction  -> auto agency-scoped (global query filter on Transactions)
    [HttpGet]
    public async Task<ActionResult> GetAll([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var query = _db.Transactions.AsQueryable();
        if (from.HasValue) query = query.Where(t => t.DateTransaction >= from.Value);
        if (to.HasValue) query = query.Where(t => t.DateTransaction <= to.Value);

        var result = await query
            .OrderByDescending(t => t.DateTransaction)
            .Select(t => new
            {
                t.TransactionID,
                t.ReceiptNumber,
                TransactionType = t.TransactionType.ToString(),
                t.AccountID,
                t.ToAccountID,
                t.ClientID,
                t.CollectorID,
                t.RemitterName,
                t.BeneficiaryName,
                t.Montant,
                t.MontantCommission,
                t.DateTransaction,
                t.Statut
            })
            .ToListAsync();

        return Ok(result);
    }

    // GET api/transaction/client-lookup?search=... -> powers the "bank receipt" style
    // transaction form: search a client, auto-load their accounts + assigned collector.
    [HttpGet("client-lookup")]
    public async Task<ActionResult<IEnumerable<ClientAccountLookupDto>>> ClientLookup([FromQuery] string search)
    {
        if (string.IsNullOrWhiteSpace(search) || search.Length < 2) return Ok(new List<ClientAccountLookupDto>());

        var clients = await _db.Clients
            .Where(c => c.Nom.Contains(search) || c.ClientID.Contains(search)
                || (c.PhoneNumber != null && c.PhoneNumber.Contains(search))
                || (c.Prenom != null && c.Prenom.Contains(search)))
            .Take(15)
            .ToListAsync();

        var clientIds = clients.Select(c => c.ClientID).ToList();
        var accounts = await _db.Accounts.Include(a => a.Contract)
            .Where(a => clientIds.Contains(a.ClientID))
            .ToListAsync();

        var collectorIds = clients.Where(c => c.CollectorID != null).Select(c => c.CollectorID!).Distinct().ToList();
        var collectorNames = await _db.Collectors.IgnoreQueryFilters()
            .Where(c => collectorIds.Contains(c.CollectorID))
            .ToDictionaryAsync(c => c.CollectorID, c => $"{c.Name} {c.Surname}".Trim());

        var result = clients.Select(c => new ClientAccountLookupDto(
            c.ClientID, $"{c.Nom} {c.Prenom}".Trim(), c.PhoneNumber,
            c.CollectorID, c.CollectorID != null ? collectorNames.GetValueOrDefault(c.CollectorID) : null,
            accounts.Where(a => a.ClientID == c.ClientID).Select(a => new AccountLookupDto(
                a.AccountID, a.AccountType, a.Balance, a.Status, a.ContractID, a.Contract?.ContractNumber
            )).ToList()
        ));

        return Ok(result);
    }

    // POST api/transaction  -> creates AND immediately validates + auto-calculates commission
    [HttpPost]
    public async Task<ActionResult<TransactionReceiptDto>> Create(CreateTransactionRequest request)
    {
        try
        {
            var receipt = await _transactionService.CreateAndValidateAsync(request, _currentUser.CodeUser!);
            return Ok(receipt);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // GET api/transaction/dashboard-summary -> feeds dashboards/reports
    [HttpGet("dashboard-summary")]
    public async Task<ActionResult> DashboardSummary()
    {
        var today = DateTime.UtcNow.Date;
        var summary = await _db.Transactions
            .Where(t => t.DateTransaction >= today)
            .GroupBy(t => 1)
            .Select(g => new
            {
                TotalTransactions = g.Count(),
                TotalMontant = g.Sum(t => t.Montant),
                TotalCommission = g.Sum(t => t.MontantCommission)
            })
            .FirstOrDefaultAsync();

        return Ok(summary ?? new { TotalTransactions = 0, TotalMontant = 0m, TotalCommission = 0m });
    }

    // ============================================================
    // Excel bulk import — rows are parsed client-side (frontend reads the
    // spreadsheet), submitted here as JSON, and stored as a PENDING batch.
    // Nothing is posted to Transactions/balances until each row is approved
    // through the Validation Queue (Maker-Checker), same as every other
    // module in this application.
    // ============================================================

    [HttpPost("import-batch")]
    public async Task<ActionResult> CreateImportBatch(CreateImportBatchRequest request)
    {
        if (request.Rows.Count == 0) return BadRequest(new { message = "Le fichier ne contient aucune ligne exploitable." });

        var batch = new TransactionImportBatch
        {
            FileName = request.FileName,
            UploadedBy = _currentUser.CodeUser!,
            AgenceID = _currentUser.AgenceID ?? 0,
            TotalRows = request.Rows.Count,
            Status = "PENDING"
        };

        var rowNumber = 1;
        foreach (var r in request.Rows)
        {
            batch.Rows.Add(new TransactionImportRow
            {
                RowNumber = rowNumber++,
                TransactionType = r.TransactionType,
                AccountID = r.AccountID,
                ToAccountID = r.ToAccountID,
                CollectorID = r.CollectorID,
                Montant = r.Montant,
                RemitterName = r.RemitterName,
                BeneficiaryName = r.BeneficiaryName,
                RefRowLabel = r.RefRowLabel,
                Status = "PENDING"
            });
        }

        _db.TransactionImportBatches.Add(batch);
        await _db.SaveChangesAsync();

        return Ok(new { message = $"{request.Rows.Count} ligne(s) importée(s) et soumise(s) pour validation.", batchId = batch.BatchID });
    }

    [HttpGet("import-batch/pending")]
    [Authorize(Policy = "SupervisorOrAdmin")]
    public async Task<ActionResult<IEnumerable<ImportBatchRowDto>>> GetPendingImportRows()
    {
        var query = _db.TransactionImportRows.Include(r => r.Batch).Where(r => r.Status == "PENDING");
        if (!_currentUser.IsHeadOffice)
            query = query.Where(r => r.Batch!.AgenceID == _currentUser.AgenceID);

        var rows = await query.OrderBy(r => r.Batch!.UploadedDate).ThenBy(r => r.RowNumber).ToListAsync();

        return Ok(rows.Select(r => new ImportBatchRowDto(
            r.RowID, r.RowNumber, r.TransactionType, r.AccountID, r.ToAccountID, r.CollectorID,
            r.Montant, r.RemitterName, r.BeneficiaryName, r.Status, r.ErrorMessage, r.RefRowLabel
        )));
    }

    [HttpPost("import-batch/row/{rowId:int}/approve")]
    [Authorize(Policy = "SupervisorOrAdmin")]
    public async Task<ActionResult> ApproveRow(int rowId)
    {
        var row = await _db.TransactionImportRows.FirstOrDefaultAsync(r => r.RowID == rowId)
            ?? throw new KeyNotFoundException("Ligne d'import introuvable.");

        if (row.Status != "PENDING") return BadRequest(new { message = "Cette ligne a déjà été traitée." });

        if (!Enum.TryParse<TransactionType>(row.TransactionType, true, out var txType))
        {
            row.Status = "ERROR";
            row.ErrorMessage = $"Type de transaction inconnu: {row.TransactionType}";
            await _db.SaveChangesAsync();
            return BadRequest(new { message = row.ErrorMessage });
        }

        try
        {
            var receipt = await _transactionService.CreateAndValidateAsync(
               new CreateTransactionRequest(txType, row.AccountID, row.ToAccountID, row.CollectorID, row.Montant, row.RemitterName, row.BeneficiaryName, null, null),
                _currentUser.CodeUser!);

            row.Status = "APPROVED";
            row.TransactionID = receipt.TransactionID;
            row.ApprovedBy = _currentUser.CodeUser;
            row.ApprovalDate = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok(new { message = "Ligne validée et transaction créée.", receipt });
        }
        catch (InvalidOperationException ex)
        {
            row.Status = "ERROR";
            row.ErrorMessage = ex.Message;
            await _db.SaveChangesAsync();
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("import-batch/row/{rowId:int}/reject")]
    [Authorize(Policy = "SupervisorOrAdmin")]
    public async Task<ActionResult> RejectRow(int rowId, RejectRequest request)
    {
        var row = await _db.TransactionImportRows.FirstOrDefaultAsync(r => r.RowID == rowId)
            ?? throw new KeyNotFoundException("Ligne d'import introuvable.");

        row.Status = "REJECTED";
        row.ErrorMessage = request.Reason;
        row.ApprovedBy = _currentUser.CodeUser;
        row.ApprovalDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Ligne rejetée." });
    }
}
