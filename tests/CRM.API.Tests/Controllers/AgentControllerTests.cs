namespace CRM.API.Tests.Controllers;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
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
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

/// <summary>US-2 (docs/agent-spec.md §3): POST /api/v1/agent/messages is SuperAdmin-only, enforced server-side.</summary>
public class AgentControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Url = "/api/v1/agent/messages";

    private readonly WebApplicationFactory<Program> _factory;
    private readonly IChatClient _chatClient = Substitute.For<IChatClient>();

    public AgentControllerTests(WebApplicationFactory<Program> factory)
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable("JWT_SECRET", "12345678901234567890123456789012");
        Environment.SetEnvironmentVariable("BREVO_API_KEY", "test-brevo-key");
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", "Host=localhost;Database=fake;Username=postgres;Password=password");
        Environment.SetEnvironmentVariable("Jwt__SigningKey", "12345678901234567890123456789012");
        Environment.SetEnvironmentVariable("Jwt__AccessTokenExpiryMinutes", "60");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "TestIssuer");
        Environment.SetEnvironmentVariable("Jwt__Audience", "TestAudience");
        Environment.SetEnvironmentVariable("Agent__Enabled", "true");
        Environment.SetEnvironmentVariable("RateLimiting__Agent__PermitLimit", "3");
        Environment.SetEnvironmentVariable("RateLimiting__Agent__WindowMinutes", "10");

        _chatClient
            .GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions?>(), Arg.Any<CancellationToken>())
            .Returns(new ChatResponse(new ChatMessage(ChatRole.Assistant, "There are 0 donors.")) { ModelId = "stub-model" });

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
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
                services.AddDbContext<CrmDbContext>(options => options.UseInMemoryDatabase("InMemoryDbForAgentTesting"));

                // Stub the LLM provider — no network calls from tests.
                services.AddSingleton(_chatClient);
            });
        });
    }

    private static object Body() => new
    {
        conversationId = Guid.NewGuid(),
        messages = new[] { new { role = "user", content = "How many donors are active?" } },
    };

    private async Task<List<AuditLog>> AgentAuditRowsAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        return await context.AuditLogs.Where(a => a.EntityType == "AgentConversation").ToListAsync();
    }

    private async Task<HttpClient> LoginAsAsync(string roleName)
    {
        var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();

        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();

        var role = new Role { Id = Guid.NewGuid(), Name = roleName };
        context.Roles.Add(role);

        const string password = "TestPassword123";
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = $"agent-{roleName.ToLower()}@example.com",
            FirstName = roleName,
            LastName = "Test",
            PasswordHash = new PasswordHasher<User>().HashPassword(null!, password),
            UserRoles = new List<UserRole>()
        };
        user.UserRoles.Add(new UserRole { RoleId = role.Id, UserId = user.Id, Role = role, User = user });
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginCommand(user.Email, password));
        var loginResult = JsonSerializer.Deserialize<LoginResponseDto>(
            await login.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResult!.AccessToken);

        return client;
    }

    [Fact]
    public async Task SendMessage_Anonymous_Returns401()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync(Url, Body());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await _chatClient.DidNotReceiveWithAnyArgs().GetResponseAsync(default!, default, default);
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("Procurement")]
    [InlineData("Marketing")]
    public async Task SendMessage_NonSuperAdmin_Returns403(string roleName)
    {
        var client = await LoginAsAsync(roleName);

        var response = await client.PostAsJsonAsync(Url, Body());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await _chatClient.DidNotReceiveWithAnyArgs().GetResponseAsync(default!, default, default);
    }

    [Fact]
    public async Task SendMessage_SuperAdmin_Returns200WithReply()
    {
        var client = await LoginAsAsync("SuperAdmin");

        var response = await client.PostAsJsonAsync(Url, Body());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal("There are 0 donors.", body.GetProperty("reply").GetString());
        Assert.Equal("stub-model", body.GetProperty("model").GetString());
    }

    [Fact]
    public async Task SendMessage_SuperAdmin_WritesOneAuditRow()
    {
        var client = await LoginAsAsync("SuperAdmin");
        var conversationId = Guid.NewGuid();

        await client.PostAsJsonAsync(Url, new
        {
            conversationId,
            messages = new[] { new { role = "user", content = "How many donors are active?" } },
        });

        var row = Assert.Single(await AgentAuditRowsAsync());
        Assert.Equal(conversationId, row.EntityId);
        Assert.Equal(AuditAction.Viewed, row.Action);
        Assert.NotNull(row.UserId);
        Assert.NotEqual(Guid.Empty, row.UserId.Value);
        Assert.Null(row.OldValues);
        var newValues = JsonDocument.Parse(row.NewValues!).RootElement;
        Assert.Equal("How many donors are active?", newValues.GetProperty("question").GetString());
        Assert.Equal("stub-model", newValues.GetProperty("model").GetString());
        Assert.DoesNotContain("There are 0 donors.", row.NewValues);
    }

    [Fact]
    public async Task SendMessage_IgnoresAuditFieldsInRequestBody()
    {
        var client = await LoginAsAsync("SuperAdmin");
        var conversationId = Guid.NewGuid();

        await client.PostAsJsonAsync(Url, new
        {
            conversationId,
            messages = new[] { new { role = "user", content = "Hi" } },
            entityId = Guid.NewGuid(),
            oldValues = new { forged = true },
            newValues = new { forged = true },
        });

        var row = Assert.Single(await AgentAuditRowsAsync());
        Assert.Equal(conversationId, row.EntityId);
        Assert.Null(row.OldValues);
        Assert.DoesNotContain("forged", row.NewValues);
    }

    [Fact]
    public async Task SendMessage_ProviderFailure_WritesNoAuditRow()
    {
        _chatClient
            .GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions?>(), Arg.Any<CancellationToken>())
            .Returns<ChatResponse>(_ => throw new HttpRequestException("provider down"));
        var client = await LoginAsAsync("SuperAdmin");

        var response = await client.PostAsJsonAsync(Url, Body());

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Empty(await AgentAuditRowsAsync());
    }

    [Fact]
    public async Task SendMessage_Forbidden_WritesNoAuditRow()
    {
        var client = await LoginAsAsync("Admin");

        await client.PostAsJsonAsync(Url, Body());

        Assert.Empty(await AgentAuditRowsAsync());
    }

    [Fact]
    public async Task SendMessage_ProviderFailure_Returns503Envelope()
    {
        _chatClient
            .GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions?>(), Arg.Any<CancellationToken>())
            .Returns<ChatResponse>(_ => throw new HttpRequestException("Groq 429: rate limit reached for org_123"));
        var client = await LoginAsAsync("SuperAdmin");

        var response = await client.PostAsJsonAsync(Url, Body());

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var raw = await response.Content.ReadAsStringAsync();
        var body = JsonDocument.Parse(raw).RootElement;
        Assert.Equal(503, body.GetProperty("status").GetInt32());
        Assert.Equal("AGENT_UNAVAILABLE", body.GetProperty("code").GetString());
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("traceId").GetString()));
        Assert.DoesNotContain("org_123", raw);
    }

    [Fact]
    public async Task SendMessage_OverPerUserLimit_Returns429Envelope()
    {
        // RateLimiting:Agent:PermitLimit is overridden to 3 in this class's constructor.
        var client = await LoginAsAsync("SuperAdmin");
        for (var i = 0; i < 3; i++)
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync(Url, Body())).StatusCode);

        var fourth = await client.PostAsJsonAsync(Url, Body());

        Assert.Equal(HttpStatusCode.TooManyRequests, fourth.StatusCode);
        var body = JsonDocument.Parse(await fourth.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal("RATE_LIMITED", body.GetProperty("code").GetString());
    }
}
