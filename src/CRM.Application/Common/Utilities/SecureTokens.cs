namespace CRM.Application.Common.Utilities;

using System.Security.Cryptography;
using System.Text;

/// <summary>
/// Hashing for high-entropy bearer secrets (refresh tokens, password-reset tokens).
/// These are already 256+ bits of randomness, so a fast SHA-256 is correct here — the slow,
/// salted hashing used for passwords only matters for low-entropy, guessable inputs.
/// </summary>
public static class SecureTokens
{
    /// <summary>Lowercase hex SHA-256 of <paramref name="token"/>.</summary>
    public static string Hash(string token)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();

    /// <summary>
    /// Constant-time check that <paramref name="token"/> hashes to <paramref name="storedHash"/>.
    /// Comparing fixed-length hashes with FixedTimeEquals means response timing reveals
    /// nothing about how much of a guess matched.
    /// </summary>
    public static bool Matches(string? token, string? storedHash)
    {
        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(storedHash))
            return false;

        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(Hash(token)),
            Encoding.ASCII.GetBytes(storedHash));
    }
}
