namespace DailySavingV.API.Services.Interfaces;

/// <summary>
/// Exposes the identity of the currently connected user (from JWT claims),
/// scoped per-request. This is what the AppDbContext's global query filters
/// read from to enforce "only see your own agency's data".
/// </summary>
public interface ICurrentUserService
{
    string? CodeUser { get; }
    int? AgenceID { get; }
    string? RoleCode { get; }

    /// <summary>
    /// True for roles that must see data across all agencies (e.g. ADMIN at HQ).
    /// False for everyone else, including SUPERVISOR (agency-level, not HQ-level).
    /// </summary>
    bool IsHeadOffice { get; }
}
