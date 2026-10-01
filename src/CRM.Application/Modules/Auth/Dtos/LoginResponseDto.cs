using System.Text.Json.Serialization;

namespace CRM.Application.Modules.Auth.Dtos;

public class LoginResponseDto : IIssuesRefreshToken
{
    public string AccessToken { get; set; } = default!;

    /// <summary>
    /// Raw refresh token for the API layer to put in the HttpOnly cookie. Never serialized —
    /// JavaScript must not be able to read it (XSS), and it must not appear in logs.
    /// </summary>
    [JsonIgnore]
    public string RefreshToken { get; set; } = default!;

    /// <summary>When the refresh token (and so the cookie) expires. Not serialized.</summary>
    [JsonIgnore]
    public DateTimeOffset RefreshTokenExpiresAt { get; set; }

    public int ExpiresIn { get; set; } // seconds
    public UserSummaryDto User { get; set; } = default!;
}

public class UserSummaryDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public List<string> Roles { get; set; } = new();
}
