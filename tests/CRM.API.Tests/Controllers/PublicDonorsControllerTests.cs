namespace CRM.API.Tests.Controllers;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CRM.API.Controllers;
using CRM.API.Extensions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Common.Models;
using CRM.Domain.Constants;
using CRM.Domain.Entities;
using CRM.Domain.Entities.Lookups;
using CRM.Domain.Enums;
using CRM.Infrastructure.Persistence;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;

public class PublicDonorsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string ValidPngBase64 =
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUAAScy0NkAAAAASUVORK5CYII=";
    private const string ValidSignatureDataUri = $"data:image/png;base64,{ValidPngBase64}";

    private readonly WebApplicationFactory<Program> _factory;
    private readonly IBlobStorageService _blobStorageMock = Substitute.For<IBlobStorageService>();

    public PublicDonorsControllerTests(WebApplicationFactory<Program> factory)
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable("JWT_SECRET", "12345678901234567890123456789012");
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", "Host=localhost;Database=fake;Username=postgres;Password=password");

        Environment.SetEnvironmentVariable("Jwt__SigningKey", "12345678901234567890123456789012");
        Environment.SetEnvironmentVariable("Jwt__AccessTokenExpiryMinutes", "60");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "TestIssuer");
        Environment.SetEnvironmentVariable("Jwt__Audience", "TestAudience");

        _blobStorageMock.UploadAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(new BlobUploadResult("some/path", "https://blob.example/some/path"));

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
                    options.UseInMemoryDatabase("InMemoryDbForPublicDonorsTesting");
                });

                services.RemoveAll<IBlobStorageService>();
                services.AddSingleton(_blobStorageMock);
            });
        });
    }

    /// <summary>
    /// Program.cs skips DatabaseSeeder entirely when ASPNETCORE_ENVIRONMENT is
    /// "Testing" (see Program.cs), so — unlike a real environment — the system
    /// user SubmitPublicDonorCommandHandler depends on doesn't exist unless this
    /// fixture creates it itself, exactly like CreateDonorControllerTests seeds
    /// its own roles/admin user by hand.
    /// </summary>
    private static async Task SeedFixtureDataAsync(CrmDbContext context, int adminCount = 1)
    {
        context.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Email = SystemUsers.PublicFormEmail,
            FirstName = "Public",
            LastName = "Form",
            PasswordHash = "unusable",
            IsActive = false
        });

        if (adminCount > 0)
        {
            var adminRole = new Role { Id = Guid.NewGuid(), Name = "Admin" };
            context.Roles.Add(adminRole);

            for (var i = 0; i < adminCount; i++)
            {
                var admin = new User
                {
                    Id = Guid.NewGuid(),
                    Email = $"admin{i}@example.com",
                    FirstName = "Admin",
                    LastName = $"{i}",
                    PasswordHash = "hash",
                    IsActive = true
                };
                admin.UserRoles.Add(new UserRole { RoleId = adminRole.Id, UserId = admin.Id, Role = adminRole, User = admin });
                context.Users.Add(admin);
            }
        }

        context.LookupCompanyTypes.Add(new LookupCompanyType { Id = 1, Name = "Manufacturer", IsActive = true });
        context.LookupEntityTypes.Add(new LookupEntityType { Id = 1, Name = "Private Company", IsActive = true });
        context.LookupDonationFrequencies.Add(new LookupDonationFrequency { Id = 1, Name = "Monthly", IsActive = true });
        context.LookupProvinces.Add(new LookupProvince { Id = 3, Code = "GP", Name = "Gauteng", IsActive = true });
        context.LookupOperationalRegions.Add(new LookupOperationalRegion { Id = 1, Code = "JHB", Name = "Johannesburg", IsActive = true });
        context.LookupDonationTypes.Add(new LookupDonationType { Id = 1, Name = "Meat", IsActive = true });

        await context.SaveChangesAsync();
    }

    private static object MakeValidPayload(string incomeTaxNumber = "9012345678", string signatureImageBase64 = ValidSignatureDataUri) => new
    {
        company = new
        {
            companyName = "Test Donor Pty Ltd",
            companyTypeId = 1,
            website = "https://test.co.za",
            registeredCompanyName = "Test Donor (Pty) Ltd",
            tradingName = "Test Donor",
            entityTypeId = 1,
            companyRegistrationNumber = "2020/000000/07",
            incomeTaxNumber
        },
        primaryContact = new { name = "Jane Tester", phone = "+27820000000", email = "jane@test.co.za" },
        legalAddress = new
        {
            streetAddress = "1 Test Street",
            suburb = "Testville",
            city = "Johannesburg",
            provinceId = 3,
            postalCode = "2000"
        },
        donations = new
        {
            frequencyId = 1,
            typeIds = new[] { 1 },
            collectionAddress = "Gate 1",
            regionIds = new[] { 1 }
        },
        signature = new { imageBase64 = signatureImageBase64 }
    };

    [Fact]
    public async Task Submit_ValidPayload_Returns201WithMessageAndReferenceNumber_NeverTheDonorId()
    {
        var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
        await SeedFixtureDataAsync(context);

        var response = await client.PostAsJsonAsync("/api/v1/public/donors/submit", MakeValidPayload());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var raw = await response.Content.ReadAsStringAsync();
        var root = JsonDocument.Parse(raw).RootElement;

        Assert.Equal(
            "Thank you. Your submission has been received and is currently under review.",
            root.GetProperty("message").GetString());
        Assert.Matches(@"^DON-\d{4}-\d{5,}$", root.GetProperty("referenceNumber").GetString()!);
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("submissionToken").GetString()));

        // Exactly these three top-level fields — nothing else, and specifically
        // no "id"/"donorId" property anywhere in the response body.
        var propertyNames = root.EnumerateObject().Select(p => p.Name).ToArray();
        Assert.Equal(["message", "referenceNumber", "submissionToken"], propertyNames.OrderBy(x => x));
        Assert.DoesNotContain("id", raw, StringComparison.OrdinalIgnoreCase);

        var donor = await context.Donors.SingleAsync();
        Assert.NotEqual(donor.Id.ToString(), root.GetProperty("submissionToken").GetString());
    }

    [Fact]
    public async Task Submit_ValidPayload_CreatesAllFiveSideEffects_OneNotificationPerAdmin()
    {
        var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
        await SeedFixtureDataAsync(context, adminCount: 2);

        var response = await client.PostAsJsonAsync("/api/v1/public/donors/submit", MakeValidPayload());
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        // 1. Donor — PendingReview, PublicForm
        var donor = await context.Donors.SingleAsync();
        Assert.Equal(DonorStatus.PendingReview, donor.Status);
        Assert.Equal(SubmissionSource.PublicForm, donor.SubmissionSource);

        // 2. Signature document
        var document = await context.DonorDocuments.SingleAsync(d => d.DonorId == donor.Id);
        Assert.Equal(DocumentType.Signature, document.DocumentType);

        // 3. FormSubmission interaction log
        var log = await context.InteractionLogs.SingleAsync(l => l.DonorId == donor.Id);
        Assert.Equal(InteractionType.FormSubmission, log.InteractionType);

        // 4. Pending approval
        var approval = await context.DonorApprovals.SingleAsync(a => a.DonorId == donor.Id);
        Assert.Equal(ApprovalStatus.Pending, approval.Status);

        // 5. One notification per admin (2 admins seeded), not just one total
        var notifications = await context.Notifications
            .Where(n => n.RelatedEntityId == donor.Id && n.NotificationType == NotificationType.NewDonorPendingReview)
            .ToListAsync();
        Assert.Equal(2, notifications.Count);
        Assert.Equal(2, notifications.Select(n => n.UserId).Distinct().Count());
    }

    [Fact]
    public async Task Submit_InvalidSignature_Returns400WithStandardEnvelope()
    {
        var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
        await SeedFixtureDataAsync(context);

        var response = await client.PostAsJsonAsync(
            "/api/v1/public/donors/submit",
            MakeValidPayload(signatureImageBase64: "data:image/png;base64,not-valid-base64!!!"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var root = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(400, root.GetProperty("status").GetInt32());
        Assert.Equal("VALIDATION_ERROR", root.GetProperty("code").GetString());
        Assert.True(root.TryGetProperty("traceId", out _));

        Assert.Empty(context.Donors);
    }

    [Fact]
    public async Task Submit_MissingCompany_Returns400()
    {
        var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
        await SeedFixtureDataAsync(context);

        var payload = new
        {
            primaryContact = new { name = "Jane Tester", phone = "+27820000000", email = "jane@test.co.za" },
            legalAddress = new { streetAddress = "1 Test Street", suburb = "Testville", city = "Johannesburg", provinceId = 3, postalCode = "2000" },
            donations = new { frequencyId = 1, typeIds = new[] { 1 }, collectionAddress = "Gate 1", regionIds = new[] { 1 } },
            signature = new { imageBase64 = ValidSignatureDataUri }
        };

        var response = await client.PostAsJsonAsync("/api/v1/public/donors/submit", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void SubmitAction_IsAnonymousAndCarriesThePublicSubmitRateLimitPolicy()
    {
        // Static/reflection check rather than firing real requests — this endpoint's
        // rate limit is already exercised end-to-end by RateLimitTestControllerTests
        // (same named policy); this proves THIS action is actually wearing it,
        // without burning its 10-requests-per-hour budget across test runs.
        var method = typeof(PublicDonorsController).GetMethod(nameof(PublicDonorsController.Submit))!;

        var rateLimitAttribute = method.GetCustomAttributes(typeof(EnableRateLimitingAttribute), inherit: true)
            .Cast<EnableRateLimitingAttribute>().SingleOrDefault();
        Assert.NotNull(rateLimitAttribute);
        Assert.Equal(RateLimitingExtensions.PublicSubmitPolicy, rateLimitAttribute!.PolicyName);

        var controllerAllowsAnonymous = typeof(PublicDonorsController)
            .GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true).Any();
        Assert.True(controllerAllowsAnonymous);
    }
}
