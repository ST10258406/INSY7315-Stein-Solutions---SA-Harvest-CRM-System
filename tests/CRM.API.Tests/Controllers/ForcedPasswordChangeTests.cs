namespace CRM.API.Tests.Controllers;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CRM.Application.Modules.Auth.Commands.ChangePassword;
using CRM.Application.Modules.Auth.Commands.Login;
using CRM.Domain.Entities;
using CRM.Infrastructure.Persistence;
using Hangfire;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// F-13: a user flagged MustChangePassword (seeded admin, admin-created user) can do nothing
/// except change the password and log out until the flag is cleared — enforced server-side.
/// </summary>
public class ForcedPasswordChangeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string TempPassword = "TempPassword123!";
    private readonly WebApplicationFactory<Program> _baseFactory;

    public ForcedPasswordChangeTests(WebApplicationFactory<Program> factory)
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable("JWT_SECRET", "12345678901234567890123456789012");
        Environment.SetEnvironmentVariable("BREVO_API_KEY", "test-brevo-key");
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", "Host=localhost;Database=fake;Username=postgres;Password=password");
        Environment.SetEnvironmentVariable("Jwt__AccessTokenExpiryMinutes", "60");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "TestIssuer");
        Environment.SetEnvironmentVariable("Jwt__Audience", "TestAudience");
        _baseFactory = factory;
    }

    private WebApplicationFactory<Program> CreateFactory()
    {
        var databaseName = $"InMemoryDbForForcedChange-{Guid.NewGuid():N}";
        return _baseFactory.WithWebHostBuilder(builder =>
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

    private static async Task SeedUserAsync(WebApplicationFactory<Program> factory, string email, bool mustChange)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();

        var role = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin")
            ?? context.Roles.Add(new Role { Id = Guid.NewGuid(), Name = "Admin" }).Entity;
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            FirstName = "Forced",
            LastName = "Change",
            PasswordHash = new PasswordHasher<User>().HashPassword(null!, TempPassword),
            IsActive = true,
            MustChangePassword = mustChange
        };
        user.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id, AssignedByUserId = user.Id });
        context.Users.Add(user);
        await context.SaveChangesAsync();
    }

    private static async Task<(HttpClient Client, JsonElement LoginBody)> LoginAsync(
        WebApplicationFactory<Program> factory, string email, string password)
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginCommand(email, password));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", body.GetProperty("accessToken").GetString());

        // The refresh cookie is Secure, so the test client (plain http) would not send it back
        // by itself. A browser does, and the server uses it to keep the caller's own session
        // alive when a password change revokes the others — so replay it explicitly.
        var setCookie = response.Headers.GetValues("Set-Cookie").First();
        client.DefaultRequestHeaders.Add("Cookie", setCookie.Split(';')[0]);
        return (client, body);
    }

    [Fact]
    public async Task FlaggedUser_LoginReportsTheFlag_AndEveryOtherEndpointIsBlocked()
    {
        using var factory = CreateFactory();
        await SeedUserAsync(factory, "forced@example.com", mustChange: true);

        var (client, login) = await LoginAsync(factory, "forced@example.com", TempPassword);

        Assert.True(login.GetProperty("user").GetProperty("mustChangePassword").GetBoolean());

        var blocked = await client.GetAsync("/api/v1/donors");
        Assert.Equal(HttpStatusCode.Forbidden, blocked.StatusCode);
        var body = JsonDocument.Parse(await blocked.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal("PASSWORD_CHANGE_REQUIRED", body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task FlaggedUser_CanChangePassword_ThenRegainsAccessWithTheSameToken()
    {
        using var factory = CreateFactory();
        await SeedUserAsync(factory, "forced2@example.com", mustChange: true);
        var (client, _) = await LoginAsync(factory, "forced2@example.com", TempPassword);

        var change = await client.PatchAsJsonAsync("/api/auth/change-password",
            new ChangePasswordCommand(TempPassword, "BrandNewPassword456!", "BrandNewPassword456!"));
        Assert.Equal(HttpStatusCode.NoContent, change.StatusCode);

        var after = await client.GetAsync("/api/v1/donors");
        Assert.Equal(HttpStatusCode.OK, after.StatusCode);

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        Assert.False((await context.Users.SingleAsync(u => u.Email == "forced2@example.com")).MustChangePassword);
    }

    [Fact]
    public async Task FlaggedUser_CannotReuseTheTemporaryPassword()
    {
        using var factory = CreateFactory();
        await SeedUserAsync(factory, "forced3@example.com", mustChange: true);
        var (client, _) = await LoginAsync(factory, "forced3@example.com", TempPassword);

        var change = await client.PatchAsJsonAsync("/api/auth/change-password",
            new ChangePasswordCommand(TempPassword, TempPassword, TempPassword));

        Assert.Equal(HttpStatusCode.BadRequest, change.StatusCode);
    }

    [Fact]
    public async Task FlaggedUser_CanStillLogOut()
    {
        using var factory = CreateFactory();
        await SeedUserAsync(factory, "forced4@example.com", mustChange: true);
        var (client, _) = await LoginAsync(factory, "forced4@example.com", TempPassword);

        var logout = await client.PostAsync("/api/auth/logout", null);

        Assert.NotEqual("PASSWORD_CHANGE_REQUIRED", await CodeOrEmptyAsync(logout));
    }

    [Fact]
    public async Task UnflaggedUser_IsNotBlocked()
    {
        using var factory = CreateFactory();
        await SeedUserAsync(factory, "normal@example.com", mustChange: false);
        var (client, login) = await LoginAsync(factory, "normal@example.com", TempPassword);

        Assert.False(login.GetProperty("user").GetProperty("mustChangePassword").GetBoolean());
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/donors")).StatusCode);
    }

    private static async Task<string> CodeOrEmptyAsync(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        try
        {
            return JsonDocument.Parse(text).RootElement.TryGetProperty("code", out var code)
                ? code.GetString() ?? string.Empty
                : string.Empty;
        }
        catch (JsonException)
        {
            return string.Empty;
        }
    }
}
