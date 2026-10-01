namespace CRM.API.Tests.Controllers;

using System.Net;
using System.Net.Http.Json;
using System.Text;
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
using CRM.Infrastructure.Services;
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
        Environment.SetEnvironmentVariable("BREVO_API_KEY", "test-brevo-key");
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

                // Swap the real network call out from under EmailService so
                // submitting the form never hits the actual Brevo API in tests —
                // EmailService's own logging/EmailLog behaviour still runs for real,
                // which is what Submit_ValidPayload_SendsOnboardingConfirmationEmail
                // below relies on.
                services.AddHttpClient<IEmailService, EmailService>()
                    .ConfigurePrimaryHttpMessageHandler(() => new FakeBrevoHttpMessageHandler());
            });
        });
    }

    private class FakeBrevoHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(new { messageId = "fake-brevo-message-id" }),
                    Encoding.UTF8,
                    "application/json")
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

    private async Task<(HttpClient Client, CrmDbContext Context)> NewPublicClientAsync()
    {
        var client = _factory.CreateClient();
        var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
        await SeedFixtureDataAsync(context);
        return (client, context);
    }

    private static System.Text.Json.Nodes.JsonObject MakeValidPayloadNode() =>
        (System.Text.Json.Nodes.JsonObject)System.Text.Json.Nodes.JsonNode.Parse(JsonSerializer.Serialize(MakeValidPayload()))!;

    [Fact]
    public async Task Submit_CompanyNameOver255Characters_Returns400NotServerError()
    {
        var (client, _) = await NewPublicClientAsync();
        var body = MakeValidPayloadNode();
        body["company"]!["companyName"] = new string('a', 300);

        var response = await client.PostAsJsonAsync("/api/v1/public/donors/submit", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("CompanyName", await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Submit_500TypeIds_Returns400()
    {
        var (client, _) = await NewPublicClientAsync();
        var body = MakeValidPayloadNode();
        body["donations"]!["typeIds"] = JsonSerializer.SerializeToNode(Enumerable.Range(1, 500).ToArray());

        var response = await client.PostAsJsonAsync("/api/v1/public/donors/submit", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html;base64,AAAA")]
    [InlineData("/relative")]
    public async Task Submit_NonHttpWebsite_Returns400(string website)
    {
        var (client, _) = await NewPublicClientAsync();
        var body = MakeValidPayloadNode();
        body["company"]!["website"] = website;

        var response = await client.PostAsJsonAsync("/api/v1/public/donors/submit", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Submit_StaffOnlyCrmFieldsInBody_AreIgnored()
    {
        var (client, context) = await NewPublicClientAsync();
        var staffId = (await context.Users.FirstAsync()).Id;
        var body = MakeValidPayloadNode();
        body["crm"] = new System.Text.Json.Nodes.JsonObject
        {
            ["relationshipManagerId"] = staffId.ToString(),
            ["additionalInformation"] = "injected by anonymous caller",
            ["marketingConsent"] = true
        };

        var response = await client.PostAsJsonAsync("/api/v1/public/donors/submit", body);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var donor = await context.Donors.AsNoTracking().SingleAsync();
        Assert.Null(donor.RelationshipManagerId);
        Assert.Null(donor.AdditionalInformation);
        Assert.True(donor.MarketingConsent);
    }

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
    public async Task Submit_ValidPayload_SendsOnboardingConfirmationEmail_ExactlyOneEmailLogAndNoInteractionLogForIt()
    {
        var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
        await SeedFixtureDataAsync(context);

        var response = await client.PostAsJsonAsync("/api/v1/public/donors/submit", MakeValidPayload());
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var donor = await context.Donors.SingleAsync();

        var emailLog = await context.EmailLogs.SingleAsync();
        Assert.Equal(EmailType.OnboardingConfirmation, emailLog.EmailType);
        Assert.Equal(EmailStatus.Sent, emailLog.Status);
        Assert.Equal("jane@test.co.za", emailLog.ToAddress);
        Assert.Equal(donor.Id, emailLog.DonorId);
        Assert.Equal("fake-brevo-message-id", emailLog.ProviderMessageId);

        // The FormSubmission log from the submission itself is the only
        // InteractionLog row — the confirmation email does not add a second one.
        var interactionLog = await context.InteractionLogs.SingleAsync(l => l.DonorId == donor.Id);
        Assert.Equal(InteractionType.FormSubmission, interactionLog.InteractionType);
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

    [Fact]
    public void SubmitDocumentAction_CarriesThePublicSubmitRateLimitPolicy_SameBucketAsSubmit()
    {
        // Issue's recommendation: submit and submit/document share one bucket
        // (10/hour/IP) since a legitimate donor only calls each once per attempt.
        var method = typeof(PublicDonorsController).GetMethod(nameof(PublicDonorsController.SubmitDocument))!;

        var rateLimitAttribute = method.GetCustomAttributes(typeof(EnableRateLimitingAttribute), inherit: true)
            .Cast<EnableRateLimitingAttribute>().SingleOrDefault();
        Assert.NotNull(rateLimitAttribute);
        Assert.Equal(RateLimitingExtensions.PublicSubmitPolicy, rateLimitAttribute!.PolicyName);
    }

    private static MultipartFormDataContent MakeDocumentUploadForm(
        string sessionToken, string documentType, byte[]? fileBytes = null, string contentType = "application/pdf", string fileName = "cert.pdf")
    {
        var content = new MultipartFormDataContent
        {
            { new StringContent(sessionToken), "sessionToken" },
            { new StringContent(documentType), "documentType" }
        };

        var fileContent = new ByteArrayContent(fileBytes ?? "%PDF-1.7 test"u8.ToArray());
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        content.Add(fileContent, "file", fileName);

        return content;
    }

    private async Task<(CrmDbContext Context, Donor Donor)> SeedPendingDonorWithTokenAsync(
        string token, DateTimeOffset? expiresAt = null)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();

        var donor = new Donor
        {
            Id = Guid.NewGuid(),
            CompanyName = "Doc Test Pty Ltd",
            RegisteredCompanyName = "Doc Test Pty Ltd",
            ReferenceNumber = $"DON-{DateTime.UtcNow.Year}-{Random.Shared.Next(1, 99999):D5}",
            CompanyTypeId = 1,
            EntityTypeId = 1,
            DonationFrequencyId = 1,
            CollectionAddress = "Gate 1",
            Status = DonorStatus.PendingReview,
            SubmissionSource = SubmissionSource.PublicForm,
            SubmissionToken = token,
            SubmissionTokenExpiresAt = expiresAt ?? DateTimeOffset.UtcNow.AddMinutes(30),
            CreatedByUserId = (await context.Users.FirstAsync()).Id
        };
        context.Donors.Add(donor);
        await context.SaveChangesAsync();

        return (context, donor);
    }

    [Fact]
    public async Task SubmitDocument_ValidToken_Returns201AndCreatesBbbeeCertificateDocument()
    {
        var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var seedContext = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        await seedContext.Database.EnsureDeletedAsync();
        await seedContext.Database.EnsureCreatedAsync();
        await SeedFixtureDataAsync(seedContext);

        var (_, donor) = await SeedPendingDonorWithTokenAsync("valid-doc-token");

        var response = await client.PostAsync(
            "/api/v1/public/donors/submit/document",
            MakeDocumentUploadForm("valid-doc-token", "BBBEECertificate"));

        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var root = JsonDocument.Parse(body).RootElement;
        Assert.Equal("Document uploaded successfully.", root.GetProperty("message").GetString());

        using var verifyScope = _factory.Services.CreateScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<CrmDbContext>();
        var document = await verifyContext.DonorDocuments.SingleAsync(d => d.DonorId == donor.Id);
        Assert.Equal(DocumentType.BBBEECertificate, document.DocumentType);
        Assert.Null(document.UploadedByUserId);
    }

    [Fact]
    public async Task SubmitDocument_TokenReused_SecondCallReturns400()
    {
        var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var seedContext = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        await seedContext.Database.EnsureDeletedAsync();
        await seedContext.Database.EnsureCreatedAsync();
        await SeedFixtureDataAsync(seedContext);

        await SeedPendingDonorWithTokenAsync("reuse-me-token");

        var first = await client.PostAsync(
            "/api/v1/public/donors/submit/document",
            MakeDocumentUploadForm("reuse-me-token", "BBBEECertificate"));
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await client.PostAsync(
            "/api/v1/public/donors/submit/document",
            MakeDocumentUploadForm("reuse-me-token", "BBBEECertificate"));

        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
        var root = JsonDocument.Parse(await second.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal("VALIDATION_ERROR", root.GetProperty("code").GetString());
    }

    [Fact]
    public async Task SubmitDocument_ExpiredToken_Returns400()
    {
        var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var seedContext = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        await seedContext.Database.EnsureDeletedAsync();
        await seedContext.Database.EnsureCreatedAsync();
        await SeedFixtureDataAsync(seedContext);

        await SeedPendingDonorWithTokenAsync("expired-token", DateTimeOffset.UtcNow.AddMinutes(-1));

        var response = await client.PostAsync(
            "/api/v1/public/donors/submit/document",
            MakeDocumentUploadForm("expired-token", "BBBEECertificate"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SubmitDocument_UnknownToken_Returns400()
    {
        var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var seedContext = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        await seedContext.Database.EnsureDeletedAsync();
        await seedContext.Database.EnsureCreatedAsync();
        await SeedFixtureDataAsync(seedContext);

        var response = await client.PostAsync(
            "/api/v1/public/donors/submit/document",
            MakeDocumentUploadForm("never-issued-token", "BBBEECertificate"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SubmitDocument_WrongDocumentType_Returns400()
    {
        var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var seedContext = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        await seedContext.Database.EnsureDeletedAsync();
        await seedContext.Database.EnsureCreatedAsync();
        await SeedFixtureDataAsync(seedContext);

        await SeedPendingDonorWithTokenAsync("wrong-type-token");

        var response = await client.PostAsync(
            "/api/v1/public/donors/submit/document",
            MakeDocumentUploadForm("wrong-type-token", "Signature"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("MZ-executable", "application/pdf", "cert.pdf")]
    [InlineData("png-as-pdf", "image/png", "cert.pdf")]
    [InlineData("pdf-labelled-png", "image/png", "cert.png")]
    public async Task SubmitDocument_ContentDoesNotMatchDeclaredTypeOrExtension_Returns400(
        string scenario, string contentType, string fileName)
    {
        var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var seedContext = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        await seedContext.Database.EnsureDeletedAsync();
        await seedContext.Database.EnsureCreatedAsync();
        await SeedFixtureDataAsync(seedContext);
        var token = "mismatch-token-" + scenario;
        await SeedPendingDonorWithTokenAsync(token);

        byte[] bytes = scenario switch
        {
            "MZ-executable" => [0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00],
            "png-as-pdf" => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A],
            _ => "%PDF-1.7 test"u8.ToArray()
        };

        var response = await client.PostAsync(
            "/api/v1/public/donors/submit/document",
            MakeDocumentUploadForm(token, "BBBEECertificate", bytes, contentType, fileName));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await _blobStorageMock.DidNotReceive().UploadAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task SubmitDocument_HostileFileName_BlobPathIsGuidOnlyAndDisplayNameHasNoSeparators()
    {
        var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var seedContext = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        await seedContext.Database.EnsureDeletedAsync();
        await seedContext.Database.EnsureCreatedAsync();
        await SeedFixtureDataAsync(seedContext);
        await SeedPendingDonorWithTokenAsync("hostile-name-token");

        var response = await client.PostAsync(
            "/api/v1/public/donors/submit/document",
            MakeDocumentUploadForm("hostile-name-token", "BBBEECertificate", fileName: @"a\..\..\x.pdf"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        await _blobStorageMock.Received(1).UploadAsync(
            Arg.Any<Stream>(),
            Arg.Is<string>(p => System.Text.RegularExpressions.Regex.IsMatch(
                p, @"^donors/[0-9a-f-]{36}/BBBEECertificate/[0-9a-f-]{36}\.pdf$")),
            "application/pdf");
        var doc = await seedContext.DonorDocuments.AsNoTracking().SingleAsync();
        Assert.Equal("x.pdf", doc.FileName);
    }

    [Fact]
    public async Task SubmitDocument_UnsupportedMimeType_Returns400()
    {
        var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var seedContext = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        await seedContext.Database.EnsureDeletedAsync();
        await seedContext.Database.EnsureCreatedAsync();
        await SeedFixtureDataAsync(seedContext);

        await SeedPendingDonorWithTokenAsync("bad-mime-token");

        var response = await client.PostAsync(
            "/api/v1/public/donors/submit/document",
            MakeDocumentUploadForm("bad-mime-token", "BBBEECertificate", contentType: "application/zip", fileName: "cert.zip"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SubmitThenSubmitDocument_EndToEndChain_LinksDocumentToTheSubmittedDonor()
    {
        var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var seedContext = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        await seedContext.Database.EnsureDeletedAsync();
        await seedContext.Database.EnsureCreatedAsync();
        await SeedFixtureDataAsync(seedContext);

        var submitResponse = await client.PostAsJsonAsync("/api/v1/public/donors/submit", MakeValidPayload());
        Assert.Equal(HttpStatusCode.Created, submitResponse.StatusCode);
        var submitRoot = JsonDocument.Parse(await submitResponse.Content.ReadAsStringAsync()).RootElement;
        var sessionToken = submitRoot.GetProperty("submissionToken").GetString()!;

        var docResponse = await client.PostAsync(
            "/api/v1/public/donors/submit/document",
            MakeDocumentUploadForm(sessionToken, "BBBEECertificate"));

        Assert.Equal(HttpStatusCode.Created, docResponse.StatusCode);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<CrmDbContext>();
        var donor = await verifyContext.Donors.SingleAsync();
        Assert.True(await verifyContext.DonorDocuments.AnyAsync(
            d => d.DonorId == donor.Id && d.DocumentType == DocumentType.BBBEECertificate));
        Assert.Null(donor.SubmissionToken); // burned after the successful upload
    }
}
