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
public class AccountsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AccountsController(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    // Auto agency-scoped via the Accounts global query filter in AppDbContext
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AccountFullDto>>> GetAll([FromQuery] string? search)
    {
        var query = _db.Accounts.Include(a => a.Contract).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(a => a.AccountID.Contains(search) || (a.NumCarnet != null && a.NumCarnet.Contains(search)) || a.ClientID.Contains(search));

        var accounts = await query.ToListAsync();

        var clientIds = accounts.Select(a => a.ClientID).Distinct().ToList();
        var clientNames = await _db.Clients.IgnoreQueryFilters().Where(c => clientIds.Contains(c.ClientID))
            .ToDictionaryAsync(c => c.ClientID, c => $"{c.Nom} {c.Prenom}".Trim());

        var collectorIds = accounts.Where(a => a.CollectorID != null).Select(a => a.CollectorID!).Distinct().ToList();
        var collectorNames = await _db.Collectors.IgnoreQueryFilters().Where(c => collectorIds.Contains(c.CollectorID))
            .ToDictionaryAsync(c => c.CollectorID, c => $"{c.Name} {c.Surname}".Trim());

        var result = accounts.Select(a => new AccountFullDto(
            a.AccountID, a.ClientID, clientNames.GetValueOrDefault(a.ClientID), a.NumCarnet,
            a.ContractID, a.Contract?.ContractNumber, a.CollectorID, a.CollectorID != null ? collectorNames.GetValueOrDefault(a.CollectorID) : null,
            a.AccountType, a.Currency, a.OpeningBalance, a.Balance, a.AvailableBalance, a.BlockedBalance,
            a.MinimumBalance, a.MaximumBalance, a.DailyDepositLimit, a.DailyWithdrawalLimit, a.DailyTransactionLimit,
            a.OverdraftAllowed, a.OverdraftLimit, a.Status, a.Active, a.AgenceID, a.CreatedBy, a.CreateDate
        ));
        return Ok(result);
    }

    // GET api/accounts/eligible-clients -> validated clients with an ACTIVE contract and no open account yet on that contract
    [HttpGet("eligible-contracts")]
    public async Task<ActionResult<IEnumerable<ContractFullDto>>> GetEligibleContracts()
    {
        var contractsWithAccount = await _db.Accounts.Where(a => a.ContractID != null).Select(a => a.ContractID!.Value).ToListAsync();

        var contracts = await _db.Contracts
            .Where(c => c.Statut == "ACTIVE" && !contractsWithAccount.Contains(c.ContractID))
            .ToListAsync();

        var clientIds = contracts.Where(c => c.ClientID != null).Select(c => c.ClientID!).Distinct().ToList();
        var clientNames = await _db.Clients.IgnoreQueryFilters().Where(c => clientIds.Contains(c.ClientID))
            .ToDictionaryAsync(c => c.ClientID, c => $"{c.Nom} {c.Prenom}".Trim());

        return Ok(contracts.Select(c => new ContractFullDto(
            c.ContractID, c.ContractNumber, c.ClientID, c.ClientID != null ? clientNames.GetValueOrDefault(c.ClientID) : null,
            c.AgenceID, c.CollectorID, null, c.CommissionTypeID, null, c.CommissionRangeID,
            c.CollectionFrequency, c.CollectionDay, c.OpeningDeposit, c.MinimumBalance, c.MaximumBalance,
            c.PenaltyRules, c.GracePeriod, c.StartDate, c.EndDate, c.ContractType, c.Description, c.Statut,
            c.TerminationReason, c.CustomerSigned, c.OfficerSigned
        )));
    }

    [HttpPost]
    public async Task<ActionResult> Create(CreateAccountRequest request)
    {
        var contract = await _db.Contracts.FirstOrDefaultAsync(c => c.ContractID == request.ContractID)
            ?? throw new KeyNotFoundException("Contract not found.");

        if (contract.Statut != "ACTIVE")
            return BadRequest(new { message = "Un compte ne peut être ouvert que pour un contrat Actif." });

        if (await _db.Accounts.AnyAsync(a => a.ContractID == request.ContractID))
            return BadRequest(new { message = "Un compte existe déjà pour ce contrat." });

        var draft = new AccountsTMP
        {
            ActionType = PendingActionType.CREATE,
            ClientID = request.ClientID,
            NumCarnet = request.NumCarnet,
            ContractID = request.ContractID,
            CollectorID = contract.CollectorID,
            AccountType = request.AccountType,
            Currency = request.Currency,
            OpeningBalance = request.OpeningBalance,
            Balance = request.OpeningBalance,
            AvailableBalance = request.OpeningBalance,
            BlockedBalance = 0,
            MinimumBalance = request.MinimumBalance,
            MaximumBalance = request.MaximumBalance,
            DailyDepositLimit = request.DailyDepositLimit,
            DailyWithdrawalLimit = request.DailyWithdrawalLimit,
            DailyTransactionLimit = request.DailyTransactionLimit,
            OverdraftAllowed = request.OverdraftAllowed,
            OverdraftLimit = request.OverdraftLimit,
            Status = "ACTIVE",
            Active = true,
            AgenceID = _currentUser.AgenceID,
            RequestUser = _currentUser.CodeUser!,
            NewData = JsonSerializer.Serialize(request)
        };

        _db.AccountsTMPs.Add(draft);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Compte soumis pour validation.", pendingId = draft.PendingID });
    }

    [HttpGet("pending")]
    [Authorize(Policy = "SupervisorOrAdmin")]
    public async Task<ActionResult<IEnumerable<AccountsTMP>>> GetPending()
    {
        var pending = await _db.AccountsTMPs
            .Where(t => t.PendingStatus == PendingStatusEnum.PENDING
                        && (_currentUser.IsHeadOffice || t.AgenceID == _currentUser.AgenceID))
            .OrderBy(t => t.RequestDate)
            .ToListAsync();
        return Ok(pending);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(string id, UpdateAccountRequest request)
    {
        var existing = await _db.Accounts.FirstOrDefaultAsync(a => a.AccountID == id)
            ?? throw new KeyNotFoundException("Account not found in your agency.");

        var draft = new AccountsTMP
        {
            ActionType = PendingActionType.UPDATE,
            TargetAccountID = id,
            MinimumBalance = request.MinimumBalance,
            MaximumBalance = request.MaximumBalance,
            DailyDepositLimit = request.DailyDepositLimit,
            DailyWithdrawalLimit = request.DailyWithdrawalLimit,
            DailyTransactionLimit = request.DailyTransactionLimit,
            OverdraftAllowed = request.OverdraftAllowed,
            OverdraftLimit = request.OverdraftLimit,
            Status = request.Status,
            AgenceID = existing.AgenceID,
            RequestUser = _currentUser.CodeUser!,
            PreviousData = JsonSerializer.Serialize(existing),
            NewData = JsonSerializer.Serialize(request)
        };

        _db.AccountsTMPs.Add(draft);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Modification soumise pour validation.", pendingId = draft.PendingID });
    }

    // Freeze/Unfreeze/Close bypass Maker-Checker (operational actions, reversible
    // except Close) but are always logged via UpdatedBy/UpdatedDate + reason.
    [HttpPost("{id}/freeze")]
    [Authorize(Policy = "SupervisorOrAdmin")]
    public async Task<ActionResult> Freeze(string id, FreezeAccountRequest request)
    {
        var account = await _db.Accounts.FirstOrDefaultAsync(a => a.AccountID == id)
            ?? throw new KeyNotFoundException("Account not found.");

        account.Status = "FROZEN";
        account.Active = false;
        account.FreezeReason = request.Reason;
        account.UpdatedBy = _currentUser.CodeUser;
        account.UpdatedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Compte gelé." });
    }

    [HttpPost("{id}/unfreeze")]
    [Authorize(Policy = "SupervisorOrAdmin")]
    public async Task<ActionResult> Unfreeze(string id)
    {
        var account = await _db.Accounts.FirstOrDefaultAsync(a => a.AccountID == id)
            ?? throw new KeyNotFoundException("Account not found.");

        account.Status = "ACTIVE";
        account.Active = true;
        account.FreezeReason = null;
        account.UpdatedBy = _currentUser.CodeUser;
        account.UpdatedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Compte réactivé." });
    }

    // Soft close only — account row is never deleted.
    [HttpPost("{id}/close")]
    [Authorize(Policy = "SupervisorOrAdmin")]
    public async Task<ActionResult> Close(string id, CloseAccountRequest request)
    {
        var account = await _db.Accounts.FirstOrDefaultAsync(a => a.AccountID == id)
            ?? throw new KeyNotFoundException("Account not found.");

        account.Status = "CLOSED";
        account.Active = false;
        account.CloseReason = request.Reason;
        account.ClosingDate = DateTime.UtcNow;
        account.UpdatedBy = _currentUser.CodeUser;
        account.UpdatedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Compte clôturé." });
    }

    // GET api/accounts/{id}/statement?from=&to=
    [HttpGet("{id}/statement")]
    public async Task<ActionResult<AccountStatementDto>> GetStatement(string id, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var account = await _db.Accounts.FirstOrDefaultAsync(a => a.AccountID == id)
            ?? throw new KeyNotFoundException("Account not found.");
        var client = await _db.Clients.FirstOrDefaultAsync(c => c.ClientID == account.ClientID);

        var fromDate = from ?? account.CreateDate.Date;
        var toDate = to ?? DateTime.UtcNow.Date;

        var transactions = await _db.Transactions.IgnoreQueryFilters()
            .Where(t => t.AccountID == id && t.DateTransaction >= fromDate && t.DateTransaction < toDate.AddDays(1))
            .OrderBy(t => t.DateTransaction)
            .ToListAsync();

        var running = account.OpeningBalance;
        var lines = new List<StatementLineDto>();
        decimal totalDeposits = 0, totalWithdrawals = 0, totalCollections = 0;

        foreach (var t in transactions)
        {
            var isCredit = t.TransactionType == TransactionType.DAILY_COLLECTION || t.TransactionType == TransactionType.DEPOSIT;
            running += isCredit ? t.Montant : -t.Montant;

            if (t.TransactionType == TransactionType.DAILY_COLLECTION) totalCollections += t.Montant;
            else if (t.TransactionType == TransactionType.DEPOSIT) totalDeposits += t.Montant;
            else if (t.TransactionType == TransactionType.WITHDRAWAL) totalWithdrawals += t.Montant;

            lines.Add(new StatementLineDto(t.DateTransaction, t.TransactionType.ToString(), t.Statut, isCredit ? t.Montant : -t.Montant, running));
        }

        return Ok(new AccountStatementDto(
            account.AccountID, client != null ? $"{client.Nom} {client.Prenom}".Trim() : account.ClientID,
            account.OpeningBalance, running, totalDeposits, totalWithdrawals, totalCollections, lines
        ));
    }

    [HttpPost("pending/{pendingId:int}/approve")]
    [Authorize(Policy = "SupervisorOrAdmin")]
    public async Task<ActionResult> Approve(int pendingId)
    {
        var draft = await _db.AccountsTMPs.FirstOrDefaultAsync(t => t.PendingID == pendingId)
            ?? throw new KeyNotFoundException("Pending record not found.");

        if (draft.PendingStatus != PendingStatusEnum.PENDING)
            return BadRequest(new { message = "This request has already been processed." });

        if (draft.ActionType == PendingActionType.CREATE)
        {
            var count = await _db.Accounts.IgnoreQueryFilters().CountAsync();
            var newId = $"CC-{(count + 1):D6}";

            _db.Accounts.Add(new Accounts
            {
                AccountID = newId,
                ClientID = draft.ClientID!,
                NumCarnet = draft.NumCarnet,
                ContractID = draft.ContractID,
                CollectorID = draft.CollectorID,
                AccountType = draft.AccountType ?? "DAILY_SAVING",
                Currency = draft.Currency ?? "XAF",
                OpeningBalance = draft.OpeningBalance ?? 0,
                Balance = draft.Balance ?? 0,
                AvailableBalance = draft.AvailableBalance ?? 0,
                BlockedBalance = draft.BlockedBalance ?? 0,
                MinimumBalance = draft.MinimumBalance,
                MaximumBalance = draft.MaximumBalance,
                DailyDepositLimit = draft.DailyDepositLimit,
                DailyWithdrawalLimit = draft.DailyWithdrawalLimit,
                DailyTransactionLimit = draft.DailyTransactionLimit,
                OverdraftAllowed = draft.OverdraftAllowed ?? false,
                OverdraftLimit = draft.OverdraftLimit,
                Status = draft.Status ?? "ACTIVE",
                Active = draft.Active ?? true,
                AgenceID = draft.AgenceID!.Value,
                CreatedBy = draft.RequestUser
            });
        }
        else if (draft.ActionType == PendingActionType.UPDATE && draft.TargetAccountID != null)
        {
            var existing = await _db.Accounts.FirstOrDefaultAsync(a => a.AccountID == draft.TargetAccountID)
                ?? throw new KeyNotFoundException("Target account no longer exists.");
            if (draft.MinimumBalance.HasValue) existing.MinimumBalance = draft.MinimumBalance;
            if (draft.MaximumBalance.HasValue) existing.MaximumBalance = draft.MaximumBalance;
            if (draft.DailyDepositLimit.HasValue) existing.DailyDepositLimit = draft.DailyDepositLimit;
            if (draft.DailyWithdrawalLimit.HasValue) existing.DailyWithdrawalLimit = draft.DailyWithdrawalLimit;
            if (draft.DailyTransactionLimit.HasValue) existing.DailyTransactionLimit = draft.DailyTransactionLimit;
            if (draft.OverdraftAllowed.HasValue) existing.OverdraftAllowed = draft.OverdraftAllowed.Value;
            if (draft.OverdraftLimit.HasValue) existing.OverdraftLimit = draft.OverdraftLimit;
            if (draft.Status != null) { existing.Status = draft.Status; existing.Active = draft.Status == "ACTIVE"; }
            existing.UpdatedBy = _currentUser.CodeUser;
            existing.UpdatedDate = DateTime.UtcNow;
        }
        else if (draft.ActionType == PendingActionType.DELETE && draft.TargetAccountID != null)
        {
            var existing = await _db.Accounts.FirstOrDefaultAsync(a => a.AccountID == draft.TargetAccountID)
                ?? throw new KeyNotFoundException("Target account no longer exists.");
            existing.Status = "CLOSED";
            existing.Active = false;
            existing.ClosingDate = DateTime.UtcNow;
        }

        draft.PendingStatus = PendingStatusEnum.APPROVED;
        draft.ValidationUser = _currentUser.CodeUser;
        draft.ValidationDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Compte validé et créé en production." });
    }

    [HttpPost("pending/{pendingId:int}/reject")]
    [Authorize(Policy = "SupervisorOrAdmin")]
    public async Task<ActionResult> Reject(int pendingId, RejectRequest request)
    {
        var draft = await _db.AccountsTMPs.FirstOrDefaultAsync(t => t.PendingID == pendingId)
            ?? throw new KeyNotFoundException("Pending record not found.");

        draft.PendingStatus = PendingStatusEnum.REJECTED;
        draft.ValidationUser = _currentUser.CodeUser;
        draft.ValidationDate = DateTime.UtcNow;
        draft.RejectionReason = request.Reason;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Compte rejeté." });
    }
}
