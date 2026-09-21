namespace CRM.Application.Common.Utilities;

using System.Security.Cryptography;

/// <summary>
/// Generates a one-time temporary password for newly created staff accounts
/// (CreateUserCommandHandler). Uses a CSPRNG, never Random/Math.Random — the
/// plaintext is shown to the admin exactly once and is never logged or stored.
/// </summary>
public static class TemporaryPasswordGenerator
{
    // Excludes visually ambiguous characters (0/O, 1/l/I) while still satisfying the
    // password policy: at least one digit, one uppercase letter, one special character.
    private const string Uppercase = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string Lowercase = "abcdefghjkmnpqrstuvwxyz";
    private const string Digits = "23456789";
    private const string Special = "!#%&*+?@";
    private const string AllChars = Uppercase + Lowercase + Digits + Special;

    public static string Generate(int length = 12)
    {
        if (length < 8)
            throw new ArgumentOutOfRangeException(nameof(length), "Password must be at least 8 characters.");

        Span<char> password = stackalloc char[length];

        // Guarantee the password policy is met, then fill the rest randomly.
        password[0] = Uppercase[RandomNumberGenerator.GetInt32(Uppercase.Length)];
        password[1] = Digits[RandomNumberGenerator.GetInt32(Digits.Length)];
        password[2] = Special[RandomNumberGenerator.GetInt32(Special.Length)];
        password[3] = Lowercase[RandomNumberGenerator.GetInt32(Lowercase.Length)];

        for (var i = 4; i < length; i++)
            password[i] = AllChars[RandomNumberGenerator.GetInt32(AllChars.Length)];

        // Shuffle so the guaranteed characters aren't always in the same positions.
        for (var i = length - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (password[i], password[j]) = (password[j], password[i]);
        }

        return new string(password);
    }
}
