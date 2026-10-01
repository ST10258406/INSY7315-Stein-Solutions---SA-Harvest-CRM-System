namespace CRM.Application.Interfaces;

using CRM.Domain.Entities;

public interface IJwtTokenService
{
    /// <summary>
    /// Issues an access token bound to one session (<paramref name="sessionId"/> = the
    /// refresh-token family id, carried as the "sid" claim). The JWT pipeline rejects the
    /// token as soon as that session is revoked, so logout, reset, email change and reuse
    /// detection end access immediately rather than at the token's expiry.
    /// </summary>
    string GenerateAccessToken(User user, Guid sessionId);
    string GenerateRefreshToken();
    int AccessTokenExpirySeconds { get; }
}
