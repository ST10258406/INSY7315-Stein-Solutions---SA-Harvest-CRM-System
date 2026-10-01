namespace CRM.Application.Modules.Auth;

/// <summary>Refresh-token lifetime and rotation settings, bound from "Auth:RefreshToken".</summary>
public class RefreshTokenOptions
{
    public const string SectionName = "Auth:RefreshToken";

    /// <summary>
    /// Absolute session length from login. Rotation does not extend it — every token in a
    /// family keeps the family's original expiry, so a session always ends after this long.
    /// </summary>
    public int LifetimeDays { get; set; } = 7;

    /// <summary>
    /// How long after rotation the *previous* token is still accepted once more. Covers the
    /// benign race where two tabs reload at the same moment and both present the same
    /// cookie; outside this window, a rotated token coming back means it was copied, and the
    /// whole family is revoked.
    /// </summary>
    public int ReuseGraceSeconds { get; set; } = 20;
}
