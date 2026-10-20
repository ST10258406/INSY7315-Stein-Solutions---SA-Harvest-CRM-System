namespace CRM.API.Tests.Controllers;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CRM.Application.Modules.Auth.Commands.Login;
using CRM.Application.Modules.Auth.Dtos;
using CRM.Application.Modules.Vetting.Submissions.Dtos;
using System.Text.Json.Nodes;
using CRM.Domain.Entities;
using CRM.Domain.Entities.Lookups;
using CRM.Domain.Entities.Vetting;
using CRM.Domain.Enums;
using CRM.Infrastructure.Persistence;
using Hangfire;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

/// <summary>Officers and site-visits endpoints: role matrix plus the create → workload round-trip.</summary>
public class VettingControllersTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Date = "2026-10-21";
    private readonly WebApplicationFactory<Program> _factory;

    public VettingControllersTests(WebApplicationFactory<Program> factory)
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
                    services.Remove(d);

                services.Remove(services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<CrmDbContext>))!);
                services.Remove(services.SingleOrDefault(d => d.ServiceType == typeof(CrmDbContext))!);

                services.AddHangfire(config => config.UseInMemoryStorage());
                services.AddDbContext<CrmDbContext>(options => options.UseInMemoryDatabase("InMemoryDbForVettingTesting"));
            });
        });
    }

    private sealed record Fixture(HttpClient Client, Guid DonorId, Guid OfficerId, Guid VisitId);

    private async Task<Fixture> SetupAsync(string? roleName)
    {
        var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();

        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();

        const string password = "TestPassword123";
        var role = new Role { Id = Guid.NewGuid(), Name = roleName ?? "Marketing" };
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "vetting-test@example.com",
            FirstName = "Vetting",
            LastName = "Test",
            PasswordHash = new PasswordHasher<User>().HashPassword(null!, password),
            UserRoles = new List<UserRole>()
        };
        user.UserRoles.Add(new UserRole { RoleId = role.Id, UserId = user.Id, Role = role, User = user });
        context.Roles.Add(role);
        context.Users.Add(user);

        context.LookupCompanyTypes.Add(new LookupCompanyType { Id = 1, Name = "Manufacturer", IsActive = true });
        context.LookupEntityTypes.Add(new LookupEntityType { Id = 1, Name = "Pty Ltd", IsActive = true });
        context.LookupDonationFrequencies.Add(new LookupDonationFrequency { Id = 1, Name = "Monthly", IsActive = true });
        var donor = new Donor
        {
            Id = Guid.NewGuid(),
            CompanyName = "FoodCorp SA",
            CompanyTypeId = 1,
            RegisteredCompanyName = "FoodCorp SA (Pty) Ltd",
            EntityTypeId = 1,
            DonationFrequencyId = 1,
            Status = DonorStatus.PendingReview,
            SubmissionSource = SubmissionSource.ManualCapture,
            CreatedByUserId = user.Id
        };
        context.Donors.Add(donor);

        var officer = new VettingOfficer { Id = Guid.NewGuid(), FullName = "Thandi Mokoena", Initials = "TM", AssignedRegion = "Gauteng North" };
        var visit = new VettingSiteVisit
        {
            Id = Guid.NewGuid(), DonorId = donor.Id, OfficerId = officer.Id, LocationLabel = "Midrand",
            ScheduledDate = DateOnly.Parse(Date), ScheduledTime = new TimeOnly(9, 0)
        };
        context.VettingOfficers.Add(officer);
        context.VettingSiteVisits.Add(visit);
        await context.SaveChangesAsync();

        if (roleName is not null)
        {
            var login = await client.PostAsJsonAsync("/api/auth/login", new LoginCommand(user.Email, password));
            var token = JsonSerializer.Deserialize<LoginResponseDto>(
                await login.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!.AccessToken;
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        return new Fixture(client, donor.Id, officer.Id, visit.Id);
    }

    private static object NewOfficer => new { fullName = "Pieter van Wyk", initials = "PVW", assignedRegion = "Western Cape Metro" };

    private static IEnumerable<Func<Fixture, Task<HttpResponseMessage>>> AllEndpoints() =>
    [
        f => f.Client.GetAsync("/api/v1/vetting/officers"),
        f => f.Client.GetAsync($"/api/v1/vetting/officers/{f.OfficerId}"),
        f => f.Client.PostAsJsonAsync("/api/v1/vetting/officers", NewOfficer),
        f => f.Client.PatchAsJsonAsync($"/api/v1/vetting/officers/{f.OfficerId}", new { assignedRegion = "Gauteng South" }),
        f => f.Client.GetAsync($"/api/v1/vetting/site-visits?date={Date}"),
        f => f.Client.PostAsJsonAsync("/api/v1/vetting/site-visits",
            new { donorId = f.DonorId, officerId = f.OfficerId, scheduledDate = Date, scheduledTime = "11:30:00", locationLabel = "Midrand" }),
        f => f.Client.PatchAsJsonAsync($"/api/v1/vetting/site-visits/{f.VisitId}", new { visitStatus = "InProgress" }),
        f => f.Client.GetAsync("/api/v1/vetting/submissions"),
        f => f.Client.GetAsync($"/api/v1/vetting/submissions/{Guid.NewGuid()}"),
        f => f.Client.PostAsJsonAsync("/api/v1/vetting/submissions", new { donorId = f.DonorId, officerId = f.OfficerId, details = Details() }),
        f => f.Client.PatchAsJsonAsync($"/api/v1/vetting/submissions/{Guid.NewGuid()}", new { details = Details() }),
        f => f.Client.PostAsync($"/api/v1/vetting/submissions/{Guid.NewGuid()}/submit", null),
    ];

    [Fact]
    public async Task EveryEndpoint_WithoutToken_Returns401()
    {
        var f = await SetupAsync(null);
        foreach (var call in AllEndpoints())
            Assert.Equal(HttpStatusCode.Unauthorized, (await call(f)).StatusCode);
    }

    [Fact]
    public async Task EveryEndpoint_AsMarketing_Returns403()
    {
        var f = await SetupAsync("Marketing");
        foreach (var call in AllEndpoints())
            Assert.Equal(HttpStatusCode.Forbidden, (await call(f)).StatusCode);
    }

    [Fact]
    public async Task OfficerWrites_AsProcurement_Return403_ButReadsSucceed()
    {
        var f = await SetupAsync("Procurement");

        Assert.Equal(HttpStatusCode.Forbidden, (await f.Client.PostAsJsonAsync("/api/v1/vetting/officers", NewOfficer)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await f.Client.PatchAsJsonAsync($"/api/v1/vetting/officers/{f.OfficerId}", new { assignedRegion = "Gauteng South" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await f.Client.GetAsync("/api/v1/vetting/officers")).StatusCode);
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("SuperAdmin")]
    public async Task OfficerWrites_AsAdmin_Succeed_AndWorkloadIsNotAccepted(string role)
    {
        var f = await SetupAsync(role);

        var created = await f.Client.PostAsJsonAsync("/api/v1/vetting/officers",
            new { fullName = "Pieter van Wyk", initials = "PVW", assignedRegion = "Western Cape Metro", workloadCount = 42 });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(0, Body(created).GetProperty("data").GetProperty("workloadCount").GetInt32());

        var patched = await f.Client.PatchAsJsonAsync($"/api/v1/vetting/officers/{f.OfficerId}", new { assignedRegion = "Gauteng South" });
        Assert.Equal(HttpStatusCode.OK, patched.StatusCode);
        Assert.Equal("Gauteng South", Body(patched).GetProperty("data").GetProperty("assignedRegion").GetString());
    }

    [Fact]
    public async Task CreateSiteVisit_AsProcurement_ListsForTheDay_AndRecountsWorkload()
    {
        var f = await SetupAsync("Procurement");

        var created = await f.Client.PostAsJsonAsync("/api/v1/vetting/site-visits",
            new { donorId = f.DonorId, officerId = f.OfficerId, scheduledDate = Date, scheduledTime = "08:00:00", locationLabel = "Sandton" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var visit = Body(created).GetProperty("data");
        Assert.Equal("ToDo", visit.GetProperty("visitStatus").GetString());
        Assert.Equal("FoodCorp SA", visit.GetProperty("donorCompanyName").GetString());
        Assert.Equal("TM", visit.GetProperty("officerInitials").GetString());

        var day = Body(await f.Client.GetAsync($"/api/v1/vetting/site-visits?date={Date}&officerId={f.OfficerId}")).GetProperty("data");
        Assert.Equal(2, day.GetArrayLength());
        Assert.Equal("Sandton", day[0].GetProperty("locationLabel").GetString()); // 08:00 sorts before 09:00

        var officer = Body(await f.Client.GetAsync($"/api/v1/vetting/officers/{f.OfficerId}")).GetProperty("data");
        Assert.Equal(2, officer.GetProperty("workloadCount").GetInt32());
    }

    [Fact]
    public async Task CreateSiteVisit_UnknownDonor_Returns404()
    {
        var f = await SetupAsync("Procurement");
        var response = await f.Client.PostAsJsonAsync("/api/v1/vetting/site-visits",
            new { donorId = Guid.NewGuid(), officerId = f.OfficerId, scheduledDate = Date, scheduledTime = "08:00:00", locationLabel = "Sandton" });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetSiteVisits_WithoutDate_Returns400()
    {
        var f = await SetupAsync("Procurement");
        Assert.Equal(HttpStatusCode.BadRequest, (await f.Client.GetAsync("/api/v1/vetting/site-visits")).StatusCode);
    }

    private static VettingAssessmentDetailsDto Details() => new()
    {
        LegalName = "FoodCorp SA (Pty) Ltd", ContactName = "Sipho Dlamini", ContactEmail = "sipho@example.com",
        ContactPhone = "+27 82 000 0000", Website = "https://foodcorp.example.com",
        AddressPhysical = "12 Industrial Road", AddressDetail = "Gate 3", Province = "Gauteng", What3Words = "///apple.orange.banana",
        CoreFocusAreas = ["Feeding", "Health"], TargetPopulation = ["Children"], ProgrammesSummary = "Daily soup kitchen.",
        DistributionChannel = "Both", FullTimeFemales = 2, FullTimeMales = 1, VolunteersCount = 5,
        IsRegisteredNpo = false, IsRegisteredDsd = true,
        BeneficiaryRaces = ["African"], BeneficiaryGenders = ["Female", "Male"], BeneficiaryAgeGroups = ["0–5", "60+"],
        FeedingFrequency = "Weekdays only", TotalPeopleServed = 50, FemalesServed = 30, MalesServed = 20,
        AfricanServed = 50, ColouredServed = 0, IndianAsianServed = 0, WhiteServed = 0,
        SaHarvestReliance = "High", TransportCapacity = "None — delivery required",
        MealsProvided = ["Lunch"], FeedingDays = ["Mon", "Fri"], LastFedDate = new DateOnly(2026, 1, 15),
        FoodStorageTypes = ["Dry"], KitchenClean = true, WaterAccess = true, ToiletFacilities = false, PestFree = true,
        InfrastructureChecks = ["Roofing"], DeliveryAccessEase = true, ParkingAndSecurity = true, PoliceStationProximity = "2 km",
        OfficerComments = "Well run.", ProposalCapabilities = "Basic", DigitalCapabilities = "WhatsApp only",
        SlaSigned = true, ConsentFormSigned = true, PolicyDocumentPresent = true
    };

    [Fact]
    public async Task Submission_CreateGetSubmitThenPatch_RoundTripsEveryField_AndLocksAfterSubmit()
    {
        var f = await SetupAsync("Procurement");
        var details = Details();

        var created = await f.Client.PostAsJsonAsync("/api/v1/vetting/submissions",
            new { donorId = f.DonorId, officerId = f.OfficerId, siteVisitId = f.VisitId, details });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var createdData = Body(created).GetProperty("data");
        var id = createdData.GetProperty("id").GetGuid();
        Assert.Matches(@"^VET-\d{4}-\d{4}$", createdData.GetProperty("formReference").GetString());

        var got = Body(await f.Client.GetAsync($"/api/v1/vetting/submissions/{id}")).GetProperty("data");
        var sent = JsonSerializer.SerializeToNode(details, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.True(JsonNode.DeepEquals(sent, JsonNode.Parse(got.GetProperty("details").GetRawText())), got.GetProperty("details").GetRawText());
        Assert.Equal("New", got.GetProperty("approvalState").GetString());
        Assert.Equal("FoodCorp SA", got.GetProperty("donorCompanyName").GetString());
        Assert.Equal(4, got.GetProperty("photos").GetArrayLength());
        Assert.False(got.GetProperty("files").GetProperty("npoCertificate").GetBoolean());
        Assert.DoesNotContain("Path", got.GetRawText());

        Assert.Equal(HttpStatusCode.BadRequest, (await f.Client.PostAsync($"/api/v1/vetting/submissions/{id}/submit", null)).StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
            await context.VettingEvidencePhotos.Where(p => p.SubmissionId == id).ForEachAsync(p => p.IsCaptured = true);
            await context.SaveChangesAsync();
        }

        var submitResponse = await f.Client.PostAsync($"/api/v1/vetting/submissions/{id}/submit", null);
        Assert.True(submitResponse.IsSuccessStatusCode, await submitResponse.Content.ReadAsStringAsync());
        var submitted = Body(submitResponse).GetProperty("data");
        Assert.Equal("Ready", submitted.GetProperty("approvalState").GetString());
        Assert.Equal(
            ["Organisation is not a registered NPO", "Inadequate toilet facilities"],
            submitted.GetProperty("flags").EnumerateArray().Select(x => x.GetProperty("description").GetString()).Order().Reverse());

        var list = Body(await f.Client.GetAsync("/api/v1/vetting/submissions?approvalState=Ready&search=foodcorp"));
        Assert.Equal(2, list.GetProperty("data")[0].GetProperty("activeFlagCount").GetInt32());

        var patched = await f.Client.PatchAsJsonAsync($"/api/v1/vetting/submissions/{id}", new { details });
        Assert.Equal(HttpStatusCode.Conflict, patched.StatusCode);
    }

    [Fact]
    public async Task CreateSubmission_UnansweredBoolean_Returns400_KeyedUnderDetails()
    {
        var f = await SetupAsync("Procurement");
        var details = Details();
        details.SlaSigned = null;

        var response = await f.Client.PostAsJsonAsync("/api/v1/vetting/submissions", new { donorId = f.DonorId, officerId = f.OfficerId, details });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Details.SlaSigned", await response.Content.ReadAsStringAsync());
    }

    private static JsonElement Body(HttpResponseMessage r) =>
        JsonDocument.Parse(r.Content.ReadAsStringAsync().Result).RootElement;
}
