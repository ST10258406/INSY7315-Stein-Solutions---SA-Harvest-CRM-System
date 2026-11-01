namespace CRM.API.Tests.Controllers;

using System.Net;
using System.Net.Http.Headers;
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
using Microsoft.Extensions.DependencyInjection;

/// <summary>Issue 103: /api/v1/saved-filters is ProcurementOrAbove and scoped to the caller.</summary>
public class SavedFiltersControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Url = "/api/v1/saved-filters";
    private readonly WebApplicationFactory<Program> _factory;

    public SavedFiltersControllerTests(WebApplicationFactory<Program> factory)
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable("JWT_SECRET", "12345678901234567890123456789012");
        Environment.SetEnvironmentVariable("BREVO_API_KEY", "test-brevo-key");
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", "Host=localhost;Database=fake;Username=postgres;Password=password");
        Environment.SetEnvironmentVariable("Jwt__AccessTokenExpiryMinutes", "60");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "TestIssuer");
        Environment.SetEnvironmentVariable("Jwt__Audience", "TestAudience");

        var databaseName = $"InMemoryDbForSavedFilters-{Guid.NewGuid():N}";
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
                services.AddDbContext<CrmDbContext>(options => options.UseInMemoryDatabase(databaseName));
            });
        });
    }

    private async Task<HttpClient> LoginAsAsync(string roleName)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        var role = await context.Roles.FirstOrDefaultAsync(r => r.Name == roleName)
            ?? context.Roles.Add(new Role { Id = Guid.NewGuid(), Name = roleName }).Entity;
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = $"filters-{Guid.NewGuid():N}@example.com",
            FirstName = roleName,
            LastName = "Test",
            PasswordHash = new PasswordHasher<User>().HashPassword(null!, "TestPassword123"),
            IsActive = true,
        };
        user.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id, AssignedByUserId = user.Id });
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var client = _factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginCommand(user.Email, "TestPassword123"));
        var body = JsonSerializer.Deserialize<LoginResponseDto>(
            await login.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.AccessToken);
        return client;
    }

    private static object Body(string name = "Gauteng") => new { name, page = "Donors", queryString = "regionCode=GP&page=2" };

    private static async Task<JsonElement> JsonAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();

    [Fact]
    public async Task Anonymous_Returns401()
        => Assert.Equal(HttpStatusCode.Unauthorized, (await _factory.CreateClient().GetAsync(Url)).StatusCode);

    [Fact]
    public async Task Marketing_Returns403()
    {
        var client = await LoginAsAsync("Marketing");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(Url)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync(Url, Body())).StatusCode);
    }

    [Fact]
    public async Task Procurement_CreateListDuplicateDelete()
    {
        var client = await LoginAsAsync("Procurement");

        var created = await client.PostAsJsonAsync(Url, Body());
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var data = (await JsonAsync(created)).GetProperty("data");
        Assert.Equal("regionCode=GP", data.GetProperty("queryString").GetString()); // page stripped
        var id = data.GetProperty("id").GetGuid();

        var list = await JsonAsync(await client.GetAsync($"{Url}?page=Donors"));
        Assert.Equal("Gauteng", Assert.Single(list.EnumerateArray()).GetProperty("name").GetString());

        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync(Url, Body("gauteng"))).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"{Url}/{id}")).StatusCode);
        Assert.Empty((await JsonAsync(await client.GetAsync(Url))).EnumerateArray());
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"{Url}/{id}")).StatusCode);

        // Soft delete: the row is still there, inactive, and both actions were audited.
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        Assert.False((await context.SavedFilters.SingleAsync(f => f.Id == id)).IsActive);
        var audit = await context.AuditLogs.Where(a => a.EntityType == "SavedFilter" && a.EntityId == id).ToListAsync();
        Assert.Contains(audit, a => a.Action == AuditAction.Created);
        Assert.Contains(audit, a => a.Action == AuditAction.Deleted);
    }

    [Fact]
    public async Task AnotherUsersFilter_IsInvisibleAndReturns404OnDelete()
    {
        var owner = await LoginAsAsync("Admin");
        var id = (await JsonAsync(await owner.PostAsJsonAsync(Url, Body()))).GetProperty("data").GetProperty("id").GetGuid();

        var other = await LoginAsAsync("SuperAdmin");
        Assert.Empty((await JsonAsync(await other.GetAsync(Url))).EnumerateArray());
        Assert.Equal(HttpStatusCode.NotFound, (await other.DeleteAsync($"{Url}/{id}")).StatusCode);
    }

    [Fact]
    public async Task DisallowedQueryKey_Returns400Envelope()
    {
        var client = await LoginAsAsync("Procurement");

        var response = await client.PostAsJsonAsync(Url, new { name = "x", page = "Donors", queryString = "status=Active&redirect=evil" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("VALIDATION_ERROR", (await JsonAsync(response)).GetProperty("code").GetString());
    }
}
