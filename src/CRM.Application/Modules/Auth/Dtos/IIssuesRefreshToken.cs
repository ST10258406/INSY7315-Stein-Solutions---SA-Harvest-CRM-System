namespace CRM.Application.Modules.Auth.Dtos;

/// <summary>
/// A command result that carries a freshly issued refresh token. The API layer's
/// IssuesRefreshCookie filter moves it into the HttpOnly cookie; the properties are
/// [JsonIgnore]d so the token never appears in a response body.
/// </summary>
public interface IIssuesRefreshToken
{
    string RefreshToken { get; }
    DateTimeOffset RefreshTokenExpiresAt { get; }
}
