namespace CRM.API.Tests.Controllers;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CRM.Application.Modules.Auth.Commands.Login;
using CRM.Application.Modules.Auth.Dtos;
using CRM.Domain.Entities;
using CRM.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Hangfire;

public class PolicyTestControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public PolicyTestControllerTests(WebApplicationFactory<Program> factory)
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable("JWT_SECRET", "12345678901234567890123456789012");
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", "Host=localhost;Database=fake;Username=postgres;Password=password");
        
        Environment.SetEnvironmentVariable("JwtSettings__SigningKey", "12345678901234567890123456789012");
        Environment.SetEnvironmentVariable("JwtSettings__AccessTokenExpiryMinutes", "60");
        Environment.SetEnvironmentVariable("JwtSettings__Issuer", "TestIssuer");
        Environment.SetEnvironmentVariable("JwtSettings__Audience", "TestAudience");
        
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
                    options.UseInMemoryDatabase("InMemoryDbForPolicyTesting");
                });
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

        return client;
    }

    [Fact]
    public async Task MarketingUser_CanAccessMarketing_ButNotProcurement()
    {
        var client = await CreateAuthenticatedClient("Marketing");

        var mktResponse = await client.GetAsync("/api/v1/policy-test/marketing");
        Assert.Equal(HttpStatusCode.OK, mktResponse.StatusCode);

        var procResponse = await client.GetAsync("/api/v1/policy-test/procurement");
        Assert.Equal(HttpStatusCode.Forbidden, procResponse.StatusCode);
    }

    [Fact]
    public async Task AdminUser_CanAccessAdmin_ButNotSuperAdmin()
    {
        var client = await CreateAuthenticatedClient("Admin");

        var adminResponse = await client.GetAsync("/api/v1/policy-test/admin");
        Assert.Equal(HttpStatusCode.OK, adminResponse.StatusCode);

        var superResponse = await client.GetAsync("/api/v1/policy-test/superadmin");
        Assert.Equal(HttpStatusCode.Forbidden, superResponse.StatusCode);
    }

    [Fact]
    public async Task SuperAdminUser_CanAccessEverything()
    {
        var client = await CreateAuthenticatedClient("SuperAdmin");

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/policy-test/marketing")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/policy-test/procurement")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/policy-test/admin")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/policy-test/superadmin")).StatusCode);
    }

    [Fact]
    public async Task Unauthenticated_Gets401()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/policy-test/marketing");
        
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
