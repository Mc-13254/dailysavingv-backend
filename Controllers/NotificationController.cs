using DailySavingV.API.Data;
using DailySavingV.API.DTOs;
using DailySavingV.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DailySavingV.API.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public NotificationController(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<NotificationDto>>> List([FromQuery] bool unreadOnly = false)
    {
        var query = _db.Notifications.Where(n => n.CodeUser == _currentUser.CodeUser);
        if (unreadOnly) query = query.Where(n => !n.IsRead);

        var notifications = await query.OrderByDescending(n => n.CreatedDate).Take(50).ToListAsync();
        return Ok(notifications.Select(n => new NotificationDto(n.NotificationID, n.Title, n.Message, n.Severity, n.Link, n.IsRead, n.CreatedDate)));
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<int>> UnreadCount() =>
        Ok(await _db.Notifications.CountAsync(n => n.CodeUser == _currentUser.CodeUser && !n.IsRead));

    [HttpPost("{id:int}/read")]
    public async Task<ActionResult> MarkRead(int id)
    {
        var n = await _db.Notifications.FirstOrDefaultAsync(n => n.NotificationID == id && n.CodeUser == _currentUser.CodeUser)
            ?? throw new KeyNotFoundException("Notification introuvable.");
        n.IsRead = true;
        n.ReadDate = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpPost("read-all")]
    public async Task<ActionResult> MarkAllRead()
    {
        var unread = await _db.Notifications.Where(n => n.CodeUser == _currentUser.CodeUser && !n.IsRead).ToListAsync();
        foreach (var n in unread) { n.IsRead = true; n.ReadDate = DateTime.UtcNow; }
        await _db.SaveChangesAsync();
        return Ok(new { count = unread.Count });
    }
}
