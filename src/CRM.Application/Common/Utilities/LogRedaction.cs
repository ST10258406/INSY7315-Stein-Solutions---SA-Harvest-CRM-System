namespace CRM.Application.Common.Utilities;

using System.Security.Cryptography;
using System.Text;

/// <summary>Helpers for keeping personal data out of application logs.</summary>
public static class LogRedaction
{
    /// <summary>
    /// A short, stable SHA-256 fingerprint of an email address (case- and
    /// whitespace-insensitive). Lets operators correlate repeated events for the same
    /// address without the address itself ever reaching the logs.
    /// </summary>
    public static string HashEmail(string? email)
    {
        var normalised = (email ?? string.Empty).Trim().ToLowerInvariant();
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalised));
        return Convert.ToHexString(hash, 0, 8).ToLowerInvariant();
    }
}
