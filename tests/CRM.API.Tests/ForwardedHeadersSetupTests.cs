using System.Net;
using CRM.API.Extensions;
using Microsoft.Extensions.Configuration;

namespace CRM.API.Tests;

public class ForwardedHeadersSetupTests
{
    private static IConfiguration Config(params (string Key, string Value)[] values)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(v => new KeyValuePair<string, string?>(v.Key, v.Value)))
            .Build();

    [Fact]
    public void Create_ByDefault_TrustsLoopbackOnly()
    {
        var options = ForwardedHeadersSetup.Create(Config());

        Assert.True(options.KnownProxies.Count + options.KnownIPNetworks.Count > 0);
        Assert.All(options.KnownProxies, p => Assert.True(IPAddress.IsLoopback(p)));
        Assert.Equal(1, options.ForwardLimit);
    }

    [Fact]
    public void Create_OnAppService_TrustsThePlatformFrontEndWithOneHop()
    {
        var options = ForwardedHeadersSetup.Create(Config(("WEBSITE_SITE_NAME", "app-crm-api-prod")));

        Assert.Empty(options.KnownProxies);
        Assert.Empty(options.KnownIPNetworks);
        Assert.Equal(1, options.ForwardLimit);
    }

    [Fact]
    public void Create_WithConfiguredProxies_TrustsExactlyThose()
    {
        var options = ForwardedHeadersSetup.Create(Config(
            ("ForwardedHeaders:KnownProxies:0", "10.0.0.4"),
            ("ForwardedHeaders:KnownNetworks:0", "10.1.0.0/16")));

        Assert.Equal([IPAddress.Parse("10.0.0.4")], options.KnownProxies);
        Assert.Single(options.KnownIPNetworks);
    }
}
