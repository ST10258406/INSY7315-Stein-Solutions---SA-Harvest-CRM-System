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

public class TasksControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public TasksControllerTests(WebApplicationFactory<Program> factory)
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
                services.AddDbContext<CrmDbContext>(options => options.UseInMemoryDatabase("InMemoryDbForTasksTesting"));
            });
        });
    }

    private sealed record Fixture(
        HttpClient Client, Guid LoginUserId, Guid OtherUserId, Guid DonorId, Guid OtherDonorId,
        Guid MyOpenTaskId, Guid MyCompletedTaskId);

    private async Task<Fixture> SetupAsync(string roleName)
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
        var loginUser = new User
        {
            Id = Guid.NewGuid(),
            Email = $"test-{roleName.ToLower()}@example.com",
            FirstName = roleName,
            LastName = "Test",
            PasswordHash = hasher.HashPassword(null!, password),
            UserRoles = new List<UserRole>()
        };
        loginUser.UserRoles.Add(new UserRole { RoleId = role.Id, UserId = loginUser.Id, Role = role, User = loginUser });
        context.Users.Add(loginUser);

        var otherUser = new User { Id = Guid.NewGuid(), Email = "other@example.com", FirstName = "Other", LastName = "User", PasswordHash = "n/a" };
        context.Users.Add(otherUser);

        context.LookupCompanyTypes.Add(new LookupCompanyType { Id = 1, Name = "Manufacturer", IsActive = true });
        context.LookupEntityTypes.Add(new LookupEntityType { Id = 1, Name = "Pty Ltd", IsActive = true });
        context.LookupDonationFrequencies.Add(new LookupDonationFrequency { Id = 1, Name = "Monthly", IsActive = true });

        var donor = MakeDonor("FoodCorp SA", loginUser.Id);
        var otherDonor = MakeDonor("Other Co", loginUser.Id);
        context.Donors.AddRange(donor, otherDonor);

        var today = DateTime.UtcNow.Date;
        var myOpen = MakeTask(donor.Id, loginUser.Id, loginUser.Id, today.AddDays(2));                  // mine, open
        var myCompleted = MakeTask(donor.Id, loginUser.Id, loginUser.Id, today.AddDays(10), completed: true); // mine, completed
        context.DonorTasks.AddRange(
            myOpen,
            myCompleted,
            MakeTask(donor.Id, otherUser.Id, loginUser.Id, today.AddDays(1)),                       // other's, open, same donor
            MakeTask(otherDonor.Id, loginUser.Id, loginUser.Id, today.AddDays(3)));                 // mine, open, other donor

        await context.SaveChangesAsync();

        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginCommand(loginUser.Email, password));
        var loginResult = JsonSerializer.Deserialize<LoginResponseDto>(
            await login.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResult!.AccessToken);

        return new Fixture(client, loginUser.Id, otherUser.Id, donor.Id, otherDonor.Id, myOpen.Id, myCompleted.Id);
    }

    private static Donor MakeDonor(string name, Guid creatorId) => new()
    {
        Id = Guid.NewGuid(),
        CompanyName = name,
        CompanyTypeId = 1,
        RegisteredCompanyName = name + " (Pty) Ltd",
        EntityTypeId = 1,
        DonationFrequencyId = 1,
        Status = DonorStatus.Active,
        SubmissionSource = SubmissionSource.ManualCapture,
        CreatedByUserId = creatorId
    };

    private static DonorTask MakeTask(Guid donorId, Guid assignee, Guid creator, DateTime due, bool completed = false) => new()
    {
        Id = Guid.NewGuid(),
        DonorId = donorId,
        Title = "Follow up",
        DueDate = due,
        IsCompleted = completed,
        AssignedToUserId = assignee,
        CreatedByUserId = creator,
        CompletedByUserId = completed ? creator : null
    };

    private static JsonElement Body(HttpResponseMessage r) =>
        JsonDocument.Parse(r.Content.ReadAsStringAsync().Result).RootElement;

    [Fact]
    public async Task GetMyTasks_ReturnsOnlyCallersOpenTasks_ByDefault()
    {
        var f = await SetupAsync("Marketing");

        var response = await f.Client.GetAsync("/api/v1/tasks");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = Body(response);
        // mine + open = 2 (one same donor, one other donor); the completed one and the other user's are excluded
        Assert.Equal(2, json.GetProperty("pagination").GetProperty("totalCount").GetInt32());
        foreach (var t in json.GetProperty("data").EnumerateArray())
        {
            Assert.False(t.GetProperty("isCompleted").GetBoolean());
            Assert.Equal(f.LoginUserId, t.GetProperty("assignedTo").GetProperty("id").GetGuid());
        }
    }

    [Fact]
    public async Task GetMyTasks_IsCompletedTrue_ReturnsCallersCompletedTasks()
    {
        var f = await SetupAsync("Marketing");

        var response = await f.Client.GetAsync("/api/v1/tasks?isCompleted=true");

        var json = Body(response);
        Assert.Equal(1, json.GetProperty("pagination").GetProperty("totalCount").GetInt32());
        Assert.True(json.GetProperty("data")[0].GetProperty("isCompleted").GetBoolean());
    }

    [Fact]
    public async Task GetMyTasks_DonorIdFilter_NarrowsToThatDonor()
    {
        var f = await SetupAsync("Marketing");

        var response = await f.Client.GetAsync($"/api/v1/tasks?donorId={f.OtherDonorId}");

        var json = Body(response);
        Assert.Equal(1, json.GetProperty("pagination").GetProperty("totalCount").GetInt32());
        Assert.Equal(f.OtherDonorId, json.GetProperty("data")[0].GetProperty("donor").GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task GetMyTasks_Unauthenticated_Returns401()
    {
        var response = await _factory.CreateClient().GetAsync("/api/v1/tasks");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetDonorTasks_ReturnsAllAssigneesTasksForThatDonor_DefaultAll()
    {
        var f = await SetupAsync("Procurement");

        var response = await f.Client.GetAsync($"/api/v1/donors/{f.DonorId}/tasks");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = Body(response);
        // donor has 3 tasks total (2 mine incl. completed, 1 other user's)
        Assert.Equal(3, json.GetProperty("pagination").GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task GetDonorTasks_IsCompletedFalse_ExcludesCompleted()
    {
        var f = await SetupAsync("Procurement");

        var response = await f.Client.GetAsync($"/api/v1/donors/{f.DonorId}/tasks?isCompleted=false");

        var json = Body(response);
        Assert.Equal(2, json.GetProperty("pagination").GetProperty("totalCount").GetInt32());
        foreach (var t in json.GetProperty("data").EnumerateArray())
            Assert.False(t.GetProperty("isCompleted").GetBoolean());
    }

    [Fact]
    public async Task GetDonorTasks_UnknownDonor_Returns404Envelope()
    {
        var f = await SetupAsync("Procurement");

        var response = await f.Client.GetAsync($"/api/v1/donors/{Guid.NewGuid()}/tasks");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var json = Body(response);
        Assert.Equal(404, json.GetProperty("status").GetInt32());
        Assert.Equal("NOT_FOUND", json.GetProperty("code").GetString());
    }

    [Fact]
    public async Task GetDonorTasks_BadIsCompletedValue_Returns400()
    {
        var f = await SetupAsync("Procurement");

        var response = await f.Client.GetAsync($"/api/v1/donors/{f.DonorId}/tasks?isCompleted=maybe");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetDonorTasks_MarketingUser_Forbidden()
    {
        var f = await SetupAsync("Marketing");

        var response = await f.Client.GetAsync($"/api/v1/donors/{f.DonorId}/tasks");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateDonorTask_Valid_Returns201AndAppearsInTheDonorFeed()
    {
        var f = await SetupAsync("Procurement");
        var due = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(6);

        var post = await f.Client.PostAsJsonAsync($"/api/v1/donors/{f.DonorId}/tasks", new
        {
            title = "Confirm August pickup",
            description = "Call the warehouse manager",
            assignedToUserId = f.OtherUserId,
            dueDate = due.ToString("yyyy-MM-dd")
        });

        Assert.Equal(HttpStatusCode.Created, post.StatusCode);
        var created = Body(post).GetProperty("data");
        Assert.Equal("Confirm August pickup", created.GetProperty("title").GetString());
        Assert.Equal(f.OtherUserId, created.GetProperty("assignedTo").GetProperty("id").GetGuid());
        Assert.False(created.GetProperty("isCompleted").GetBoolean());

        var list = Body(await f.Client.GetAsync($"/api/v1/donors/{f.DonorId}/tasks"));
        Assert.Equal(4, list.GetProperty("pagination").GetProperty("totalCount").GetInt32()); // 3 seeded + 1 new
    }

    [Fact]
    public async Task CreateDonorTask_NotifiesTheAssignee()
    {
        var f = await SetupAsync("Procurement");

        await f.Client.PostAsJsonAsync($"/api/v1/donors/{f.DonorId}/tasks", new
        {
            title = "Ping assignee",
            assignedToUserId = f.OtherUserId,
            dueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(2).ToString("yyyy-MM-dd")
        });

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        var notification = await context.Notifications.AsNoTracking().SingleAsync(n => n.UserId == f.OtherUserId);
        Assert.Equal(NotificationType.TaskAssigned, notification.NotificationType);
        Assert.Equal("DonorTask", notification.RelatedEntityType);
    }

    [Fact]
    public async Task CreateDonorTask_PastDueDate_Returns400()
    {
        var f = await SetupAsync("Procurement");

        var post = await f.Client.PostAsJsonAsync($"/api/v1/donors/{f.DonorId}/tasks", new
        {
            title = "Too late",
            assignedToUserId = f.OtherUserId,
            dueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1).ToString("yyyy-MM-dd")
        });

        Assert.Equal(HttpStatusCode.BadRequest, post.StatusCode);
    }

    [Fact]
    public async Task CreateDonorTask_UnknownDonor_Returns404()
    {
        var f = await SetupAsync("Procurement");

        var post = await f.Client.PostAsJsonAsync($"/api/v1/donors/{Guid.NewGuid()}/tasks", new
        {
            title = "x",
            assignedToUserId = f.OtherUserId,
            dueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(2).ToString("yyyy-MM-dd")
        });

        Assert.Equal(HttpStatusCode.NotFound, post.StatusCode);
    }

    [Fact]
    public async Task CreateDonorTask_UnknownAssignedUser_Returns404()
    {
        var f = await SetupAsync("Procurement");

        var post = await f.Client.PostAsJsonAsync($"/api/v1/donors/{f.DonorId}/tasks", new
        {
            title = "x",
            assignedToUserId = Guid.NewGuid(),
            dueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(2).ToString("yyyy-MM-dd")
        });

        Assert.Equal(HttpStatusCode.NotFound, post.StatusCode);
    }

    [Fact]
    public async Task CreateDonorTask_MarketingUser_Forbidden()
    {
        var f = await SetupAsync("Marketing");

        var post = await f.Client.PostAsJsonAsync($"/api/v1/donors/{f.DonorId}/tasks", new
        {
            title = "x",
            assignedToUserId = f.OtherUserId,
            dueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(2).ToString("yyyy-MM-dd")
        });

        Assert.Equal(HttpStatusCode.Forbidden, post.StatusCode);
    }

    // ---- PATCH /tasks/{id} ---------------------------------------------------

    [Fact]
    public async Task PatchTask_PartialUpdate_ChangesOnlyProvidedFields()
    {
        var f = await SetupAsync("Procurement");

        var patch = await f.Client.PatchAsJsonAsync($"/api/v1/tasks/{f.MyOpenTaskId}", new { title = "Renamed task" });

        Assert.Equal(HttpStatusCode.OK, patch.StatusCode);
        var data = Body(patch).GetProperty("data");
        Assert.Equal("Renamed task", data.GetProperty("title").GetString());
        Assert.False(data.GetProperty("isCompleted").GetBoolean());
    }

    [Fact]
    public async Task PatchTask_ReassignToUnknownUser_Returns404()
    {
        var f = await SetupAsync("Procurement");

        var patch = await f.Client.PatchAsJsonAsync($"/api/v1/tasks/{f.MyOpenTaskId}", new { assignedToUserId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.NotFound, patch.StatusCode);
    }

    [Fact]
    public async Task PatchTask_PastDueDate_Returns400()
    {
        var f = await SetupAsync("Procurement");

        var patch = await f.Client.PatchAsJsonAsync($"/api/v1/tasks/{f.MyOpenTaskId}", new
        {
            dueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-2).ToString("yyyy-MM-dd")
        });

        Assert.Equal(HttpStatusCode.BadRequest, patch.StatusCode);
    }

    [Fact]
    public async Task PatchTask_UnknownTask_Returns404()
    {
        var f = await SetupAsync("Procurement");

        var patch = await f.Client.PatchAsJsonAsync($"/api/v1/tasks/{Guid.NewGuid()}", new { title = "x" });

        Assert.Equal(HttpStatusCode.NotFound, patch.StatusCode);
    }

    [Fact]
    public async Task PatchTask_MarketingUser_Forbidden()
    {
        var f = await SetupAsync("Marketing");

        var patch = await f.Client.PatchAsJsonAsync($"/api/v1/tasks/{f.MyOpenTaskId}", new { title = "x" });

        Assert.Equal(HttpStatusCode.Forbidden, patch.StatusCode);
    }

    // ---- POST /tasks/{id}/complete ----------------------------------------

    [Fact]
    public async Task CompleteTask_OpenTask_AnyAuthenticatedUserCan_SetsCompletionFields()
    {
        var f = await SetupAsync("Marketing"); // deliberately the lowest role

        var response = await f.Client.PostAsync($"/api/v1/tasks/{f.MyOpenTaskId}/complete", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        var task = await context.DonorTasks.AsNoTracking().SingleAsync(t => t.Id == f.MyOpenTaskId);
        Assert.True(task.IsCompleted);
        Assert.Equal(f.LoginUserId, task.CompletedByUserId);
        Assert.NotNull(task.CompletedAt);
    }

    [Fact]
    public async Task CompleteTask_AlreadyComplete_Returns400()
    {
        var f = await SetupAsync("Procurement");

        var response = await f.Client.PostAsync($"/api/v1/tasks/{f.MyCompletedTaskId}/complete", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CompleteTask_UnknownTask_Returns404()
    {
        var f = await SetupAsync("Procurement");

        var response = await f.Client.PostAsync($"/api/v1/tasks/{Guid.NewGuid()}/complete", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CompleteTask_Unauthenticated_Returns401()
    {
        var response = await _factory.CreateClient().PostAsync($"/api/v1/tasks/{Guid.NewGuid()}/complete", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---- POST /tasks/{id}/reopen ----------------------------------------

    [Fact]
    public async Task ReopenTask_CompletedTask_Procurement_ClearsCompletion()
    {
        var f = await SetupAsync("Procurement");

        var response = await f.Client.PostAsync($"/api/v1/tasks/{f.MyCompletedTaskId}/reopen", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        var task = await context.DonorTasks.AsNoTracking().SingleAsync(t => t.Id == f.MyCompletedTaskId);
        Assert.False(task.IsCompleted);
        Assert.Null(task.CompletedAt);
        Assert.Null(task.CompletedByUserId);
    }

    [Fact]
    public async Task ReopenTask_NotCompleted_Returns400()
    {
        var f = await SetupAsync("Procurement");

        var response = await f.Client.PostAsync($"/api/v1/tasks/{f.MyOpenTaskId}/reopen", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ReopenTask_MarketingUser_Forbidden_EvenThoughCompleteIsOpenToAll()
    {
        var f = await SetupAsync("Marketing");

        var response = await f.Client.PostAsync($"/api/v1/tasks/{f.MyCompletedTaskId}/reopen", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
