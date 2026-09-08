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
using Microsoft.Extensions.DependencyInjection;

public class NotificationsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    // Real Postgres, not the in-memory provider: the mark-all-read endpoint uses
    // ExecuteUpdateAsync, which the EF Core in-memory provider does not support.
    private const string TestDbConnection =
        "Host=localhost;Database=crm_test_notifications_api;Username=postgres;Password=P@ss1234ID";

    private readonly WebApplicationFactory<Program> _factory;

    public NotificationsControllerTests(WebApplicationFactory<Program> factory)
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable("JWT_SECRET", "12345678901234567890123456789012");
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", "Host=localhost;Database=fake;Username=postgres;Password=password");
        Environment.SetEnvironmentVariable("Jwt__SigningKey", "12345678901234567890123456789012");
        Environment.SetEnvironmentVariable("Jwt__AccessTokenExpiryMinutes", "60");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "TestIssuer");
        Environment.SetEnvironmentVariable("Jwt__Audience", "TestAudience");

        var conn = Environment.GetEnvironmentVariable("CRM_TEST_POSTGRES_CONNECTION_STRING") ?? TestDbConnection;

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
                services.AddDbContext<CrmDbContext>(options => options.UseNpgsql(conn));
            });
        });
    }

    private sealed record Fixture(HttpClient Client, Guid UserId, Guid OtherUserId, Guid MyUnreadId, Guid OthersId);

    private async Task<Fixture> SetupAsync()
    {
        var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();

        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();

        var role = new Role { Id = Guid.NewGuid(), Name = "Marketing" };
        context.Roles.Add(role);

        const string password = "TestPassword123";
        var hasher = new PasswordHasher<User>();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "notif-user@example.com",
            FirstName = "Nia",
            LastName = "User",
            PasswordHash = hasher.HashPassword(null!, password),
            UserRoles = new List<UserRole>()
        };
        user.UserRoles.Add(new UserRole
        {
            RoleId = role.Id,
            UserId = user.Id,
            Role = role,
            User = user,
            AssignedByUserId = user.Id, // self — real Postgres enforces this FK
            AssignedAt = DateTime.UtcNow
        });
        context.Users.Add(user);

        var other = new User { Id = Guid.NewGuid(), Email = "notif-other@example.com", FirstName = "Otto", LastName = "Other", PasswordHash = "n/a" };
        context.Users.Add(other);

        var now = DateTime.UtcNow;
        var myUnread1 = MakeNotification(user.Id, false, now.AddMinutes(-1));
        var myUnread2 = MakeNotification(user.Id, false, now.AddMinutes(-2));
        var myRead = MakeNotification(user.Id, true, now.AddMinutes(-3));
        var others = MakeNotification(other.Id, false, now);
        context.Notifications.AddRange(myUnread1, myUnread2, myRead, others);

        await context.SaveChangesAsync();

        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginCommand(user.Email, password));
        var loginResult = JsonSerializer.Deserialize<LoginResponseDto>(
            await login.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResult!.AccessToken);

        return new Fixture(client, user.Id, other.Id, myUnread1.Id, others.Id);
    }

    private static Notification MakeNotification(Guid userId, bool isRead, DateTime createdAt) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        Title = "Something happened",
        Message = "Details here",
        NotificationType = NotificationType.TaskAssigned,
        IsRead = isRead,
        ReadAt = isRead ? createdAt.AddMinutes(1) : null,
        CreatedAt = createdAt
    };

    private static JsonElement Body(HttpResponseMessage r) =>
        JsonDocument.Parse(r.Content.ReadAsStringAsync().Result).RootElement;

    [Fact]
    public async Task Get_ReturnsCallersNotifications_NewestFirst_WithUnreadCountOutsideEnvelope()
    {
        var f = await SetupAsync();

        var response = await f.Client.GetAsync("/api/v1/notifications");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = Body(response);
        Assert.Equal(3, json.GetProperty("pagination").GetProperty("totalCount").GetInt32()); // mine only
        Assert.Equal(2, json.GetProperty("unreadCount").GetInt32());

        var rows = json.GetProperty("data").EnumerateArray().Select(e => e.GetProperty("createdAt").GetDateTime()).ToArray();
        Assert.Equal(rows.OrderByDescending(d => d).ToArray(), rows);
    }

    [Fact]
    public async Task Get_IsReadFalse_ReturnsUnreadOnly()
    {
        var f = await SetupAsync();

        var json = Body(await f.Client.GetAsync("/api/v1/notifications?isRead=false"));

        Assert.Equal(2, json.GetProperty("pagination").GetProperty("totalCount").GetInt32());
        foreach (var n in json.GetProperty("data").EnumerateArray())
            Assert.False(n.GetProperty("isRead").GetBoolean());
    }

    [Fact]
    public async Task Get_Unauthenticated_Returns401()
    {
        var response = await _factory.CreateClient().GetAsync("/api/v1/notifications");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task MarkRead_OwnNotification_Returns204_AndFlipsIt()
    {
        var f = await SetupAsync();

        var response = await f.Client.PatchAsync($"/api/v1/notifications/{f.MyUnreadId}/read", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        var n = await context.Notifications.AsNoTracking().SingleAsync(x => x.Id == f.MyUnreadId);
        Assert.True(n.IsRead);
        Assert.NotNull(n.ReadAt);
    }

    [Fact]
    public async Task MarkRead_AnotherUsersNotification_Returns403()
    {
        var f = await SetupAsync();

        var response = await f.Client.PatchAsync($"/api/v1/notifications/{f.OthersId}/read", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        Assert.False((await context.Notifications.AsNoTracking().SingleAsync(x => x.Id == f.OthersId)).IsRead);
    }

    [Fact]
    public async Task MarkRead_UnknownNotification_Returns404()
    {
        var f = await SetupAsync();

        var response = await f.Client.PatchAsync($"/api/v1/notifications/{Guid.NewGuid()}/read", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task MarkAllRead_Returns204_MarksAllMineRead_LeavesOthers()
    {
        var f = await SetupAsync();

        var response = await f.Client.PatchAsync("/api/v1/notifications/read-all", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        Assert.Equal(0, await context.Notifications.AsNoTracking().CountAsync(n => n.UserId == f.UserId && !n.IsRead));
        Assert.True(await context.Notifications.AsNoTracking().AnyAsync(n => n.UserId == f.OtherUserId && !n.IsRead));

        var listAfter = Body(await f.Client.GetAsync("/api/v1/notifications"));
        Assert.Equal(0, listAfter.GetProperty("unreadCount").GetInt32());
    }
}
