namespace DailySavingV.API.DTOs;

public record LoginRequest(string Username, string Password);

public record LoginResponse(
    string CodeUser,
    string Username,
    string RoleCode,
    int? AgenceID,
    string? AgenceNom,
    string? AgenceCode,
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAt,
    bool MustChangePassword,
    string? PhotoUrl,
    string? RoleType
);

public record RefreshRequest(string RefreshToken);
public record VerifyPasswordRequest(string Password);
