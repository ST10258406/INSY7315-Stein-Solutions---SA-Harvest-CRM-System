namespace CRM.API.Tests.Controllers;

using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CRM.Application.Common.Interfaces;
using CRM.Application.Common.Models;
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
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;

/// <summary>
/// POST /api/v1/reports/export end to end: validation → handler dispatching the GET report
/// query → real QuestPDF/ClosedXML rendering → (mocked) blob upload + SAS → AuditBehaviour.
/// Only IBlobStorageService is substituted; there's no Azurite in the test run.
/// </summary>
public class ReportsExportControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string ExportUrl = "/api/v1/reports/export";

    private readonly WebApplicationFactory<Program> _factory;
    private readonly IBlobStorageService _blobStorageMock = Substitute.For<IBlobStorageService>();

    public ReportsExportControllerTests(WebApplicationFactory<Program> factory)
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable("JWT_SECRET", "12345678901234567890123456789012");
        Environment.SetEnvironmentVariable("BREVO_API_KEY", "test-brevo-key");
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", "Host=localhost;Database=fake;Username=postgres;Password=password");
        Environment.SetEnvironmentVariable("Jwt__SigningKey", "12345678901234567890123456789012");
        Environment.SetEnvironmentVariable("Jwt__AccessTokenExpiryMinutes", "60");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "TestIssuer");
        Environment.SetEnvironmentVariable("Jwt__Audience", "TestAudience");

        _blobStorageMock
            .UploadAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(ci => new BlobUploadResult(ci.ArgAt<string>(1), "https://raw.blob/" + ci.ArgAt<string>(1)));
        _blobStorageMock
            .GenerateSasUrlAsync(Arg.Any<string>(), Arg.Any<TimeSpan>())
            .Returns(ci => "https://storage.blob.core.windows.net/" + ci.ArgAt<string>(0) + "?sig=sas");

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
                services.AddDbContext<CrmDbContext>(options => options.UseInMemoryDatabase("InMemoryDbForReportsExportTesting"));

                services.RemoveAll<IBlobStorageService>();
                services.AddSingleton(_blobStorageMock);
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
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = $"test-export-{roleName.ToLower()}@example.com",
            FirstName = roleName,
            LastName = "Test",
            PasswordHash = new PasswordHasher<User>().HashPassword(null!, password),
            UserRoles = new List<UserRole>()
        };
        user.UserRoles.Add(new UserRole { RoleId = role.Id, UserId = user.Id, Role = role, User = user });
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginCommand(user.Email, password));
        var loginResult = JsonSerializer.Deserialize<LoginResponseDto>(
            await login.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResult!.AccessToken);

        return client;
    }

    private static Task<HttpResponseMessage> PostJsonAsync(HttpClient client, string json) =>
        client.PostAsync(ExportUrl, new StringContent(json, Encoding.UTF8, "application/json"));

    private static JsonElement Body(HttpResponseMessage response) =>
        JsonDocument.Parse(response.Content.ReadAsStringAsync().Result).RootElement;

    [Fact]
    public async Task Export_DonorsContactedPdf_Returns200WithSasUrlAndWritesExportedAuditLog()
    {
        var client = await CreateAuthenticatedClient("Admin");
        var before = DateTime.UtcNow;

        var response = await PostJsonAsync(client, """
            {
              "reportType": "donors-contacted",
              "format": "pdf",
              "filters": { "startDate": "2026-07-01", "endDate": "2026-07-31", "relationshipManagerId": null }
            }
            """);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = Body(response).GetProperty("data");

        Assert.Equal("donors-contacted-july-2026.pdf", data.GetProperty("fileName").GetString());
        Assert.True(data.GetProperty("fileSizeBytes").GetInt64() > 0);
        var downloadUrl = data.GetProperty("downloadUrl").GetString()!;
        Assert.StartsWith("https://storage.blob.core.windows.net/reports/donors-contacted/", downloadUrl);
        Assert.EndsWith("/donors-contacted-july-2026.pdf?sig=sas", downloadUrl);
        Assert.InRange(data.GetProperty("expiresAt").GetDateTime().ToUniversalTime(), before.AddMinutes(15), DateTime.UtcNow.AddMinutes(15));

        await _blobStorageMock.Received(1).UploadAsync(
            Arg.Any<Stream>(), Arg.Is<string>(p => p.StartsWith("reports/donors-contacted/")), "application/pdf");
        await _blobStorageMock.Received(1).GenerateSasUrlAsync(
            Arg.Is<string>(p => p.StartsWith("reports/donors-contacted/")), TimeSpan.FromMinutes(15));

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        var audit = await context.AuditLogs.SingleAsync(a => a.Action == AuditAction.Exported);
        Assert.Equal("Report", audit.EntityType);
        Assert.NotNull(audit.UserId);
        Assert.Contains($"/{audit.EntityId}/", downloadUrl);
        Assert.Contains("donors-contacted-july-2026.pdf", audit.NewValues);
    }

    [Fact]
    public async Task Export_DonorsByRegionExcelWithInvalidFilters_IgnoresFiltersAndReturns200()
    {
        var client = await CreateAuthenticatedClient("Admin");

        var response = await PostJsonAsync(client, """
            {
              "reportType": "donors-by-region",
              "format": "excel",
              "filters": { "startDate": "2026-08-01", "endDate": "2026-07-01" }
            }
            """);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var today = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
        Assert.Equal($"donors-by-region-{today}.xlsx", Body(response).GetProperty("data").GetProperty("fileName").GetString());
        await _blobStorageMock.Received(1).UploadAsync(
            Arg.Any<Stream>(),
            Arg.Is<string>(p => p.StartsWith("reports/donors-by-region/")),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }

    [Theory]
    [InlineData("donors-by-type")]
    [InlineData("donors-by-status")]
    public async Task Export_OtherSnapshotReports_Return200(string reportType)
    {
        var client = await CreateAuthenticatedClient("Admin");

        var response = await PostJsonAsync(client, $$"""{ "reportType": "{{reportType}}", "format": "pdf" }""");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.StartsWith(reportType, Body(response).GetProperty("data").GetProperty("fileName").GetString());
    }

    [Theory]
    [InlineData("""{ "reportType": "donors-everywhere", "format": "pdf" }""", "ReportType")]
    [InlineData("""{ "format": "pdf" }""", "ReportType")]
    [InlineData("""{ "reportType": "donors-by-region", "format": "csv" }""", "Format")]
    [InlineData("""{ "reportType": "donors-by-region" }""", "Format")]
    [InlineData("""{ "reportType": "donors-contacted", "format": "pdf" }""", "Filters")]
    [InlineData("""{ "reportType": "donors-contacted", "format": "pdf", "filters": { "endDate": "2026-07-31" } }""", "Filters.StartDate")]
    [InlineData("""{ "reportType": "donors-contacted", "format": "pdf", "filters": { "startDate": "2026-07-01" } }""", "Filters.EndDate")]
    [InlineData("""{ "reportType": "donors-contacted", "format": "pdf", "filters": { "startDate": "2026-08-01", "endDate": "2026-07-31" } }""", "Filters")]
    public async Task Export_InvalidRequest_Returns400ValidationErrorWithoutUploading(string json, string expectedField)
    {
        var client = await CreateAuthenticatedClient("Admin");

        var response = await PostJsonAsync(client, json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = Body(response);
        Assert.Equal("VALIDATION_ERROR", body.GetProperty("code").GetString());
        Assert.Contains(body.GetProperty("errors").EnumerateArray(), e => e.GetProperty("field").GetString() == expectedField);
        await _blobStorageMock.DidNotReceive().UploadAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>());

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        Assert.False(await context.AuditLogs.AnyAsync(a => a.Action == AuditAction.Exported));
    }

    [Fact]
    public async Task Export_NonAdminUser_Forbidden()
    {
        var client = await CreateAuthenticatedClient("Procurement");

        var response = await PostJsonAsync(client, """{ "reportType": "donors-by-region", "format": "pdf" }""");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await _blobStorageMock.DidNotReceive().UploadAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task Export_Unauthenticated_Returns401()
    {
        var response = await PostJsonAsync(_factory.CreateClient(), """{ "reportType": "donors-by-region", "format": "pdf" }""");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
