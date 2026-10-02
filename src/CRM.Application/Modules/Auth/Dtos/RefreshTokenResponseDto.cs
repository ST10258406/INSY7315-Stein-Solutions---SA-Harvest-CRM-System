using System.Text.Json.Serialization;

namespace CRM.Application.Modules.Auth.Dtos;

public class RefreshTokenResponseDto : IIssuesRefreshToken
{
    public string AccessToken { get; set; } = default!;
    public int ExpiresIn { get; set; } // seconds

    /// <summary>Current user — lets the SPA restore its session on page load from the cookie alone.</summary>
    public UserSummaryDto User { get; set; } = default!;

    /// <summary>The rotated refresh token, for the API layer's HttpOnly cookie. Never serialized.</summary>
    [JsonIgnore]
    public string RefreshToken { get; set; } = default!;

    /// <summary>When the rotated refresh token expires. Not serialized.</summary>
    [JsonIgnore]
    public DateTimeOffset RefreshTokenExpiresAt { get; set; }
}
