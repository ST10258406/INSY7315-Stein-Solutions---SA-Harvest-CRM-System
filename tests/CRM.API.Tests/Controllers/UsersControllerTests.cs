namespace CRM.API.Tests.Controllers;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CRM.Application.Modules.Auth.Commands.Login;
using CRM.Application.Modules.Auth.Dtos;
using CRM.Application.Modules.Users.Dtos;
using CRM.Domain.Constants;
using CRM.Domain.Entities;
using CRM.Infrastructure.Persistence;
using Hangfire;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

public class UsersControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public UsersControllerTests(WebApplicationFactory<Program> factory)
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable("JWT_SECRET", "12345678901234567890123456789012");
        Environment.SetEnvironmentVariable("BREVO_API_KEY", "test-brevo-key");
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
                {
                    services.Remove(d);
                }

                services.Remove(services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<CrmDbContext>))!);
                services.Remove(services.SingleOrDefault(d => d.ServiceType == typeof(CrmDbContext))!);

                services.AddHangfire(config => config.UseInMemoryStorage());

                services.AddDbContext<CrmDbContext>(options =>
                {
                    options.UseInMemoryDatabase("InMemoryDbForUsersTesting");
                });
            });
        });
    }

    private async Task<(HttpClient Client, User User, Role Role)> CreateAuthenticatedClientAsync(string roleName)
    {
        var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();

        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();

        var role = new Role { Id = Guid.NewGuid(), Name = roleName };
        context.Roles.Add(role);

        var password = "TestPassword123";
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

        var loginCommand = new LoginCommand(user.Email, password);
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", loginCommand);
        var loginContent = await loginResponse.Content.ReadAsStringAsync();
        var loginResult = JsonSerializer.Deserialize<LoginResponseDto>(loginContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResult!.AccessToken);

        return (client, user, role);
    }

    /// <summary>Logs in as an already-seeded user without wiping the DB (unlike CreateAuthenticatedClientAsync) — used to hold a second, independent session's token.</summary>
    private async Task<HttpClient> LoginExistingUserAsync(string email, string password)
    {
        var client = _factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginCommand(email, password));
        var loginContent = await loginResponse.Content.ReadAsStringAsync();
        var loginResult = JsonSerializer.Deserialize<LoginResponseDto>(loginContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResult!.AccessToken);
        return client;
    }

    private static async Task<User> SeedSystemActorAsync(CrmDbContext context)
    {
        var existing = await context.Users.FirstOrDefaultAsync(u => u.Email == SystemUsers.PublicFormEmail);
        if (existing is not null) return existing;

        var systemUser = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Public",
            LastName = "Form Submission",
            Email = SystemUsers.PublicFormEmail,
            PasswordHash = "n/a",
            IsActive = false
        };
        context.Users.Add(systemUser);
        await context.SaveChangesAsync();
        return systemUser;
    }

    [Fact]
    public async Task GetUsers_ProcurementUser_IsForbidden()
    {
        var (client, _, _) = await CreateAuthenticatedClientAsync("Procurement");

        var response = await client.GetAsync("/api/v1/users");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetUsers_AdminUser_ReturnsPaginatedEnvelope()
    {
        var (client, _, _) = await CreateAuthenticatedClientAsync("Admin");

        var response = await client.GetAsync("/api/v1/users");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content).RootElement;
        // At least the Admin who's making the call was seeded.
        Assert.True(json.GetProperty("pagination").GetProperty("totalCount").GetInt32() >= 1);
    }

    [Fact]
    public async Task CreateUser_AdminUser_CreatesUserAndReturnsTemporaryPassword()
    {
        var (client, _, role) = await CreateAuthenticatedClientAsync("Admin");

        var request = new CreateUserRequest { FirstName = "Riaan", LastName = "Fourie", Email = "riaan@saharvest.org", RoleId = role.Id };
        var response = await client.PostAsJsonAsync("/api/v1/users", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content).RootElement.GetProperty("data");

        Assert.Equal("riaan@saharvest.org", json.GetProperty("email").GetString());
        Assert.False(string.IsNullOrWhiteSpace(json.GetProperty("temporaryPassword").GetString()));
    }

    [Fact]
    public async Task CreateUser_DuplicateEmail_ReturnsValidationError()
    {
        var (client, existingUser, role) = await CreateAuthenticatedClientAsync("Admin");

        var request = new CreateUserRequest { FirstName = "Dup", LastName = "User", Email = existingUser.Email, RoleId = role.Id };
        var response = await client.PostAsJsonAsync("/api/v1/users", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateUser_AdminAssigningSuperAdminRole_IsForbidden()
    {
        var (client, _, _) = await CreateAuthenticatedClientAsync("Admin");

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        var superAdminRole = new Role { Id = Guid.NewGuid(), Name = "SuperAdmin" };
        context.Roles.Add(superAdminRole);
        await context.SaveChangesAsync();

        var request = new CreateUserRequest { FirstName = "Escalate", LastName = "Attempt", Email = "escalate@saharvest.org", RoleId = superAdminRole.Id };
        var response = await client.PostAsJsonAsync("/api/v1/users", request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ChangeUserRole_AdminUser_IsForbidden()
    {
        var (client, targetUser, role) = await CreateAuthenticatedClientAsync("Admin");

        var response = await client.PatchAsJsonAsync($"/api/v1/users/{targetUser.Id}/role", new ChangeUserRoleRequest { RoleId = role.Id });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ChangeUserRole_SuperAdminUser_Succeeds()
    {
        var (client, superAdmin, superAdminRole) = await CreateAuthenticatedClientAsync("SuperAdmin");

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        var marketingRole = new Role { Id = Guid.NewGuid(), Name = "Marketing" };
        context.Roles.Add(marketingRole);
        var targetUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "target@saharvest.org",
            FirstName = "Target",
            LastName = "User",
            PasswordHash = "n/a"
        };
        // No Role navigation set here (RoleId alone is enough) — superAdminRole was
        // persisted by CreateAuthenticatedClientAsync in a different DbContext scope,
        // so attaching that instance to this graph would make EF re-insert it.
        targetUser.UserRoles.Add(new UserRole { UserId = targetUser.Id, RoleId = superAdminRole.Id, AssignedByUserId = superAdmin.Id });
        context.Users.Add(targetUser);
        await context.SaveChangesAsync();

        var response = await client.PatchAsJsonAsync($"/api/v1/users/{targetUser.Id}/role", new ChangeUserRoleRequest { RoleId = marketingRole.Id });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content).RootElement.GetProperty("data");
        Assert.Equal("Marketing", json.GetProperty("role").GetString());
    }

    [Fact]
    public async Task ChangeUserRole_SameRoleResubmitted_DoesNotThrow()
    {
        // Regression test: the Change Role dialog preselects the user's current role, so
        // submitting without changing it is a normal request. A naive "clear the tracked
        // UserRoles collection, then add a new row with the same (UserId, RoleId) key"
        // implementation throws under EF Core's real change tracker (not reproducible
        // against a mocked IUserRepository) because two tracked entries end up with an
        // identical key.
        var (client, superAdmin, superAdminRole) = await CreateAuthenticatedClientAsync("SuperAdmin");

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        var targetUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "unchanged-role@saharvest.org",
            FirstName = "Unchanged",
            LastName = "Role",
            PasswordHash = "n/a"
        };
        targetUser.UserRoles.Add(new UserRole { UserId = targetUser.Id, RoleId = superAdminRole.Id, AssignedByUserId = superAdmin.Id });
        context.Users.Add(targetUser);
        await context.SaveChangesAsync();

        var response = await client.PatchAsJsonAsync($"/api/v1/users/{targetUser.Id}/role", new ChangeUserRoleRequest { RoleId = superAdminRole.Id });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content).RootElement.GetProperty("data");
        Assert.Equal("SuperAdmin", json.GetProperty("role").GetString());
    }

    [Fact]
    public async Task SetUserActiveStatus_DeactivateSelf_IsForbidden()
    {
        var (client, self, _) = await CreateAuthenticatedClientAsync("Admin");

        var response = await client.PatchAsJsonAsync($"/api/v1/users/{self.Id}/status", new SetUserActiveStatusRequest { IsActive = false });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SetUserActiveStatus_DeactivateOtherUser_TheirExistingAccessTokenIsRejectedImmediately()
    {
        // Regression: deactivation must cut off access on the very next request, not just
        // block future logins/refreshes and leave an already-issued access token valid
        // until it expires.
        var (adminClient, _, adminRole) = await CreateAuthenticatedClientAsync("Admin");

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        const string targetPassword = "TargetPassword123";
        var hasher = new PasswordHasher<User>();
        var targetUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "will-be-deactivated@saharvest.org",
            FirstName = "Will",
            LastName = "BeDeactivated",
            PasswordHash = hasher.HashPassword(null!, targetPassword),
            IsActive = true
        };
        targetUser.UserRoles.Add(new UserRole { UserId = targetUser.Id, RoleId = adminRole.Id, AssignedByUserId = targetUser.Id });
        context.Users.Add(targetUser);
        await context.SaveChangesAsync();

        var targetClient = await LoginExistingUserAsync(targetUser.Email, targetPassword);

        // Confirm the token works before deactivation.
        var beforeResponse = await targetClient.GetAsync("/api/v1/users");
        Assert.Equal(HttpStatusCode.OK, beforeResponse.StatusCode);

        var deactivateResponse = await adminClient.PatchAsJsonAsync(
            $"/api/v1/users/{targetUser.Id}/status", new SetUserActiveStatusRequest { IsActive = false });
        Assert.Equal(HttpStatusCode.OK, deactivateResponse.StatusCode);

        // Same access token, no re-login — must be rejected now.
        var afterResponse = await targetClient.GetAsync("/api/v1/users");
        Assert.Equal(HttpStatusCode.Unauthorized, afterResponse.StatusCode);
    }

    [Fact]
    public async Task ChangeUserRole_TargetLosesElevatedAccessImmediately()
    {
        // Regression: a role downgrade must take effect on the very next request — the
        // demoted user's existing access token still carries the old (elevated) role
        // claim, but the JWT pipeline re-derives roles from the DB on every request.
        var (superAdminClient, actingSuperAdmin, superAdminRole) = await CreateAuthenticatedClientAsync("SuperAdmin");

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        var marketingRole = new Role { Id = Guid.NewGuid(), Name = "Marketing" };
        context.Roles.Add(marketingRole);
        const string targetPassword = "TargetPassword123";
        var hasher = new PasswordHasher<User>();
        var targetUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "soon-demoted@saharvest.org",
            FirstName = "Soon",
            LastName = "Demoted",
            PasswordHash = hasher.HashPassword(null!, targetPassword),
            IsActive = true
        };
        targetUser.UserRoles.Add(new UserRole { UserId = targetUser.Id, RoleId = superAdminRole.Id, AssignedByUserId = actingSuperAdmin.Id });
        context.Users.Add(targetUser);
        await context.SaveChangesAsync();

        var targetClient = await LoginExistingUserAsync(targetUser.Email, targetPassword);

        // Confirm the SuperAdminOnly endpoint works before the demotion (no-op re-submit
        // of the acting SuperAdmin's own current role, so nothing else changes).
        var beforeResponse = await targetClient.PatchAsJsonAsync(
            $"/api/v1/users/{actingSuperAdmin.Id}/role", new ChangeUserRoleRequest { RoleId = superAdminRole.Id });
        Assert.Equal(HttpStatusCode.OK, beforeResponse.StatusCode);

        var demoteResponse = await superAdminClient.PatchAsJsonAsync(
            $"/api/v1/users/{targetUser.Id}/role", new ChangeUserRoleRequest { RoleId = marketingRole.Id });
        Assert.Equal(HttpStatusCode.OK, demoteResponse.StatusCode);

        // Same access token as before — must lose SuperAdminOnly access immediately.
        var afterResponse = await targetClient.PatchAsJsonAsync(
            $"/api/v1/users/{actingSuperAdmin.Id}/role", new ChangeUserRoleRequest { RoleId = superAdminRole.Id });
        Assert.Equal(HttpStatusCode.Forbidden, afterResponse.StatusCode);
    }

    [Fact]
    public async Task GetUsers_ExcludesSystemActor()
    {
        var (client, _, _) = await CreateAuthenticatedClientAsync("Admin");
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        await SeedSystemActorAsync(context);

        var response = await client.GetAsync("/api/v1/users");

        var content = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(SystemUsers.PublicFormEmail, content);
    }

    [Fact]
    public async Task UpdateUser_SystemActor_ReturnsNotFound()
    {
        var (client, _, _) = await CreateAuthenticatedClientAsync("Admin");
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        var systemUser = await SeedSystemActorAsync(context);

        var response = await client.PatchAsJsonAsync(
            $"/api/v1/users/{systemUser.Id}",
            new UpdateUserRequest { FirstName = "Hacked", LastName = "Name", Email = "hacked@saharvest.org" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SetUserActiveStatus_SystemActor_ReturnsNotFound()
    {
        var (client, _, _) = await CreateAuthenticatedClientAsync("Admin");
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        var systemUser = await SeedSystemActorAsync(context);

        var response = await client.PatchAsJsonAsync(
            $"/api/v1/users/{systemUser.Id}/status", new SetUserActiveStatusRequest { IsActive = true });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ChangeUserRole_SystemActor_ReturnsNotFound()
    {
        var (client, _, role) = await CreateAuthenticatedClientAsync("SuperAdmin");
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        var systemUser = await SeedSystemActorAsync(context);

        var response = await client.PatchAsJsonAsync(
            $"/api/v1/users/{systemUser.Id}/role", new ChangeUserRoleRequest { RoleId = role.Id });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
