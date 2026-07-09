using DailySavingV.API.Data;
using DailySavingV.API.DTOs;
using DailySavingV.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DailySavingV.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AuthController(IAuthService authService, AppDbContext db, ICurrentUserService currentUser)
    {
        _authService = authService;
        _db = db;
        _currentUser = currentUser;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();

        try
        {
            var result = await _authService.LoginAsync(request.Username, request.Password, ip, userAgent);
            if (result == null) return Unauthorized(new { message = "Identifiants invalides." });
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            // Account-locked case: give a specific, actionable message instead of the generic one.
            return StatusCode(423, new { message = ex.Message }); // 423 Locked
        }
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<LoginResponse>> Refresh(RefreshRequest request)
    {
        var result = await _authService.RefreshAsync(request.RefreshToken);
        if (result == null) return Unauthorized(new { message = "Refresh token invalide ou expiré." });
        return Ok(result);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(RefreshRequest request)
    {
        await _authService.LogoutAsync(request.RefreshToken);
        return NoContent();
    }

    // Used before approving a pending Maker-Checker request: proves the
    // logged-in approver really knows their own password, even though their
    // JWT session is already valid, as an extra confirmation step.
    [HttpPost("verify-password")]
    [Authorize]
    public async Task<ActionResult> VerifyPassword(VerifyPasswordRequest request)
    {
        var user = await _db.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.CodeUser == _currentUser.CodeUser);
        if (user == null) return Unauthorized(new { message = "Utilisateur introuvable." });

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { message = "Mot de passe incorrect." });

        return Ok(new { message = "Mot de passe vérifié." });
    }
}
