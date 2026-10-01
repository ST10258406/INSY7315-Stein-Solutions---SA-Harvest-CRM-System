namespace CRM.Domain.Entities;

/// <summary>
/// One link in a refresh-token chain. Only the SHA-256 hash of the token is stored — the
/// raw value goes to the client once (in an HttpOnly cookie) and is never persisted, so a
/// leaked table or backup can't be used to mint sessions.
///
/// Every refresh rotates the token: the presented one is revoked and points at its
/// replacement via <see cref="ReplacedByTokenHash"/>. All tokens descended from one login
/// share a <see cref="FamilyId"/>, so presenting an already-rotated token (a sign it was
/// copied) can revoke the whole chain.
/// </summary>
public class RefreshToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = default!;

    /// <summary>Lowercase hex SHA-256 of the raw token.</summary>
    public string TokenHash { get; set; } = default!;

    /// <summary>Shared by every token rotated from the same login.</summary>
    public Guid FamilyId { get; set; }

    /// <summary>Set when this token was rotated (as opposed to revoked by logout/reset/reuse).</summary>
    public string? ReplacedByTokenHash { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Marks the token unusable. Pass <paramref name="replacedByTokenHash"/> only when the
    /// revoke is a rotation — that's what distinguishes "superseded" from "killed" when the
    /// token is presented again.
    /// </summary>
    public void Revoke(DateTimeOffset at, string? replacedByTokenHash = null)
    {
        IsRevoked = true;
        RevokedAt = at;
        ReplacedByTokenHash = replacedByTokenHash;
    }
}
