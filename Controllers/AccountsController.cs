using System.Text.Json;
using DailySavingV.API.Data;
using DailySavingV.API.DTOs;
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
        var query = _db.Accounts.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(a => a.AccountID.Contains(search) || a.NumCarnet!.Contains(search) || a.ClientID.Contains(search));

        var result = await query
            .Select(a => new AccountFullDto(a.AccountID, a.ClientID, a.NumCarnet, a.Balance, a.Active, a.AgenceID, a.CreatedBy, a.CreateDate))
            .ToListAsync();
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult> Create(CreateAccountRequest request)
    {
        var draft = new AccountsTMP
        {
            ActionType = PendingActionType.CREATE,
            ClientID = request.ClientID,
            NumCarnet = request.NumCarnet,
            Balance = 0,
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

            _db.Accounts.Add(new Entities.Accounts
            {
                AccountID = newId,
                ClientID = draft.ClientID!,
                NumCarnet = draft.NumCarnet,
                Balance = draft.Balance ?? 0,
                Active = draft.Active ?? true,
                AgenceID = draft.AgenceID!.Value,
                CreatedBy = draft.RequestUser
            });
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
