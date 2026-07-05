namespace DailySavingV.API.DTOs;

public record LoginRequest(string Username, string Password);

public record LoginResponse(
    string CodeUser,
    string Username,
    string RoleCode,
    int? AgenceID,
    string? AgenceNom,
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAt
);

public record RefreshRequest(string RefreshToken);
