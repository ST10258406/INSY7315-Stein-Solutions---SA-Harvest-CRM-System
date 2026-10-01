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
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Hangfire;

public class DonorsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public DonorsControllerTests(WebApplicationFactory<Program> factory)
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
                    options.UseInMemoryDatabase("InMemoryDbForDonorsTesting");
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

    private static async Task SeedDonorsAsync(CrmDbContext context, int count)
    {
        var companyType = new LookupCompanyType { Id = 1, Name = "Manufacturer", IsActive = true };
        var entityType = new LookupEntityType { Id = 1, Name = "Pty Ltd", IsActive = true };
        var frequency = new LookupDonationFrequency { Id = 1, Name = "Monthly", IsActive = true };
        context.LookupCompanyTypes.Add(companyType);
        context.LookupEntityTypes.Add(entityType);
        context.LookupDonationFrequencies.Add(frequency);

        var creator = new User
        {
            Id = Guid.NewGuid(),
            Email = "creator@example.com",
            FirstName = "Creator",
            LastName = "User",
            PasswordHash = "n/a"
        };
        context.Users.Add(creator);

        for (var i = 1; i <= count; i++)
        {
            context.Donors.Add(new Donor
            {
                Id = Guid.NewGuid(),
                CompanyName = $"Donor Company {i:00}",
                CompanyTypeId = companyType.Id,
                RegisteredCompanyName = $"Donor Company {i:00} (Pty) Ltd",
                EntityTypeId = entityType.Id,
                DonationFrequencyId = frequency.Id,
                Status = DonorStatus.Active,
                SubmissionSource = SubmissionSource.ManualCapture,
                CreatedByUserId = creator.Id
            });
        }

        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetDonors_ProcurementUser_ReturnsPaginatedEnvelope()
    {
        var client = await CreateAuthenticatedClient("Procurement");
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        await SeedDonorsAsync(context, 25);

        var response = await client.GetAsync("/api/v1/donors?page=2&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content).RootElement;

        var data = json.GetProperty("data");
        Assert.Equal(10, data.GetArrayLength());

        var pagination = json.GetProperty("pagination");
        Assert.Equal(2, pagination.GetProperty("page").GetInt32());
        Assert.Equal(10, pagination.GetProperty("pageSize").GetInt32());
        Assert.Equal(25, pagination.GetProperty("totalCount").GetInt32());
        Assert.Equal(3, pagination.GetProperty("totalPages").GetInt32());

        var firstDonor = data[0];
        Assert.True(firstDonor.TryGetProperty("id", out _));
        Assert.True(firstDonor.TryGetProperty("companyName", out _));
        Assert.True(firstDonor.TryGetProperty("companyType", out _));
        Assert.True(firstDonor.TryGetProperty("status", out _));
        Assert.True(firstDonor.TryGetProperty("operationalRegions", out _));
        Assert.True(firstDonor.TryGetProperty("donationTypes", out _));
        Assert.False(firstDonor.TryGetProperty("collectionAddress", out _));
        Assert.False(firstDonor.TryGetProperty("incomeTaxNumber", out _));
    }

    [Fact]
    public async Task GetDonors_InvalidPageSize_Returns400()
    {
        var client = await CreateAuthenticatedClient("Procurement");
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        await SeedDonorsAsync(context, 1);

        var response = await client.GetAsync("/api/v1/donors?page=1&pageSize=500");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetDonors_MarketingUser_Forbidden()
    {
        var client = await CreateAuthenticatedClient("Marketing");

        var response = await client.GetAsync("/api/v1/donors");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetDonors_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/donors");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static async Task<Donor> SeedFullyPopulatedDonorAsync(CrmDbContext context)
    {
        var companyType = new LookupCompanyType { Id = 1, Name = "Manufacturer", IsActive = true };
        var entityType = new LookupEntityType { Id = 1, Name = "Private Company", IsActive = true };
        var frequency = new LookupDonationFrequency { Id = 2, Name = "Monthly", IsActive = true };
        var bbbeeStatus = new LookupBbbeeStatus { Id = 2, Name = "Level 2", IsActive = true };
        var province = new LookupProvince { Id = 3, Code = "GP", Name = "Gauteng", IsActive = true };
        var region = new LookupOperationalRegion { Id = 1, Code = "JHB", Name = "Johannesburg", IsActive = true };
        var donationType = new LookupDonationType { Id = 3, Name = "Meat", IsActive = true };

        context.LookupCompanyTypes.Add(companyType);
        context.LookupEntityTypes.Add(entityType);
        context.LookupDonationFrequencies.Add(frequency);
        context.LookupBbbeeStatuses.Add(bbbeeStatus);
        context.LookupProvinces.Add(province);
        context.LookupOperationalRegions.Add(region);
        context.LookupDonationTypes.Add(donationType);

        var relationshipManager = new User { Id = Guid.NewGuid(), Email = "jane@example.com", FirstName = "Jane", LastName = "Doe", PasswordHash = "n/a" };
        var creator = new User { Id = Guid.NewGuid(), Email = "creator2@example.com", FirstName = "Creator", LastName = "User", PasswordHash = "n/a" };
        context.Users.Add(relationshipManager);
        context.Users.Add(creator);

        var donor = new Donor
        {
            Id = Guid.NewGuid(),
            CompanyName = "FoodCorp SA",
            CompanyTypeId = companyType.Id,
            Website = "https://foodcorp.co.za",
            RegisteredCompanyName = "FoodCorp (Pty) Ltd",
            TradingName = "FoodCorp SA",
            EntityTypeId = entityType.Id,
            CompanyRegistrationNumber = "2010/012345/07",
            IncomeTaxNumber = "9012345678",
            DonationFrequencyId = frequency.Id,
            BbbeeStatusId = bbbeeStatus.Id,
            CollectionAddress = "Gate 3, 12 Industrial Rd",
            OperationsLogisticsDetails = "Contact John before arrival",
            AdditionalInformation = "Prefers morning calls",
            RelationshipManagerId = relationshipManager.Id,
            Status = DonorStatus.Active,
            SubmissionSource = SubmissionSource.PublicForm,
            MarketingConsent = true,
            MarketingConsentDate = new DateTime(2026, 1, 15, 8, 0, 0, DateTimeKind.Utc),
            ImpactReportingPreferences = "Quarterly PDF via email",
            FollowUpDate = new DateTime(2026, 8, 15),
            CreatedByUserId = creator.Id
        };
        context.Donors.Add(donor);

        context.DonorLegalAddresses.Add(new DonorLegalAddress
        {
            DonorId = donor.Id,
            StreetAddress = "12 Industrial Road",
            Suburb = "Meadowdale",
            City = "Johannesburg",
            ProvinceId = province.Id,
            PostalCode = "1609"
        });

        context.DonorContacts.AddRange(
            new DonorContact { DonorId = donor.Id, ContactType = ContactType.Primary, Name = "John Smith", JobTitle = "Operations Manager", Phone = "+27821234567", Email = "john@foodcorp.co.za" },
            new DonorContact { DonorId = donor.Id, ContactType = ContactType.Marketing, Name = "Sarah Jones", Phone = "+27831234567", Email = "sarah@foodcorp.co.za" },
            new DonorContact { DonorId = donor.Id, ContactType = ContactType.Accounts, Name = "Mike Brown", Phone = "+27841234567", Email = "accounts@foodcorp.co.za" }
        );

        context.DonorOperationalRegions.Add(new DonorOperationalRegion { DonorId = donor.Id, OperationalRegionId = region.Id });
        context.DonorDonationTypes.Add(new DonorDonationType { DonorId = donor.Id, DonationTypeId = donationType.Id });

        context.DonorDocuments.Add(new DonorDocument
        {
            Id = Guid.NewGuid(),
            DonorId = donor.Id,
            DocumentType = DocumentType.BBBEECertificate,
            FileName = "bbbee_cert_2025.pdf",
            BlobStoragePath = "blob://donors/private/bbbee_cert_2025.pdf",
            CreatedAt = new DateTime(2026, 1, 15, 8, 0, 0, DateTimeKind.Utc)
        });

        // Soft-deleted by an Admin — must not appear in the detail response (security review F-06).
        context.DonorDocuments.Add(new DonorDocument
        {
            Id = Guid.NewGuid(),
            DonorId = donor.Id,
            DocumentType = DocumentType.Signature,
            FileName = "deleted_signature.png",
            BlobStoragePath = "blob://donors/private/deleted_signature.png",
            IsActive = false,
            CreatedAt = new DateTime(2026, 1, 10, 8, 0, 0, DateTimeKind.Utc)
        });

        await context.SaveChangesAsync();

        return donor;
    }

    [Fact]
    public async Task GetDonorById_ExistingDonor_ReturnsFullNestedShape()
    {
        var client = await CreateAuthenticatedClient("Procurement");
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        var donor = await SeedFullyPopulatedDonorAsync(context);

        var response = await client.GetAsync($"/api/v1/donors/{donor.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var data = JsonDocument.Parse(content).RootElement.GetProperty("data");

        Assert.Equal(donor.Id, data.GetProperty("id").GetGuid());
        Assert.Equal("Active", data.GetProperty("status").GetString());

        Assert.True(data.TryGetProperty("company", out var company));
        Assert.Equal("FoodCorp SA", company.GetProperty("companyName").GetString());

        Assert.True(data.TryGetProperty("primaryContact", out var primaryContact));
        Assert.Equal("John Smith", primaryContact.GetProperty("name").GetString());
        Assert.True(data.TryGetProperty("marketingContact", out _));
        Assert.True(data.TryGetProperty("accountsContact", out _));

        Assert.True(data.TryGetProperty("legalAddress", out var legalAddress));
        Assert.Equal("Meadowdale", legalAddress.GetProperty("suburb").GetString());

        Assert.True(data.TryGetProperty("donations", out var donations));
        Assert.Equal("Monthly", donations.GetProperty("frequency").GetProperty("name").GetString());

        Assert.True(data.TryGetProperty("compliance", out var compliance));
        var documents = compliance.GetProperty("documents");
        Assert.Equal(1, documents.GetArrayLength());
        Assert.Equal("bbbee_cert_2025.pdf", documents[0].GetProperty("originalFileName").GetString());
        Assert.DoesNotContain("deleted_signature.png", content);

        Assert.True(data.TryGetProperty("crm", out var crm));
        Assert.Equal("Jane Doe", crm.GetProperty("relationshipManager").GetProperty("fullName").GetString());

        // Security requirement: the raw blob path must never appear anywhere in the payload.
        Assert.DoesNotContain("blobStoragePath", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("blobStorageUrl", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("blob://", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetDonorById_NonExistentId_Returns404WithErrorEnvelope()
    {
        var client = await CreateAuthenticatedClient("Procurement");
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        await SeedDonorsAsync(context, 1);

        var response = await client.GetAsync($"/api/v1/donors/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content).RootElement;
        Assert.Equal(404, json.GetProperty("status").GetInt32());
        Assert.Equal("NOT_FOUND", json.GetProperty("code").GetString());
    }

    [Fact]
    public async Task GetDonorById_MarketingUser_Forbidden()
    {
        var client = await CreateAuthenticatedClient("Marketing");

        var response = await client.GetAsync($"/api/v1/donors/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
