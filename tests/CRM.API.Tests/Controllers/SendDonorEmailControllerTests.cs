namespace CRM.API.Tests.Controllers;

using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Auth.Commands.Login;
using CRM.Application.Modules.Auth.Dtos;
using CRM.Domain.Entities;
using CRM.Domain.Entities.Lookups;
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
/// Compose-and-send donor email (POST /api/v1/donors/{id}/interactions/email).
/// Brevo's real HTTP call is faked via <see cref="ConfigurableBrevoHandler"/>, whose
/// response each test controls, so both the success path (EmailLog + InteractionLog)
/// and the failure path (EmailLog only, error surfaced) run deterministically without
/// touching the network.
/// </summary>
public class SendDonorEmailControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly ConfigurableBrevoHandler _brevoHandler = new();

    public SendDonorEmailControllerTests(WebApplicationFactory<Program> factory)
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable("JWT_SECRET", "12345678901234567890123456789012");
        Environment.SetEnvironmentVariable("BREVO_API_KEY", "test-brevo-key");
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", "Host=localhost;Database=fake;Username=postgres;Password=password");
        Environment.SetEnvironmentVariable("Jwt__SigningKey", "12345678901234567890123456789012");
        Environment.SetEnvironmentVariable("Jwt__AccessTokenExpiryMinutes", "60");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "TestIssuer");
        Environment.SetEnvironmentVariable("Jwt__Audience", "TestAudience");
        // Small limit so the rate-limit test doesn't need 20+ requests to trip it.
        Environment.SetEnvironmentVariable("RateLimiting__DonorEmail__PermitLimit", "2");
        Environment.SetEnvironmentVariable("RateLimiting__DonorEmail__WindowMinutes", "60");

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
                services.AddDbContext<CrmDbContext>(options => options.UseInMemoryDatabase("InMemoryDbForSendDonorEmailTesting"));

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

    private static async Task<Guid> SeedDonorAsync(CrmDbContext context)
    {
        if (!await context.LookupCompanyTypes.AnyAsync())
        {
            context.LookupCompanyTypes.Add(new LookupCompanyType { Id = 1, Name = "Manufacturer", IsActive = true });
            context.LookupEntityTypes.Add(new LookupEntityType { Id = 1, Name = "Pty Ltd", IsActive = true });
            context.LookupDonationFrequencies.Add(new LookupDonationFrequency { Id = 1, Name = "Monthly", IsActive = true });
        }

        var creator = new User { Id = Guid.NewGuid(), Email = $"creator-{Guid.NewGuid()}@example.com", FirstName = "C", LastName = "U", PasswordHash = "n/a" };
        context.Users.Add(creator);

        var donor = new Donor
        {
            Id = Guid.NewGuid(),
            CompanyName = "FoodCorp SA",
            CompanyTypeId = 1,
            RegisteredCompanyName = "FoodCorp (Pty) Ltd",
            EntityTypeId = 1,
            DonationFrequencyId = 1,
            Status = DonorStatus.Active,
            SubmissionSource = SubmissionSource.ManualCapture,
            CreatedByUserId = creator.Id
        };
        context.Donors.Add(donor);
        await context.SaveChangesAsync();
        return donor.Id;
    }

    [Fact]
    public async Task Post_SuccessfulSend_Returns201AndCreatesEmailLogAndInteractionLog()
    {
        _brevoHandler.Respond = _ => ConfigurableBrevoHandler.Success();
        var client = await CreateAuthenticatedClient("Procurement");
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        var donorId = await SeedDonorAsync(context);

        var response = await client.PostAsJsonAsync($"/api/v1/donors/{donorId}/interactions/email", new
        {
            to = "donor@example.com",
            subject = "Thanks for your support",
            body = "Just checking in ahead of next month's collection."
        });

        var raw = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = JsonDocument.Parse(raw).RootElement.GetProperty("data");
        Assert.Equal("Email", created.GetProperty("interactionType").GetString());
        Assert.Equal(donorId, created.GetProperty("donorId").GetGuid());

        using var verifyScope = _factory.Services.CreateScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<CrmDbContext>();

        var emailLog = await verifyContext.EmailLogs.SingleAsync();
        Assert.Equal(EmailType.DonorCorrespondence, emailLog.EmailType);
        Assert.Equal(EmailStatus.Sent, emailLog.Status);
        Assert.Equal(donorId, emailLog.DonorId);
        Assert.Equal("donor@example.com", emailLog.ToAddress);

        var interactionLog = await verifyContext.InteractionLogs.SingleAsync(l => l.DonorId == donorId);
        Assert.Equal(InteractionType.Email, interactionLog.InteractionType);
        Assert.Equal(emailLog.Id, interactionLog.RelatedEntityId);
        Assert.Equal("EmailLog", interactionLog.RelatedEntityType);
    }

    [Fact]
    public async Task Post_FailedSend_ReturnsErrorAndCreatesOnlyTheEmailLog()
    {
        _brevoHandler.Respond = _ => ConfigurableBrevoHandler.Failure();
        var client = await CreateAuthenticatedClient("Procurement");
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        var donorId = await SeedDonorAsync(context);

        var response = await client.PostAsJsonAsync($"/api/v1/donors/{donorId}/interactions/email", new
        {
            to = "donor@example.com",
            subject = "Thanks for your support",
            body = "Just checking in ahead of next month's collection."
        });

        Assert.False(response.IsSuccessStatusCode);
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal("EMAIL_DELIVERY_FAILED", json.GetProperty("code").GetString());

        using var verifyScope = _factory.Services.CreateScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<CrmDbContext>();

        var emailLog = await verifyContext.EmailLogs.SingleAsync();
        Assert.Equal(EmailStatus.Failed, emailLog.Status);

        Assert.Empty(await verifyContext.InteractionLogs.Where(l => l.DonorId == donorId).ToListAsync());
    }

    [Fact]
    public async Task Post_MultipleRecipients_Returns400WithClearValidationError()
    {
        _brevoHandler.Respond = _ => ConfigurableBrevoHandler.Success();
        var client = await CreateAuthenticatedClient("Procurement");
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        var donorId = await SeedDonorAsync(context);

        var response = await client.PostAsJsonAsync($"/api/v1/donors/{donorId}/interactions/email", new
        {
            to = "donor@example.com,someoneelse@example.com",
            subject = "Thanks for your support",
            body = "Body text."
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal("VALIDATION_ERROR", json.GetProperty("code").GetString());
        var errors = json.GetProperty("errors").EnumerateArray().Select(e => e.GetProperty("message").GetString()).ToArray();
        Assert.Contains(errors, m => m!.Contains("single recipient"));

        using var verifyScope = _factory.Services.CreateScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<CrmDbContext>();
        Assert.Empty(await verifyContext.EmailLogs.ToListAsync());
    }

    [Fact]
    public async Task Post_ExceedsRateLimit_Returns429AfterConfiguredLimit()
    {
        // RateLimiting:DonorEmail:PermitLimit is overridden to 2 in this class's
        // env vars specifically so this test doesn't need 20+ requests to trip it.
        _brevoHandler.Respond = _ => ConfigurableBrevoHandler.Success();
        var client = await CreateAuthenticatedClient("Procurement");
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        var donorId = await SeedDonorAsync(context);

        object Payload(int i) => new { to = "donor@example.com", subject = $"Update {i}", body = "Body text." };

        var first = await client.PostAsJsonAsync($"/api/v1/donors/{donorId}/interactions/email", Payload(1));
        var second = await client.PostAsJsonAsync($"/api/v1/donors/{donorId}/interactions/email", Payload(2));
        var third = await client.PostAsJsonAsync($"/api/v1/donors/{donorId}/interactions/email", Payload(3));

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        Assert.Equal((HttpStatusCode)429, third.StatusCode);

        var json = JsonDocument.Parse(await third.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal("RATE_LIMITED", json.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Post_MarketingUser_Forbidden()
    {
        var client = await CreateAuthenticatedClient("Marketing");

        var response = await client.PostAsJsonAsync($"/api/v1/donors/{Guid.NewGuid()}/interactions/email", new
        {
            to = "donor@example.com",
            subject = "Subject",
            body = "Body"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Post_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync($"/api/v1/donors/{Guid.NewGuid()}/interactions/email", new
        {
            to = "donor@example.com",
            subject = "Subject",
            body = "Body"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_UnknownDonor_Returns404()
    {
        _brevoHandler.Respond = _ => ConfigurableBrevoHandler.Success();
        // CreateAuthenticatedClient already wipes and recreates the DB before seeding its
        // own user, and this test never seeds a donor — re-wiping here after login would
        // also delete that just-created user, which now fails authentication entirely
        // (the JWT pipeline re-checks the user still exists on every request) instead of
        // reaching the controller to 404 on the unknown donor id.
        var client = await CreateAuthenticatedClient("Procurement");

        var response = await client.PostAsJsonAsync($"/api/v1/donors/{Guid.NewGuid()}/interactions/email", new
        {
            to = "donor@example.com",
            subject = "Subject",
            body = "Body"
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
