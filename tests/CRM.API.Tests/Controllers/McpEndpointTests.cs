namespace CRM.API.Tests.Controllers;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CRM.Application.Modules.Agent.Tools;
using CRM.Application.Modules.Auth.Commands.Login;
using CRM.Application.Modules.Auth.Dtos;
using CRM.Domain.Entities;
using CRM.Domain.Enums;
using CRM.Infrastructure.Persistence;
using Hangfire;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Client;
using TextContentBlock = ModelContextProtocol.Protocol.TextContentBlock;

/// <summary>docs/mcp-spec.md §10: /mcp is SuperAdmin-only, rate limited, audited per call, and off unless enabled.</summary>
public class McpEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Url = "/mcp";
    private const int PermitLimit = 10;

    private readonly WebApplicationFactory<Program> _baseFactory;
    private readonly WebApplicationFactory<Program> _factory;

    public McpEndpointTests(WebApplicationFactory<Program> factory)
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable("JWT_SECRET", "12345678901234567890123456789012");
        Environment.SetEnvironmentVariable("BREVO_API_KEY", "test-brevo-key");
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", "Host=localhost;Database=fake;Username=postgres;Password=password");
        Environment.SetEnvironmentVariable("Jwt__AccessTokenExpiryMinutes", "60");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "TestIssuer");
        Environment.SetEnvironmentVariable("Jwt__Audience", "TestAudience");

        _baseFactory = factory;
        _factory = CreateFactory(mcpEnabled: true);
    }

    private WebApplicationFactory<Program> CreateFactory(bool mcpEnabled)
    {
        var databaseName = $"InMemoryDbForMcp-{Guid.NewGuid():N}";
        return _baseFactory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Mcp:Enabled"] = mcpEnabled.ToString(),
                ["RateLimiting:Mcp:PermitLimit"] = PermitLimit.ToString(),
                ["RateLimiting:Mcp:WindowMinutes"] = "10",
            }));
            builder.ConfigureServices(services =>
            {
                var descriptors = services.Where(
                    d => d.ServiceType.Namespace != null &&
                         (d.ServiceType.Namespace.StartsWith("Microsoft.EntityFrameworkCore") ||
                          d.ServiceType.Namespace.StartsWith("Npgsql.EntityFrameworkCore.PostgreSQL"))).ToList();
                foreach (var d in descriptors)
                    services.Remove(d);

                services.Remove(services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<CrmDbContext>))!);
                services.Remove(services.SingleOrDefault(d => d.ServiceType == typeof(CrmDbContext))!);

                services.AddHangfire(config => config.UseInMemoryStorage());
                services.AddDbContext<CrmDbContext>(options => options.UseInMemoryDatabase(databaseName));
            });
        });
    }

    private static async Task<HttpClient> LoginAsAsync(WebApplicationFactory<Program> factory, string roleName, bool mustChangePassword = false)
    {
        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
            var role = await context.Roles.FirstOrDefaultAsync(r => r.Name == roleName)
                ?? context.Roles.Add(new Role { Id = Guid.NewGuid(), Name = roleName }).Entity;
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = $"mcp-{Guid.NewGuid():N}@example.com",
                FirstName = roleName,
                LastName = "Test",
                PasswordHash = new PasswordHasher<User>().HashPassword(null!, "TestPassword123"),
                IsActive = true,
                MustChangePassword = mustChangePassword,
            };
            user.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id, AssignedByUserId = user.Id });
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var client = factory.CreateClient();
            var login = await client.PostAsJsonAsync("/api/auth/login", new LoginCommand(user.Email, "TestPassword123"));
            Assert.Equal(HttpStatusCode.OK, login.StatusCode);
            var body = JsonSerializer.Deserialize<LoginResponseDto>(
                await login.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.AccessToken);
            return client;
        }
    }

    private static Task<McpClient> ConnectAsync(HttpClient http) =>
        McpClient.CreateAsync(new HttpClientTransport(
            new HttpClientTransportOptions { Endpoint = new Uri(http.BaseAddress!, Url), TransportMode = HttpTransportMode.StreamableHttp },
            http,
            loggerFactory: null,
            ownsHttpClient: false));

    // A raw JSON-RPC POST, for the cases where auth or routing rejects the request before MCP runs.
    private static Task<HttpResponseMessage> RawPostAsync(HttpClient http)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, Url)
        {
            Content = new StringContent("""{"jsonrpc":"2.0","id":1,"method":"tools/list"}""", Encoding.UTF8, "application/json"),
        };
        request.Headers.Accept.ParseAdd("application/json");
        request.Headers.Accept.ParseAdd("text/event-stream");
        return http.SendAsync(request);
    }

    private async Task<List<AuditLog>> McpAuditRowsAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        return await context.AuditLogs.Where(a => a.EntityType == "McpToolCall").ToListAsync();
    }

    [Fact]
    public async Task Mcp_Anonymous_Returns401()
    {
        var response = await RawPostAsync(_factory.CreateClient());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("Procurement")]
    [InlineData("Marketing")]
    public async Task Mcp_NonSuperAdmin_Returns403(string roleName)
    {
        var client = await LoginAsAsync(_factory, roleName);

        var response = await RawPostAsync(client);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Mcp_SuperAdminMustChangePassword_IsBlocked()
    {
        var client = await LoginAsAsync(_factory, "SuperAdmin", mustChangePassword: true);

        var response = await RawPostAsync(client);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("PASSWORD_CHANGE_REQUIRED", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Mcp_Disabled_Returns404()
    {
        var disabled = CreateFactory(mcpEnabled: false);
        var client = await LoginAsAsync(disabled, "SuperAdmin");

        var response = await RawPostAsync(client);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ListTools_SuperAdmin_ReturnsTheTenReadOnlyTools()
    {
        await using var mcp = await ConnectAsync(await LoginAsAsync(_factory, "SuperAdmin"));

        var tools = await mcp.ListToolsAsync();

        Assert.Equal(CrmAgentTools.Definitions.Select(d => d.Name).Order(), tools.Select(t => t.Name).Order());
        Assert.All(tools, t =>
        {
            Assert.True(t.ProtocolTool.Annotations?.ReadOnlyHint);
            Assert.False(t.ProtocolTool.Annotations?.DestructiveHint);
            Assert.False(string.IsNullOrWhiteSpace(t.Description));
        });
    }

    [Fact]
    public async Task CallTool_SuperAdmin_WritesOneAuditRow()
    {
        await using var mcp = await ConnectAsync(await LoginAsAsync(_factory, "SuperAdmin"));

        var result = await mcp.CallToolAsync("list_relationship_managers");

        Assert.NotEqual(true, result.IsError);
        var row = Assert.Single(await McpAuditRowsAsync());
        Assert.Equal(AuditAction.Viewed, row.Action);
        Assert.NotNull(row.UserId);
        Assert.NotEqual(Guid.Empty, row.EntityId);
        Assert.Null(row.OldValues);
        var newValues = JsonDocument.Parse(row.NewValues!).RootElement;
        Assert.Equal("mcp", newValues.GetProperty("channel").GetString());
        Assert.Equal("list_relationship_managers", newValues.GetProperty("tool").GetString());
    }

    [Fact]
    public async Task CallTool_UnknownDonor_ReturnsErrorContentAndIsAudited()
    {
        await using var mcp = await ConnectAsync(await LoginAsAsync(_factory, "SuperAdmin"));
        var donorId = Guid.NewGuid();

        var result = await mcp.CallToolAsync("get_donor", new Dictionary<string, object?> { ["donorId"] = donorId });

        var text = string.Join("", result.Content.OfType<TextContentBlock>().Select(c => c.Text));
        Assert.Contains("error", text);
        var row = Assert.Single(await McpAuditRowsAsync());
        Assert.Contains(donorId.ToString(), row.NewValues);
        Assert.DoesNotContain("PrimaryContact", row.NewValues); // the result is never stored
    }

    [Fact]
    public async Task Mcp_OverPerUserLimit_Returns429Envelope()
    {
        var client = await LoginAsAsync(_factory, "SuperAdmin");
        for (var i = 0; i < PermitLimit; i++)
            Assert.NotEqual(HttpStatusCode.TooManyRequests, (await RawPostAsync(client)).StatusCode);

        var over = await RawPostAsync(client);

        Assert.Equal(HttpStatusCode.TooManyRequests, over.StatusCode);
        var body = JsonDocument.Parse(await over.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal("RATE_LIMITED", body.GetProperty("code").GetString());
    }
}
