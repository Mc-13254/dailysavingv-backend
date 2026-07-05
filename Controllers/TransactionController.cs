using DailySavingV.API.Data;
using DailySavingV.API.DTOs;
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
                t.Montant,
                t.MontantCommission,
                t.DateTransaction,
                t.Statut
            })
            .ToListAsync();

        return Ok(result);
    }

    // POST api/transaction  -> creates AND immediately validates + auto-calculates commission
    // (Per spec: transactions are not subject to Maker-Checker the same way
    // master data is - the commission calculation itself must be automatic
    // and immediate so the receipt can be printed on the spot.)
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

    // GET api/transaction/dashboard-summary -> feeds dashboards/reports (requirement #8)
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
}
