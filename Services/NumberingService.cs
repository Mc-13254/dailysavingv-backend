using DailySavingV.API.Data;
using Microsoft.EntityFrameworkCore;

namespace DailySavingV.API.Services;

/// <summary>
/// Generates the next formatted code for a given entity (e.g. "CLI000126")
/// based on its configured Numbering Parameter, and atomically increments
/// the counter. Call this instead of hand-rolling code generation in other
/// controllers (Agency, User, Collector, Client, Contract, Transaction...).
/// </summary>
public class NumberingService
{
    private readonly AppDbContext _db;
    public NumberingService(AppDbContext db) => _db = db;

    public async Task<string> GenerateNextAsync(string entityName)
    {
        var rule = await _db.NumberingParameters
            .FirstOrDefaultAsync(p => p.EntityName == entityName && p.Statut == "ACTIVE")
            ?? throw new InvalidOperationException($"No active numbering rule configured for entity '{entityName}'.");

        var next = rule.CurrentNumber == 0 ? rule.StartingNumber : rule.CurrentNumber + rule.IncrementValue;

        if (rule.AllowReset && rule.NextResetDate.HasValue && DateTime.UtcNow >= rule.NextResetDate.Value)
        {
            next = rule.StartingNumber;
            rule.NextResetDate = rule.ResetFrequency switch
            {
                "Daily" => DateTime.UtcNow.AddDays(1),
                "Monthly" => DateTime.UtcNow.AddMonths(1),
                "Yearly" => DateTime.UtcNow.AddYears(1),
                _ => null
            };
        }

        rule.CurrentNumber = next;
        var code = rule.BuildPreview(next);
        await _db.SaveChangesAsync();
        return code;
    }
}
