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

    private sealed record Fixture(HttpClient Client, Guid LoginUserId, Guid OtherUserId, Guid DonorId, Guid OtherDonorId);

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
        context.DonorTasks.AddRange(
            MakeTask(donor.Id, loginUser.Id, loginUser.Id, today.AddDays(2)),                       // mine, open
            MakeTask(donor.Id, loginUser.Id, loginUser.Id, today.AddDays(10), completed: true),     // mine, completed
            MakeTask(donor.Id, otherUser.Id, loginUser.Id, today.AddDays(1)),                       // other's, open, same donor
            MakeTask(otherDonor.Id, loginUser.Id, loginUser.Id, today.AddDays(3)));                 // mine, open, other donor

        await context.SaveChangesAsync();

        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginCommand(loginUser.Email, password));
        var loginResult = JsonSerializer.Deserialize<LoginResponseDto>(
            await login.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResult!.AccessToken);

        return new Fixture(client, loginUser.Id, otherUser.Id, donor.Id, otherDonor.Id);
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
}
