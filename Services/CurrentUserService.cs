using DailySavingV.API.Services.Interfaces;

namespace DailySavingV.API.Services;

public class CurrentUserService : ICurrentUserService
{
    public string? CodeUser { get; }
    public int? AgenceID { get; }
    public string? RoleCode { get; }
    public string? RoleType { get; }
    public bool IsHeadOffice { get; }

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        var user = httpContextAccessor.HttpContext?.User;
        if (user == null || !(user.Identity?.IsAuthenticated ?? false))
        {
            // No authenticated context (e.g. design-time / migrations) -> no scoping applied by default
            return;
        }

        CodeUser = user.FindFirst("codeUser")?.Value;
        RoleCode = user.FindFirst("role")?.Value;
        RoleType = user.FindFirst("roleType")?.Value;

        var agenceClaim = user.FindFirst("agenceId")?.Value;
        AgenceID = string.IsNullOrEmpty(agenceClaim) ? null : int.Parse(agenceClaim);

        // Only ADMIN role is treated as Head Office (sees every agency).
        // SUPERVISOR and COLLECTOR are always confined to their own AgenceID.
        IsHeadOffice = RoleType == "ADMIN";
    }
}
