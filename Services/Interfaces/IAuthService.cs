using DailySavingV.API.DTOs;

namespace DailySavingV.API.Services.Interfaces;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(string username, string password, string? ipAddress, string? userAgent);
    Task<LoginResponse?> RefreshAsync(string refreshToken);
    Task LogoutAsync(string refreshToken);
}
