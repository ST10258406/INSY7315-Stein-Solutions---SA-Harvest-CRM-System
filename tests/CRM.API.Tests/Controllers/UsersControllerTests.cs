namespace CRM.API.Tests.Controllers;

using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Auth.Commands.Login;
using CRM.Application.Modules.Auth.Dtos;
using CRM.Application.Modules.Users.Dtos;
using CRM.Domain.Constants;
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

                // Email changes send a notice to the old address — never call real Brevo.
                services.AddHttpClient<IEmailService, EmailService>()
                    .ConfigurePrimaryHttpMessageHandler(() => new FakeBrevoHandler());
            });
        });
    }

    private class FakeBrevoHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(new { messageId = "fake-brevo-message-id" }), Encoding.UTF8, "application/json")
            });
    }

    /// <summary>Seeds a user holding <paramref name="roleId"/> without wiping the DB.</summary>
    private static async Task<User> SeedUserAsync(CrmDbContext context, Guid roleId, string email, string password = "not-used")
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            FirstName = "Seeded",
            LastName = "User",
            PasswordHash = new PasswordHasher<User>().HashPassword(null!, password),
            IsActive = true
        };
        user.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = roleId, AssignedByUserId = user.Id });
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
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
    public async Task SetUserActiveStatus_MissingIsActive_ReturnsBadRequestAndLeavesUserActive()
    {
        // Regression: a body of {} used to bind IsActive to false and deactivate the
        // target. A missing value must be rejected, never treated as "deactivate".
        var (client, _, role) = await CreateAuthenticatedClientAsync("Admin");

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        var targetUser = new User
        {
            Id = Guid.NewGuid(),
            Email = $"empty-body-{Guid.NewGuid():N}@saharvest.org",
            FirstName = "Empty",
            LastName = "Body",
            PasswordHash = "not-used",
            IsActive = true
        };
        targetUser.UserRoles.Add(new UserRole { UserId = targetUser.Id, RoleId = role.Id, AssignedByUserId = targetUser.Id });
        context.Users.Add(targetUser);
        await context.SaveChangesAsync();

        var response = await client.PatchAsJsonAsync($"/api/v1/users/{targetUser.Id}/status", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var reloaded = await context.Users.AsNoTracking().SingleAsync(u => u.Id == targetUser.Id);
        Assert.True(reloaded.IsActive);
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

    // ---- F-01: per-target authorization (UserTargetAuthorizationFilter) ----
    // System users can't be managed by anyone and SuperAdmins only by SuperAdmins; both
    // are refused at the API boundary with 403 before any command handler runs.

    [Fact]
    public async Task UpdateUser_SystemActor_IsForbidden()
    {
        var (client, _, _) = await CreateAuthenticatedClientAsync("Admin");
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        var systemUser = await SeedSystemActorAsync(context);

        var response = await client.PatchAsJsonAsync(
            $"/api/v1/users/{systemUser.Id}",
            new UpdateUserRequest { FirstName = "Hacked", LastName = "Name", Email = "hacked@saharvest.org" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var reloaded = await context.Users.AsNoTracking().SingleAsync(u => u.Id == systemUser.Id);
        Assert.Equal(SystemUsers.PublicFormEmail, reloaded.Email);
    }

    [Fact]
    public async Task UpdateUser_SuperAdminTargetingSystemActor_IsForbidden()
    {
        // Not even a SuperAdmin may rename the system actor — SubmitPublicDonorCommandHandler
        // looks it up by email, so renaming it breaks the public form.
        var (client, _, _) = await CreateAuthenticatedClientAsync("SuperAdmin");
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        var systemUser = await SeedSystemActorAsync(context);

        var response = await client.PatchAsJsonAsync(
            $"/api/v1/users/{systemUser.Id}",
            new UpdateUserRequest { FirstName = "Public", LastName = "Form Submission", Email = "renamed@saharvest.org" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SetUserActiveStatus_SystemActor_IsForbidden()
    {
        var (client, _, _) = await CreateAuthenticatedClientAsync("Admin");
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        var systemUser = await SeedSystemActorAsync(context);

        var response = await client.PatchAsJsonAsync(
            $"/api/v1/users/{systemUser.Id}/status", new SetUserActiveStatusRequest { IsActive = true });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var reloaded = await context.Users.AsNoTracking().SingleAsync(u => u.Id == systemUser.Id);
        Assert.False(reloaded.IsActive);
    }

    [Fact]
    public async Task ChangeUserRole_SystemActor_IsForbidden()
    {
        var (client, _, role) = await CreateAuthenticatedClientAsync("SuperAdmin");
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        var systemUser = await SeedSystemActorAsync(context);

        var response = await client.PatchAsJsonAsync(
            $"/api/v1/users/{systemUser.Id}/role", new ChangeUserRoleRequest { RoleId = role.Id });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateUser_AdminTargetingSuperAdmin_IsForbiddenAndEmailUnchanged()
    {
        // Regression for F-01: an Admin could change a SuperAdmin's email to one they
        // control, then use forgot-password to take the account over.
        var (client, _, _) = await CreateAuthenticatedClientAsync("Admin");
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        var superAdminRole = new Role { Id = Guid.NewGuid(), Name = "SuperAdmin" };
        context.Roles.Add(superAdminRole);
        var superAdmin = await SeedUserAsync(context, superAdminRole.Id, "owner@saharvest.org");

        var response = await client.PatchAsJsonAsync(
            $"/api/v1/users/{superAdmin.Id}",
            new UpdateUserRequest { FirstName = "Seeded", LastName = "User", Email = "attacker@evil.example" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.True(json.TryGetProperty("traceId", out _));

        var reloaded = await context.Users.AsNoTracking().SingleAsync(u => u.Id == superAdmin.Id);
        Assert.Equal("owner@saharvest.org", reloaded.Email);
    }

    [Fact]
    public async Task SetUserActiveStatus_AdminTargetingSuperAdmin_IsForbiddenAndStaysActive()
    {
        var (client, _, _) = await CreateAuthenticatedClientAsync("Admin");
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        var superAdminRole = new Role { Id = Guid.NewGuid(), Name = "SuperAdmin" };
        context.Roles.Add(superAdminRole);
        var superAdmin = await SeedUserAsync(context, superAdminRole.Id, "owner@saharvest.org");

        var response = await client.PatchAsJsonAsync(
            $"/api/v1/users/{superAdmin.Id}/status", new SetUserActiveStatusRequest { IsActive = false });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var reloaded = await context.Users.AsNoTracking().SingleAsync(u => u.Id == superAdmin.Id);
        Assert.True(reloaded.IsActive);
    }

    [Fact]
    public async Task UpdateUser_SuperAdminTargetingSuperAdmin_Succeeds()
    {
        var (client, _, superAdminRole) = await CreateAuthenticatedClientAsync("SuperAdmin");
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        var otherSuperAdmin = await SeedUserAsync(context, superAdminRole.Id, "other-owner@saharvest.org");

        var response = await client.PatchAsJsonAsync(
            $"/api/v1/users/{otherSuperAdmin.Id}",
            new UpdateUserRequest { FirstName = "Renamed", LastName = "Owner", Email = "other-owner@saharvest.org" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateUser_AdminTargetingAdmin_Succeeds()
    {
        var (client, _, adminRole) = await CreateAuthenticatedClientAsync("Admin");
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        var otherAdmin = await SeedUserAsync(context, adminRole.Id, "colleague@saharvest.org");

        var response = await client.PatchAsJsonAsync(
            $"/api/v1/users/{otherAdmin.Id}",
            new UpdateUserRequest { FirstName = "Renamed", LastName = "Colleague", Email = "colleague@saharvest.org" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateUser_EmailIntoReservedSystemDomain_ReturnsBadRequest()
    {
        var (client, _, adminRole) = await CreateAuthenticatedClientAsync("Admin");
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        var target = await SeedUserAsync(context, adminRole.Id, "colleague@saharvest.org");

        var response = await client.PatchAsJsonAsync(
            $"/api/v1/users/{target.Id}",
            new UpdateUserRequest { FirstName = "Seeded", LastName = "User", Email = "sneaky@SYSTEM.local" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateUser_EmailChanged_RevokesRefreshTokensAndNotifiesOldAddress()
    {
        var (superAdminClient, _, _) = await CreateAuthenticatedClientAsync("SuperAdmin");
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        var adminRole = new Role { Id = Guid.NewGuid(), Name = "Admin" };
        context.Roles.Add(adminRole);
        const string password = "TargetPassword123";
        var target = await SeedUserAsync(context, adminRole.Id, "old-address@saharvest.org", password);
        var (targetLogin, targetCookie) = await _factory.LoginForCookieAsync(target.Email, password);
        var targetClient = _factory.CreateClient();
        targetClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", targetLogin.AccessToken);
        Assert.Equal(HttpStatusCode.OK, (await targetClient.GetAsync("/api/v1/users")).StatusCode);

        var response = await superAdminClient.PatchAsJsonAsync(
            $"/api/v1/users/{target.Id}",
            new UpdateUserRequest { FirstName = "Seeded", LastName = "User", Email = "new-address@saharvest.org" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var refreshResponse = await _factory.RefreshWithCookieAsync(targetCookie);
        Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);

        // The already-issued access token dies immediately too, not after its 60 minutes.
        Assert.Equal(HttpStatusCode.Unauthorized, (await targetClient.GetAsync("/api/v1/users")).StatusCode);

        var notice = await context.EmailLogs.AsNoTracking()
            .SingleAsync(e => e.EmailType == EmailType.AccountEmailChanged);
        Assert.Equal("old-address@saharvest.org", notice.ToAddress);
    }

    [Fact]
    public async Task UpdateUser_EmailUnchanged_KeepsRefreshTokensAndSendsNoNotice()
    {
        var (superAdminClient, _, _) = await CreateAuthenticatedClientAsync("SuperAdmin");
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        var adminRole = new Role { Id = Guid.NewGuid(), Name = "Admin" };
        context.Roles.Add(adminRole);
        const string password = "TargetPassword123";
        var target = await SeedUserAsync(context, adminRole.Id, "same-address@saharvest.org", password);
        var (_, targetCookie) = await _factory.LoginForCookieAsync(target.Email, password);

        var response = await superAdminClient.PatchAsJsonAsync(
            $"/api/v1/users/{target.Id}",
            new UpdateUserRequest { FirstName = "Renamed", LastName = "Only", Email = "same-address@saharvest.org" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var refreshResponse = await _factory.RefreshWithCookieAsync(targetCookie);
        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
        Assert.False(await context.EmailLogs.AnyAsync(e => e.EmailType == EmailType.AccountEmailChanged));
    }
}
