namespace CRM.API.Authentication;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

/// <summary>Settings for the refresh-token cookie, bound from "Auth:RefreshCookie".</summary>
public class RefreshCookieOptions
{
    public const string SectionName = "Auth:RefreshCookie";

    public string Name { get; set; } = "crm_refresh";

    /// <summary>
    /// "Strict" (default) when the SPA and API share a registrable domain (e.g.
    /// crm.saharvest.org + api.saharvest.org). "None" is only for split default Azure
    /// hostnames (*.azurestaticapps.net + *.azurewebsites.net) — and Safari blocks
    /// cross-site cookies regardless, so those users won't stay signed in across reloads.
    /// </summary>
    public SameSiteMode SameSite { get; set; } = SameSiteMode.Strict;

    /// <summary>HTTPS-only. Only ever false in Development (plain http://localhost); ProductionConfigurationGuard enforces it elsewhere.</summary>
    public bool Secure { get; set; } = true;
}

/// <summary>
/// Reads and writes the HttpOnly refresh-token cookie. The refresh token never reaches
/// JavaScript: the SPA keeps only the short-lived access token in memory and, on page load,
/// calls POST /api/auth/refresh — the browser attaches this cookie — to restore the session.
/// Scoped to /api/auth so it is not sent with any other API request.
/// </summary>
public class RefreshTokenCookie
{
    public const string Path = "/api/auth";

    private readonly RefreshCookieOptions _options;

    public RefreshTokenCookie(IOptions<RefreshCookieOptions> options) => _options = options.Value;

    public string? Read(HttpRequest request)
        => request.Cookies.TryGetValue(_options.Name, out var value) && !string.IsNullOrEmpty(value) ? value : null;

    public void Write(HttpResponse response, string refreshToken, DateTimeOffset expiresAt)
        => response.Cookies.Append(_options.Name, refreshToken, BuildOptions(expiresAt));

    public void Delete(HttpResponse response)
        => response.Cookies.Delete(_options.Name, BuildOptions(expires: null));

    private CookieOptions BuildOptions(DateTimeOffset? expires) => new()
    {
        HttpOnly = true,
        Secure = _options.Secure,
        SameSite = _options.SameSite,
        Path = Path,
        Expires = expires,
        IsEssential = true
    };
}
