namespace CRM.Infrastructure.Persistence.Seeders;

using System.Net.Mail;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;

/// <summary>
/// Startup safety net for the seeded SuperAdmin. Outside Development the application
/// refuses to boot if ADMIN_DEFAULT_PASSWORD is a well-known example value or breaks the
/// password policy, or if ADMIN_EMAIL is missing or not a real address. The deny-list is
/// a last line of defence, not the policy — production secrets should be generated.
/// </summary>
public static class AdminSeedGuard
{
    public const string DevelopmentDefaultEmail = "admin@crm.local";

    private static readonly string[] DeniedPasswords =
    [
        "ChangeMe123!", "Password123!", "Password1!", "Admin123!", "Admin@123",
        "Welcome123!", "Passw0rd!", "P@ssw0rd", "P@ssword1", "Qwerty123!"
    ];

    /// <summary>Returns the admin email to seed: configured, or the dev default in Development.</summary>
    public static string ResolveAdminEmail(IConfiguration configuration, bool isDevelopment)
    {
        var email = configuration["ADMIN_EMAIL"];
        if (string.IsNullOrWhiteSpace(email))
            return isDevelopment
                ? DevelopmentDefaultEmail
                : throw new InvalidOperationException(
                    "ADMIN_EMAIL is not set. Provide a real, reachable email address for the seeded SuperAdmin " +
                    "(the password-reset flow must be able to reach it).");

        email = email.Trim();
        if (!isDevelopment && !IsDeliverableLooking(email))
            throw new InvalidOperationException(
                $"ADMIN_EMAIL '{email}' is not a valid, routable email address.");

        return email;
    }

    /// <summary>Throws if the configured admin password is unsafe to run with outside Development.</summary>
    public static void EnsureSafe(IConfiguration configuration, bool isDevelopment)
    {
        if (isDevelopment) return;

        // Checked on every boot, not only when a fresh admin is created: the same address is
        // used to repair a legacy bootstrap admin on upgrade (see AdminUserSeeder).
        _ = ResolveAdminEmail(configuration, isDevelopment: false);

        var password = configuration["ADMIN_DEFAULT_PASSWORD"];
        if (string.IsNullOrWhiteSpace(password)) return; // absence is handled by the seeder when it is actually needed

        var problem = FindProblem(password);
        if (problem is not null)
            throw new InvalidOperationException(
                $"ADMIN_DEFAULT_PASSWORD is not acceptable outside Development: {problem} " +
                "Set a strong, unique value (generate one and store it in Key Vault).");
    }

    public static string? FindProblem(string password)
    {
        if (DeniedPasswords.Contains(password, StringComparer.OrdinalIgnoreCase)
            || password.Contains("changeme", StringComparison.OrdinalIgnoreCase))
            return "it is a known example/default password.";
        if (password.Length < 12) return "it must be at least 12 characters.";
        if (!Regex.IsMatch(password, "[0-9]")) return "it must contain a digit.";
        if (!Regex.IsMatch(password, "[A-Z]")) return "it must contain an uppercase letter.";
        if (!Regex.IsMatch(password, "[^a-zA-Z0-9]")) return "it must contain a special character.";
        return null;
    }

    private static bool IsDeliverableLooking(string email)
    {
        if (!MailAddress.TryCreate(email, out var address) || address.Address != email) return false;
        var host = address.Host.ToLowerInvariant();
        return host.Contains('.')
            && !host.EndsWith(".local") && !host.EndsWith(".test") && !host.EndsWith(".invalid")
            && !host.EndsWith(".example") && !host.EndsWith(".localhost")
            && host != "example.com";
    }
}
