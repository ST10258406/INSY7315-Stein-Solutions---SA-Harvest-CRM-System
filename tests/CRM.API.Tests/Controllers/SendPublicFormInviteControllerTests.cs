namespace CRM.API.Tests.Controllers;

using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Auth.Commands.Login;
using CRM.Application.Modules.Auth.Dtos;
using CRM.Domain.Entities;
using CRM.Domain.Enums;
using CRM.Infrastructure.Persistence;
using CRM.Infrastructure.Services;
using Hangfire;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Invite-a-prospect-to-the-public-form (POST /api/v1/donors/public-form-invite).
/// Brevo's real HTTP call is faked, same pattern as SendDonorEmailControllerTests.
/// </summary>
public class SendPublicFormInviteControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly ConfigurableBrevoHandler _brevoHandler = new();

    public SendPublicFormInviteControllerTests(WebApplicationFactory<Program> factory)
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable("JWT_SECRET", "12345678901234567890123456789012");
        Environment.SetEnvironmentVariable("BREVO_API_KEY", "test-brevo-key");
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", "Host=localhost;Database=fake;Username=postgres;Password=password");
        Environment.SetEnvironmentVariable("Jwt__SigningKey", "12345678901234567890123456789012");
        Environment.SetEnvironmentVariable("Jwt__AccessTokenExpiryMinutes", "60");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "TestIssuer");
        Environment.SetEnvironmentVariable("Jwt__Audience", "TestAudience");
        Environment.SetEnvironmentVariable("Frontend__BaseUrl", "https://app.example.test");

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
                services.AddDbContext<CrmDbContext>(options => options.UseInMemoryDatabase("InMemoryDbForPublicFormInviteTesting"));

                services.AddHttpClient<IEmailService, EmailService>()
                    .ConfigurePrimaryHttpMessageHandler(() => _brevoHandler);
            });
        });
    }

    private class ConfigurableBrevoHandler : HttpMessageHandler
    {
        public Func<HttpRequestMessage, HttpResponseMessage> Respond { get; set; } = _ => Success();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(Respond(request));

        public static HttpResponseMessage Success() => new(HttpStatusCode.Created)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new { messageId = "fake-brevo-message-id" }),
                Encoding.UTF8,
                "application/json")
        };

        public static HttpResponseMessage Failure() => new(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new { message = "Key not found" }),
                Encoding.UTF8,
                "application/json")
        };
    }

    private async Task<HttpClient> CreateAuthenticatedClient(string roleName)
    {
        var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();

        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();

        var role = new Role { Id = Guid.NewGuid(), Name = roleName };
        context.Roles.Add(role);

        const string password = "TestPassword123";
        var hasher = new PasswordHasher<User>();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = $"test-{roleName.ToLower()}-{Guid.NewGuid()}@example.com",
            FirstName = roleName,
            LastName = "Test",
            PasswordHash = hasher.HashPassword(null!, password),
            UserRoles = new List<UserRole>()
        };
        user.UserRoles.Add(new UserRole { RoleId = role.Id, UserId = user.Id, Role = role, User = user });
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginCommand(user.Email, password));
        var loginContent = await loginResponse.Content.ReadAsStringAsync();
        var loginResult = JsonSerializer.Deserialize<LoginResponseDto>(loginContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResult!.AccessToken);

        return client;
    }

    [Fact]
    public async Task Post_SuccessfulSend_Returns200AndCreatesAPublicFormInviteEmailLogWithNoDonor()
    {
        _brevoHandler.Respond = _ => ConfigurableBrevoHandler.Success();
        var client = await CreateAuthenticatedClient("Procurement");

        var response = await client.PostAsJsonAsync("/api/v1/donors/public-form-invite", new
        {
            to = "prospect@example.com",
            subject = "Join SA Harvest as a donor",
            body = "Hi, we'd love to have you on board."
        });

        var raw = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var root = JsonDocument.Parse(raw).RootElement.GetProperty("data");
        Assert.Equal("Invitation sent.", root.GetProperty("message").GetString());

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        var emailLog = await context.EmailLogs.SingleAsync();
        Assert.Equal(EmailType.PublicFormInvite, emailLog.EmailType);
        Assert.Equal(EmailStatus.Sent, emailLog.Status);
        Assert.Null(emailLog.DonorId);
        Assert.Equal("prospect@example.com", emailLog.ToAddress);
        Assert.Contains("https://app.example.test/donate", emailLog.Body);

        Assert.Empty(await context.InteractionLogs.ToListAsync());
    }

    [Fact]
    public async Task Post_FailedSend_ReturnsErrorAndCreatesOnlyTheEmailLog()
    {
        _brevoHandler.Respond = _ => ConfigurableBrevoHandler.Failure();
        var client = await CreateAuthenticatedClient("Procurement");

        var response = await client.PostAsJsonAsync("/api/v1/donors/public-form-invite", new
        {
            to = "prospect@example.com",
            subject = "Join SA Harvest as a donor",
            body = "Hi, we'd love to have you on board."
        });

        Assert.False(response.IsSuccessStatusCode);
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal("EMAIL_DELIVERY_FAILED", json.GetProperty("code").GetString());

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        var emailLog = await context.EmailLogs.SingleAsync();
        Assert.Equal(EmailStatus.Failed, emailLog.Status);
    }

    [Fact]
    public async Task Post_MultipleRecipients_Returns400WithClearValidationError()
    {
        var client = await CreateAuthenticatedClient("Procurement");

        var response = await client.PostAsJsonAsync("/api/v1/donors/public-form-invite", new
        {
            to = "a@example.com,b@example.com",
            subject = "Join SA Harvest as a donor",
            body = "Body text."
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal("VALIDATION_ERROR", json.GetProperty("code").GetString());

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        await context.Database.EnsureCreatedAsync();
        Assert.Empty(await context.EmailLogs.ToListAsync());
    }

    [Fact]
    public async Task Post_MarketingUser_Forbidden()
    {
        var client = await CreateAuthenticatedClient("Marketing");

        var response = await client.PostAsJsonAsync("/api/v1/donors/public-form-invite", new
        {
            to = "prospect@example.com",
            subject = "Subject",
            body = "Body"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Post_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/donors/public-form-invite", new
        {
            to = "prospect@example.com",
            subject = "Subject",
            body = "Body"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
