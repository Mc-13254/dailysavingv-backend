using DailySavingV.API.Data;
using DailySavingV.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace DailySavingV.API.Services;

public interface INotificationService
{
    Task SendAsync(string codeUser, string title, string message, string severity = "INFO", string? link = null);
    Task SendToSupervisorsAsync(int agenceId, string title, string message, string severity = "INFO", string? link = null);
}

public class NotificationService : INotificationService
{
    private readonly AppDbContext _db;

    public NotificationService(AppDbContext db)
    {
        _db = db;
    }

    public async Task SendAsync(string codeUser, string title, string message, string severity = "INFO", string? link = null)
    {
        _db.Notifications.Add(new Notification { CodeUser = codeUser, Title = title, Message = message, Severity = severity, Link = link });
        await _db.SaveChangesAsync();
    }

    public async Task SendToSupervisorsAsync(int agenceId, string title, string message, string severity = "INFO", string? link = null)
    {
        // "Supervisors" = anyone in this agency whose role code contains SUPERVISOR, MANAGER, or ADMIN.
        var recipients = await _db.Users.IgnoreQueryFilters()
            .Include(u => u.Role)
            .Where(u => u.Statut == "ACTIVE" && (u.AgenceID == agenceId || u.Role!.Code.Contains("ADMIN")))
            .Where(u => u.Role != null && (u.Role.Code.Contains("SUPERVISOR") || u.Role.Code.Contains("MANAGER") || u.Role.Code.Contains("ADMIN")))
            .Select(u => u.CodeUser)
            .Distinct()
            .ToListAsync();

        foreach (var codeUser in recipients)
        {
            _db.Notifications.Add(new Notification { CodeUser = codeUser, Title = title, Message = message, Severity = severity, Link = link });
        }
        await _db.SaveChangesAsync();
    }
}
