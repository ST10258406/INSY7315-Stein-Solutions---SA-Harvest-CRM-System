namespace CRM.API.Tests;

using System.Net.Http.Json;
using System.Text.Json;
using CRM.Application.Modules.Auth.Commands.Login;
using CRM.Application.Modules.Auth.Dtos;
using Microsoft.AspNetCore.Mvc.Testing;

/// <summary>
/// The refresh token travels only in the HttpOnly, Secure "crm_refresh" cookie (path
/// /api/auth). These helpers let integration tests drive that the way a browser would.
/// </summary>
public static class AuthCookieTestHelpers
{
    public const string CookieName = "crm_refresh";
    public const string CsrfHeader = "X-Requested-With";
    public const string CsrfValue = "XMLHttpRequest";

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// An HTTPS client that, by default, stores and replays cookies like a browser tab
    /// (the cookie is Secure, so it is only sent over https).
    /// </summary>
    public static HttpClient CreateBrowserClient<T>(this WebApplicationFactory<T> factory, bool handleCookies = true) where T : class
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = handleCookies
        });
        client.DefaultRequestHeaders.Add(CsrfHeader, CsrfValue);
        return client;
    }

    /// <summary>The refresh-cookie value set by <paramref name="response"/>, or null if none.</summary>
    public static string? GetRefreshCookie(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var cookies))
            return null;

        var cookie = cookies.FirstOrDefault(c => c.StartsWith(CookieName + "=", StringComparison.Ordinal));
        if (cookie is null)
            return null;

        var value = cookie[(CookieName.Length + 1)..].Split(';')[0];
        return string.IsNullOrEmpty(value) ? null : Uri.UnescapeDataString(value);
    }

    /// <summary>The raw Set-Cookie header for the refresh cookie (to inspect its attributes).</summary>
    public static string? GetRefreshSetCookieHeader(HttpResponseMessage response)
        => response.Headers.TryGetValues("Set-Cookie", out var cookies)
            ? cookies.FirstOrDefault(c => c.StartsWith(CookieName + "=", StringComparison.Ordinal))
            : null;

    /// <summary>POST /api/auth/refresh presenting exactly <paramref name="refreshToken"/> as the cookie.</summary>
    public static Task<HttpResponseMessage> RefreshWithCookieAsync<T>(this WebApplicationFactory<T> factory, string? refreshToken) where T : class
    {
        var client = factory.CreateBrowserClient(handleCookies: false);
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        if (refreshToken is not null)
            request.Headers.Add("Cookie", $"{CookieName}={Uri.EscapeDataString(refreshToken)}");
        return client.SendAsync(request);
    }

    /// <summary>Logs in; returns the body and the raw refresh token from the Set-Cookie header.</summary>
    public static async Task<(LoginResponseDto Login, string RefreshToken)> LoginForCookieAsync<T>(
        this WebApplicationFactory<T> factory, string email, string password) where T : class
    {
        var response = await factory.CreateBrowserClient(handleCookies: false)
            .PostAsJsonAsync("/api/auth/login", new LoginCommand(email, password));
        response.EnsureSuccessStatusCode();
        var login = JsonSerializer.Deserialize<LoginResponseDto>(await response.Content.ReadAsStringAsync(), JsonOptions)!;
        return (login, GetRefreshCookie(response)!);
    }
}
