namespace CRM.API.Tests.Controllers;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CRM.Application.Modules.Auth.Commands.Login;
using CRM.Application.Modules.Auth.Dtos;
using CRM.Domain.Entities;
using CRM.Domain.Entities.Lookups;
using CRM.Domain.Enums;
using CRM.Infrastructure.Persistence;
using Hangfire;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

public class DonorInteractionsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public DonorInteractionsControllerTests(WebApplicationFactory<Program> factory)
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable("JWT_SECRET", "12345678901234567890123456789012");
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", "Host=localhost;Database=fake;Username=postgres;Password=password");
        Environment.SetEnvironmentVariable("Jwt__SigningKey", "12345678901234567890123456789012");
        Environment.SetEnvironmentVariable("Jwt__AccessTokenExpiryMinutes", "60");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "TestIssuer");
        Environment.SetEnvironmentVariable("Jwt__Audience", "TestAudience");

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
                services.AddDbContext<CrmDbContext>(options => options.UseInMemoryDatabase("InMemoryDbForInteractionsTesting"));
            });
        });
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
            Email = $"test-{roleName.ToLower()}@example.com",
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
    public async Task Post_ValidInteraction_Returns201AndAppendsToTheFeed()
    {
        var client = await CreateAuthenticatedClient("Procurement");
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        var donorId = await SeedDonorAsync(context);

        var post = await client.PostAsJsonAsync($"/api/v1/donors/{donorId}/interactions", new
        {
            interactionType = "Call",
            subject = "Intro call",
            body = "Spoke to the procurement lead."
        });

        Assert.Equal(HttpStatusCode.Created, post.StatusCode);
        var created = JsonDocument.Parse(await post.Content.ReadAsStringAsync()).RootElement.GetProperty("data");
        Assert.Equal("Call", created.GetProperty("interactionType").GetString());
        Assert.Equal(donorId, created.GetProperty("donorId").GetGuid());
        Assert.Equal("Procurement Test", created.GetProperty("createdBy").GetProperty("fullName").GetString());

        var get = await client.GetAsync($"/api/v1/donors/{donorId}/interactions");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        var json = JsonDocument.Parse(await get.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(1, json.GetProperty("data").GetArrayLength());
        Assert.Equal(1, json.GetProperty("pagination").GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task Post_WithFutureFollowUpDate_MovesDonorFollowUpDate()
    {
        var client = await CreateAuthenticatedClient("Procurement");
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        var donorId = await SeedDonorAsync(context);
        var followUp = DateTime.UtcNow.AddDays(7);

        var post = await client.PostAsJsonAsync($"/api/v1/donors/{donorId}/interactions", new
        {
            interactionType = "Meeting",
            body = "Agreed to reconnect next week.",
            followUpDate = followUp
        });

        Assert.Equal(HttpStatusCode.Created, post.StatusCode);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<CrmDbContext>();
        var donor = await verifyContext.Donors.AsNoTracking().SingleAsync(d => d.Id == donorId);
        Assert.NotNull(donor.FollowUpDate);
        Assert.Equal(followUp.Date, donor.FollowUpDate!.Value.Date);
    }

    [Fact]
    public async Task Post_PastFollowUpDate_Returns400()
    {
        var client = await CreateAuthenticatedClient("Procurement");
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        var donorId = await SeedDonorAsync(context);

        var post = await client.PostAsJsonAsync($"/api/v1/donors/{donorId}/interactions", new
        {
            interactionType = "Call",
            body = "x",
            followUpDate = DateTime.UtcNow.AddDays(-1)
        });

        Assert.Equal(HttpStatusCode.BadRequest, post.StatusCode);
    }

    [Fact]
    public async Task Post_UnknownInteractionType_Returns400()
    {
        var client = await CreateAuthenticatedClient("Procurement");
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        var donorId = await SeedDonorAsync(context);

        var post = await client.PostAsJsonAsync($"/api/v1/donors/{donorId}/interactions", new
        {
            interactionType = "Telepathy",
            body = "x"
        });

        Assert.Equal(HttpStatusCode.BadRequest, post.StatusCode);
    }

    [Fact]
    public async Task Get_NewestFirst_AndFiltersByType()
    {
        var client = await CreateAuthenticatedClient("Procurement");
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        var donorId = await SeedDonorAsync(context);

        await client.PostAsJsonAsync($"/api/v1/donors/{donorId}/interactions", new { interactionType = "Note", body = "first" });
        await client.PostAsJsonAsync($"/api/v1/donors/{donorId}/interactions", new { interactionType = "Call", body = "second" });
        await client.PostAsJsonAsync($"/api/v1/donors/{donorId}/interactions", new { interactionType = "Note", body = "third" });

        var all = JsonDocument.Parse(await (await client.GetAsync($"/api/v1/donors/{donorId}/interactions")).Content.ReadAsStringAsync()).RootElement;
        var bodies = all.GetProperty("data").EnumerateArray().Select(e => e.GetProperty("body").GetString()).ToArray();
        Assert.Equal(new[] { "third", "second", "first" }, bodies);

        var notesOnly = JsonDocument.Parse(await (await client.GetAsync($"/api/v1/donors/{donorId}/interactions?interactionType=Note")).Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(2, notesOnly.GetProperty("pagination").GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task Get_UnknownDonor_Returns404WithErrorEnvelope()
    {
        var client = await CreateAuthenticatedClient("Procurement");
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        await SeedDonorAsync(context);

        var response = await client.GetAsync($"/api/v1/donors/{Guid.NewGuid()}/interactions");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(404, json.GetProperty("status").GetInt32());
        Assert.Equal("NOT_FOUND", json.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Endpoints_MarketingUser_Forbidden()
    {
        var client = await CreateAuthenticatedClient("Marketing");

        var get = await client.GetAsync($"/api/v1/donors/{Guid.NewGuid()}/interactions");
        var post = await client.PostAsJsonAsync($"/api/v1/donors/{Guid.NewGuid()}/interactions", new { interactionType = "Call", body = "x" });

        Assert.Equal(HttpStatusCode.Forbidden, get.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, post.StatusCode);
    }

    [Fact]
    public async Task Get_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/v1/donors/{Guid.NewGuid()}/interactions");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
