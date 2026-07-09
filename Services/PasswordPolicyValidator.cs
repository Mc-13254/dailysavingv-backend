using System.Text.RegularExpressions;
using DailySavingV.API.Entities;

namespace DailySavingV.API.Services;

/// <summary>
/// Pure validation logic against a PasswordPolicy — kept separate from any
/// controller/DbContext so it's trivially unit-testable.
/// </summary>
public static class PasswordPolicyValidator
{
    public static List<string> Validate(string password, PasswordPolicy policy, string? username = null)
    {
        var errors = new List<string>();

        if (password.Length < policy.MinimumLength)
            errors.Add($"Le mot de passe doit contenir au moins {policy.MinimumLength} caractères.");
        if (password.Length > policy.MaximumLength)
            errors.Add($"Le mot de passe ne doit pas dépasser {policy.MaximumLength} caractères.");
        if (policy.RequireUppercase && !password.Any(char.IsUpper))
            errors.Add("Le mot de passe doit contenir au moins une majuscule.");
        if (policy.RequireLowercase && !password.Any(char.IsLower))
            errors.Add("Le mot de passe doit contenir au moins une minuscule.");
        if (policy.RequireNumber && !password.Any(char.IsDigit))
            errors.Add("Le mot de passe doit contenir au moins un chiffre.");
        if (policy.RequireSpecialCharacter && !Regex.IsMatch(password, @"[^a-zA-Z0-9]"))
            errors.Add("Le mot de passe doit contenir au moins un caractère spécial.");
        if (!string.IsNullOrWhiteSpace(username) && password.Equals(username, StringComparison.OrdinalIgnoreCase))
            errors.Add("Le mot de passe ne peut pas être identique au nom d'utilisateur.");

        return errors;
    }

    /// Very rough 0-100 strength estimate used for the UI's colored meter.
    public static int Score(string password)
    {
        var score = 0;
        if (password.Length >= 8) score += 20;
        if (password.Length >= 12) score += 15;
        if (password.Any(char.IsUpper)) score += 15;
        if (password.Any(char.IsLower)) score += 15;
        if (password.Any(char.IsDigit)) score += 15;
        if (Regex.IsMatch(password, @"[^a-zA-Z0-9]")) score += 20;
        return Math.Min(100, score);
    }
}
