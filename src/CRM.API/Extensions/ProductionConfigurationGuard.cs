namespace CRM.API.Extensions;

using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

/// <summary>
/// Fail-fast startup validation for settings that are harmless locally but break or
/// weaken a real deployment (security review F-03 / F-20): the CORS allow-list, the
/// frontend base URL used in emailed links, and the JWT signing secret.
///
/// Development is exempt so localhost keeps working; every other environment
/// (Staging, Production, Testing) must supply real values. Messages name the setting
/// but never echo a secret value.
/// </summary>
public static class ProductionConfigurationGuard
{
    public const string AllowedOriginsKey = "Cors:AllowedOrigins";
    public const string FrontendBaseUrlKey = "Frontend:BaseUrl";
    public const string JwtSecretKey = "JWT_SECRET";
    public const string RefreshCookieSecureKey = "Auth:RefreshCookie:Secure";

    /// <summary>HS256 needs a key at least as long as its 256-bit output (RFC 7518 §3.2).</summary>
    public const int MinimumJwtSecretBytes = 32;

    private static readonly string[] LoopbackHosts = ["localhost", "127.0.0.1", "[::1]", "::1", "0.0.0.0"];

    /// <summary>
    /// The configured CORS origins, normalised to scheme://host[:port] with no trailing
    /// slash — browsers send Origin in exactly that form, so "https://x.org/" in config
    /// would otherwise silently never match.
    /// </summary>
    public static string[] GetAllowedOrigins(IConfiguration configuration) =>
        (configuration.GetSection(AllowedOriginsKey).Get<string[]>() ?? [])
            .Where(o => !string.IsNullOrWhiteSpace(o))
            .Select(o => o.Trim().TrimEnd('/'))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

    /// <summary>Throws <see cref="InvalidOperationException"/> listing every problem found, outside Development.</summary>
    public static void Validate(IConfiguration configuration, IHostEnvironment environment)
    {
        if (environment.IsDevelopment())
            return;

        var errors = new List<string>();

        var origins = GetAllowedOrigins(configuration);
        if (origins.Length == 0)
            errors.Add($"{AllowedOriginsKey} is empty. Set at least one exact frontend origin (e.g. Cors__AllowedOrigins__0=https://crm.example.org).");

        foreach (var origin in origins)
        {
            if (origin.Contains('*'))
                errors.Add($"{AllowedOriginsKey} contains a wildcard ('{origin}'). Credentials are allowed, so only exact origins are valid.");
            else if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
                errors.Add($"{AllowedOriginsKey} entry '{origin}' must be an absolute https:// origin.");
            else if (uri.AbsolutePath != "/" || !string.IsNullOrEmpty(uri.Query))
                errors.Add($"{AllowedOriginsKey} entry '{origin}' must be an origin only (scheme, host and optional port — no path or query).");
            else if (IsLoopback(uri))
                errors.Add($"{AllowedOriginsKey} entry '{origin}' points at localhost.");
        }

        var baseUrl = configuration[FrontendBaseUrlKey];
        if (string.IsNullOrWhiteSpace(baseUrl))
            errors.Add($"{FrontendBaseUrlKey} is empty. Password-reset and invite links are built from it.");
        else if (!Uri.TryCreate(baseUrl.Trim(), UriKind.Absolute, out var baseUri) || baseUri.Scheme != Uri.UriSchemeHttps)
            errors.Add($"{FrontendBaseUrlKey} ('{baseUrl}') must be an absolute https:// URL.");
        else if (IsLoopback(baseUri))
            errors.Add($"{FrontendBaseUrlKey} ('{baseUrl}') points at localhost.");

        var jwtSecret = configuration[JwtSecretKey];
        if (string.IsNullOrEmpty(jwtSecret))
            errors.Add($"{JwtSecretKey} is not set.");
        else if (Encoding.UTF8.GetByteCount(jwtSecret) < MinimumJwtSecretBytes)
            errors.Add($"{JwtSecretKey} is shorter than {MinimumJwtSecretBytes} bytes (256 bits).");

        // The refresh cookie carries a 7-day credential: it must never travel over plain HTTP,
        // and browsers reject SameSite=None without Secure anyway.
        if (configuration.GetValue<bool?>(RefreshCookieSecureKey) == false)
            errors.Add($"{RefreshCookieSecureKey} is false. The refresh-token cookie must be Secure outside Development.");

        if (errors.Count > 0)
            throw new InvalidOperationException(
                $"Invalid configuration for the '{environment.EnvironmentName}' environment:{Environment.NewLine}- " +
                string.Join($"{Environment.NewLine}- ", errors));
    }

    private static bool IsLoopback(Uri uri) =>
        uri.IsLoopback || LoopbackHosts.Contains(uri.Host, StringComparer.OrdinalIgnoreCase);
}
