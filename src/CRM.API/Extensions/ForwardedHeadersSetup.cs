namespace CRM.API.Extensions;

using System.Net;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;

/// <summary>
/// Decides whose X-Forwarded-For to believe. This matters for security: every per-IP rate
/// limit (login brute-force protection included) partitions on the address this produces,
/// so trusting the header from an arbitrary client would let it pick a fresh "IP" per request.
///
/// Three modes, most specific first:
/// <list type="number">
///   <item><b>Azure App Service</b> (WEBSITE_SITE_NAME is set by the platform): the front end
///   is the only ingress and its IPs aren't publishable, so trust any peer but read only the
///   last hop (ForwardLimit = 1) — the entry the front end itself appended. Microsoft's
///   documented App Service pattern. Must be revisited if another proxy layer (Front Door,
///   App Gateway, CDN) is added in front.</item>
///   <item><b>Configured proxies</b> (ForwardedHeaders:KnownProxies / :KnownNetworks):
///   trust exactly those peers, e.g. when hosted behind a known load balancer.</item>
///   <item><b>Anything else</b> (local Docker, any directly reachable host): ASP.NET's
///   default loopback-only trust, so a direct client's header is ignored and its real
///   connection address is used.</item>
/// </list>
/// </summary>
public static class ForwardedHeadersSetup
{
    public const string KnownProxiesKey = "ForwardedHeaders:KnownProxies";
    public const string KnownNetworksKey = "ForwardedHeaders:KnownNetworks";
    public const string AppServiceSiteNameVariable = "WEBSITE_SITE_NAME";

    public static bool IsAzureAppService(IConfiguration configuration)
        => !string.IsNullOrWhiteSpace(configuration[AppServiceSiteNameVariable]);

    public static ForwardedHeadersOptions Create(IConfiguration configuration)
    {
        var options = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
            ForwardLimit = 1
        };

        if (IsAzureAppService(configuration))
        {
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
            return options;
        }

        var proxies = configuration.GetSection(KnownProxiesKey).Get<string[]>() ?? [];
        var networks = configuration.GetSection(KnownNetworksKey).Get<string[]>() ?? [];
        if (proxies.Length == 0 && networks.Length == 0)
            return options; // loopback-only defaults

        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
        foreach (var proxy in proxies)
            options.KnownProxies.Add(IPAddress.Parse(proxy));
        foreach (var network in networks)
            options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));

        return options;
    }
}
