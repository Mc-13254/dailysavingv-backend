using DailySavingV.API.Data;
using DailySavingV.API.DTOs;
using DailySavingV.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DailySavingV.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AdminOnly")]
public class PermissionsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public PermissionsController(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    // Overrides the class-level AdminOnly policy — every authenticated user needs
    // this to know which sidebar sections they're allowed to see, not just admins.
    [HttpGet("my-modules")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<string>>> MyModules()
    {
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Code == _currentUser.RoleCode);
        if (role == null) return Ok(new List<string>());

        // Administrators always see everything, regardless of stored RolePermission rows.
        if (role.RoleType == "ADMIN")
            return Ok(await _db.Permissions.Select(p => p.Module).Distinct().ToListAsync());

        var modules = await _db.RolePermissions
            .Where(rp => rp.RoleID == role.RoleID && rp.Allowed)
            .Join(_db.Permissions, rp => rp.PermissionID, p => p.PermissionID, (rp, p) => p.Module)
            .Distinct()
            .ToListAsync();

        return Ok(modules);
    }

    // Full catalog, grouped by Module on the frontend.
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PermissionDto>>> GetAll()
    {
        var result = await _db.Permissions
            .Select(p => new PermissionDto(p.PermissionID, p.PermissionCode, p.PermissionName, p.Module, p.Action))
            .ToListAsync();
        return Ok(result);
    }

    // The full permission matrix for one role, with each permission's current Allowed state
    // (defaults to false if no RolePermission row exists yet for that pair).
    [HttpGet("role/{roleId:int}")]
    public async Task<ActionResult<IEnumerable<RolePermissionStateDto>>> GetForRole(int roleId)
    {
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.RoleID == roleId)
            ?? throw new KeyNotFoundException("Role not found.");

        var allowedMap = await _db.RolePermissions
            .Where(rp => rp.RoleID == roleId)
            .ToDictionaryAsync(rp => rp.PermissionID, rp => rp.Allowed);

        var permissions = await _db.Permissions.ToListAsync();

        // Administrator always shows (and enforces) full access, regardless of stored rows.
        var isAdmin = role.RoleType == "ADMIN";

        var result = permissions.Select(p => new RolePermissionStateDto(
            p.PermissionID, p.PermissionName, p.Module, p.Action,
            isAdmin || (allowedMap.TryGetValue(p.PermissionID, out var allowed) && allowed)
        ));
        return Ok(result);
    }

    [HttpPost("role/{roleId:int}")]
    public async Task<ActionResult> SaveForRole(int roleId, SaveRolePermissionsRequest request)
    {
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.RoleID == roleId)
            ?? throw new KeyNotFoundException("Role not found.");

        // Administrator role always has full permissions — this endpoint is a no-op for it,
        // but still returns success so the frontend doesn't show a confusing error.
        if (role.RoleType == "ADMIN")
            return Ok(new { message = "Le rôle Administrator possède toujours tous les droits." });

        var existing = await _db.RolePermissions.Where(rp => rp.RoleID == roleId).ToListAsync();
        var existingMap = existing.ToDictionary(rp => rp.PermissionID);

        foreach (var item in request.Permissions)
        {
            if (existingMap.TryGetValue(item.PermissionID, out var row))
            {
                row.Allowed = item.Allowed;
            }
            else
            {
                _db.RolePermissions.Add(new Entities.RolePermission
                {
                    RoleID = roleId,
                    PermissionID = item.PermissionID,
                    Allowed = item.Allowed
                });
            }
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = "Permissions enregistrées." });
    }
}
