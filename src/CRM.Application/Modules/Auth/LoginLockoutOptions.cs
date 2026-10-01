namespace CRM.Application.Modules.Auth;

/// <summary>
/// Per-account brute-force throttle for POST /auth/login (security review F-02), bound
/// from the "Auth:Lockout" configuration section. The per-IP rate limit stops one source
/// spraying many accounts; this stops many sources hammering one account.
///
/// The lock is short and automatic on purpose: lockout is also a lever an attacker can
/// pull to lock a real user out, so it must expire by itself and an Admin can clear it
/// early (POST /api/v1/users/{id}/unlock).
/// </summary>
public class LoginLockoutOptions
{
    public const string SectionName = "Auth:Lockout";

    /// <summary>Consecutive failures that trigger a lock.</summary>
    public int MaxFailedAttempts { get; set; } = 5;

    /// <summary>How long a triggered lock lasts.</summary>
    public int LockoutMinutes { get; set; } = 15;
}
