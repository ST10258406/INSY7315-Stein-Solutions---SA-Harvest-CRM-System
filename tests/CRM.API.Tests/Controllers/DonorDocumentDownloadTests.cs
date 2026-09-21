namespace CRM.API.Tests.Controllers;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CRM.Application.Common.Interfaces;
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
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;

/// <summary>
/// End-to-end coverage for DocumentTypeAuthorizationFilter — the resource-based
/// role check for GET /donors/{id}/documents/{docId}/download now lives outside
/// the Application handler entirely, so this is the only place that exercises
/// the full route: filter -> mediator -> controller -> AuditBehaviour together.
/// </summary>
public class DonorDocumentDownloadTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly IBlobStorageService _blobStorageMock = Substitute.For<IBlobStorageService>();

    public DonorDocumentDownloadTests(WebApplicationFactory<Program> factory)
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
            .GenerateSasUrlAsync(Arg.Any<string>(), Arg.Any<TimeSpan>())
            .Returns(ci => Task.FromResult("https://storage.blob.core.windows.net/" + ci.ArgAt<string>(0) + "?sv=sas"));

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
                    options.UseInMemoryDatabase("InMemoryDbForDocumentDownloadTesting");
                });

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

    private static async Task<DonorDocument> SeedDonorWithDocumentAsync(CrmDbContext context, DocumentType documentType)
    {
        var companyType = new LookupCompanyType { Id = 1, Name = "Manufacturer", IsActive = true };
        var entityType = new LookupEntityType { Id = 1, Name = "Pty Ltd", IsActive = true };
        var frequency = new LookupDonationFrequency { Id = 1, Name = "Monthly", IsActive = true };
        context.LookupCompanyTypes.Add(companyType);
        context.LookupEntityTypes.Add(entityType);
        context.LookupDonationFrequencies.Add(frequency);

        var creator = new User { Id = Guid.NewGuid(), Email = "creator@example.com", FirstName = "Creator", LastName = "User", PasswordHash = "n/a" };
        context.Users.Add(creator);

        var donor = new Donor
        {
            Id = Guid.NewGuid(),
            CompanyName = "FoodCorp SA",
            CompanyTypeId = companyType.Id,
            RegisteredCompanyName = "FoodCorp (Pty) Ltd",
            EntityTypeId = entityType.Id,
            DonationFrequencyId = frequency.Id,
            Status = DonorStatus.Active,
            SubmissionSource = SubmissionSource.ManualCapture,
            CreatedByUserId = creator.Id
        };
        context.Donors.Add(donor);

        var document = new DonorDocument
        {
            Id = Guid.NewGuid(),
            DonorId = donor.Id,
            DocumentType = documentType,
            FileName = "file.pdf",
            BlobStoragePath = $"donors/{donor.Id}/{documentType}/file.pdf",
            IsActive = true
        };
        context.DonorDocuments.Add(document);

        await context.SaveChangesAsync();

        return document;
    }

    [Fact]
    public async Task Download_ProcurementRequestingSignature_Returns200()
    {
        var client = await CreateAuthenticatedClient("Procurement");
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        var document = await SeedDonorWithDocumentAsync(context, DocumentType.Signature);

        var response = await client.GetAsync($"/api/v1/donors/{document.DonorId}/documents/{document.Id}/download");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Download_ProcurementRequestingBBBEECertificate_Returns403()
    {
        var client = await CreateAuthenticatedClient("Procurement");
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        var document = await SeedDonorWithDocumentAsync(context, DocumentType.BBBEECertificate);

        var response = await client.GetAsync($"/api/v1/donors/{document.DonorId}/documents/{document.Id}/download");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content).RootElement;
        Assert.Equal("FORBIDDEN", json.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Download_AdminRequestingBBBEECertificate_Returns200AndWritesViewedAuditLog()
    {
        var client = await CreateAuthenticatedClient("Admin");
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        var document = await SeedDonorWithDocumentAsync(context, DocumentType.BBBEECertificate);

        var response = await client.GetAsync($"/api/v1/donors/{document.DonorId}/documents/{document.Id}/download");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var auditEntry = await context.AuditLogs.SingleAsync(a => a.EntityId == document.Id);
        Assert.Equal(AuditAction.Viewed, auditEntry.Action);
        Assert.Equal(nameof(DonorDocument), auditEntry.EntityType);
    }

    [Fact]
    public async Task Download_NonExistentDocument_Returns404()
    {
        var client = await CreateAuthenticatedClient("Admin");

        var response = await client.GetAsync($"/api/v1/donors/{Guid.NewGuid()}/documents/{Guid.NewGuid()}/download");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
