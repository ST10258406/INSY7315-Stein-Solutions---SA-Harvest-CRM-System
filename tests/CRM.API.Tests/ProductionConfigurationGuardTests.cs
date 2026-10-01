using CRM.API.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace CRM.API.Tests;

public class ProductionConfigurationGuardTests
{
    private const string ValidSecret = "12345678901234567890123456789012";

    private sealed class FakeEnvironment(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "CRM.API";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private static IConfiguration Config(
        string[]? origins = null, string? baseUrl = "https://crm.saharvest.org", string? jwtSecret = ValidSecret)
    {
        var values = new Dictionary<string, string?>
        {
            ["Frontend:BaseUrl"] = baseUrl,
            ["JWT_SECRET"] = jwtSecret
        };
        origins ??= ["https://crm.saharvest.org"];
        for (var i = 0; i < origins.Length; i++)
            values[$"Cors:AllowedOrigins:{i}"] = origins[i];

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    private static InvalidOperationException Fails(IConfiguration config, string env = "Production") =>
        Assert.Throws<InvalidOperationException>(() =>
            ProductionConfigurationGuard.Validate(config, new FakeEnvironment(env)));

    [Fact]
    public void Validate_ValidProductionConfig_DoesNotThrow()
        => ProductionConfigurationGuard.Validate(Config(), new FakeEnvironment("Production"));

    [Fact]
    public void Validate_Development_AllowsLocalhostAndShortSecret()
        => ProductionConfigurationGuard.Validate(
            Config(["http://localhost:3000"], "http://localhost:3000", "short"), new FakeEnvironment("Development"));

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public void Validate_EmptyOrigins_Throws(string env)
        => Assert.Contains("Cors:AllowedOrigins is empty", Fails(Config(origins: []), env).Message);

    [Theory]
    [InlineData("http://localhost:3000")]
    [InlineData("https://localhost:3000")]
    [InlineData("https://127.0.0.1")]
    public void Validate_LocalhostOrigin_Throws(string origin)
        => Assert.Contains("localhost", Fails(Config(origins: [origin])).Message);

    [Fact]
    public void Validate_WildcardOrigin_Throws()
        => Assert.Contains("wildcard", Fails(Config(origins: ["*"])).Message);

    [Fact]
    public void Validate_HttpOrigin_Throws()
        => Assert.Contains("https://", Fails(Config(origins: ["http://crm.saharvest.org"])).Message);

    [Fact]
    public void Validate_OriginWithPath_Throws()
        => Assert.Contains("origin only", Fails(Config(origins: ["https://crm.saharvest.org/app"])).Message);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_MissingFrontendBaseUrl_Throws(string? baseUrl)
        => Assert.Contains("Frontend:BaseUrl is empty", Fails(Config(baseUrl: baseUrl)).Message);

    [Theory]
    [InlineData("http://localhost:3000")]
    [InlineData("https://127.0.0.1:3000")]
    public void Validate_LocalhostFrontendBaseUrl_Throws(string baseUrl)
        => Assert.Contains("Frontend:BaseUrl", Fails(Config(baseUrl: baseUrl)).Message);

    [Fact]
    public void Validate_ShortJwtSecret_ThrowsWithoutEchoingIt()
    {
        var message = Fails(Config(jwtSecret: "tensecret!")).Message;

        Assert.Contains("JWT_SECRET is shorter than 32 bytes", message);
        Assert.DoesNotContain("tensecret!", message);
    }

    [Fact]
    public void Validate_ReportsEveryProblemAtOnce()
    {
        var message = Fails(Config(origins: [], baseUrl: "", jwtSecret: "short")).Message;

        Assert.Contains("Cors:AllowedOrigins", message);
        Assert.Contains("Frontend:BaseUrl", message);
        Assert.Contains("JWT_SECRET", message);
    }

    [Fact]
    public void GetAllowedOrigins_TrimsTrailingSlashesAndDropsBlanks()
    {
        var origins = ProductionConfigurationGuard.GetAllowedOrigins(
            Config(origins: ["https://crm.saharvest.org/", " ", "https://staging.saharvest.org"]));

        Assert.Equal(["https://crm.saharvest.org", "https://staging.saharvest.org"], origins);
    }
}
