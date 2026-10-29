using System.Text.Encodings.Web;
using System.Text.Json;
using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Models;
using CRM.Application.Modules.Agent.Tools;
using CRM.Application.Modules.Approvals.Dtos;
using CRM.Application.Modules.Approvals.Queries.GetApprovals;
using CRM.Application.Modules.Dashboard.Dtos;
using CRM.Application.Modules.Dashboard.Queries.GetDashboardStats;
using CRM.Application.Modules.Dashboard.Queries.GetManagerActivity;
using CRM.Application.Modules.Donors.Dtos;
using CRM.Application.Modules.Donors.Queries.GetDonorById;
using CRM.Application.Modules.Donors.Queries.GetDonors;
using CRM.Application.Modules.Interactions.Dtos;
using CRM.Application.Modules.Interactions.Queries.GetDonorInteractions;
using CRM.Application.Modules.Lookups.Queries.GetRelationshipManagers;
using CRM.Application.Modules.Reports.Dtos;
using CRM.Application.Modules.Reports.Queries.GetDonorsByRegionReport;
using CRM.Application.Modules.Reports.Queries.GetDonorsByStatusReport;
using CRM.Application.Modules.Reports.Queries.GetDonorsByTypeReport;
using CRM.Application.Modules.Reports.Queries.GetDonorsContactedReport;
using CRM.Application.Modules.Tasks.Dtos;
using CRM.Application.Modules.Tasks.Queries.GetDonorTasks;
using CRM.Application.Modules.Tasks.Queries.GetMyTasks;
using CRM.Application.Modules.Vetting.Officers.Dtos;
using CRM.Application.Modules.Vetting.Officers.Queries.GetOfficers;
using CRM.Application.Modules.Vetting.SiteVisits.Dtos;
using CRM.Application.Modules.Vetting.SiteVisits.Queries.GetSiteVisits;
using CRM.Application.Modules.Vetting.Submissions.Dtos;
using CRM.Application.Modules.Vetting.Submissions.Queries.GetSubmissionById;
using CRM.Application.Modules.Vetting.Submissions.Queries.GetSubmissions;
using FluentValidation.Results;
using MediatR;
using Microsoft.Extensions.AI;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace CRM.Application.Tests.Modules.Agent;

/// <summary>
/// US-5 data minimisation: every query result below is stuffed with personal data, and each
/// tool's output — invoked through its real AIFunction, exactly as the model receives it —
/// must contain none of it.
/// </summary>
public class CrmAgentToolsTests
{
    // Values that must never reach the model.
    private const string Phone = "+27 82 555 0199";
    private const string Email = "leak@donor.co.za";
    private const string TaxNumber = "9123456789";
    private const string RegistrationNumber = "2019/123456/07";
    private const string FileName = "bbbee-cert-secret.pdf";
    private const string Website = "https://donor.example";
    private const string AttachmentUrl = "https://blob.example/attachment.pdf";
    private const string StreetAddress = "12 Secret Street";
    private const string FreeTextEmail = "ceo.private@gmail.com";
    private const string FreeTextUrl = "http://phish.example/login";
    private const string IdNumber = "8001015009087";
    private const string What3Words = "///index.home.raft";
    private const string ContactName = "Private Contact Person";

    private static readonly string[] ForbiddenValues =
        [Phone, Email, TaxNumber, RegistrationNumber, FileName, Website, AttachmentUrl, StreetAddress, FreeTextEmail, FreeTextUrl, IdNumber, What3Words, ContactName, "http", "www."];

    private static readonly Guid DonorId = Guid.NewGuid();
    private static readonly Guid SubmissionId = Guid.NewGuid();

    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly Dictionary<string, AIFunction> _tools;

    public CrmAgentToolsTests()
    {
        _tools = new CrmAgentTools(_sender).AsTools().Cast<AIFunction>().ToDictionary(t => t.Name);

        _sender.Send(Arg.Any<GetDonorsQuery>(), Arg.Any<CancellationToken>()).Returns(Page(new DonorListItemDto
        {
            Id = DonorId,
            CompanyName = "FoodCorp SA",
            CompanyType = "Retailer",
            Status = "Active",
            RelationshipManager = new RelationshipManagerDto { Id = Guid.NewGuid(), FullName = "Thandi Mokoena" },
            OperationalRegions = ["Gauteng"],
            DonationTypes = ["Fresh produce"],
        }));

        _sender.Send(Arg.Any<GetDonorByIdQuery>(), Arg.Any<CancellationToken>()).Returns(DonorDetail());

        _sender.Send(Arg.Any<GetDonorInteractionsQuery>(), Arg.Any<CancellationToken>()).Returns(Page(new InteractionLogDto
        {
            Id = Guid.NewGuid(),
            DonorId = DonorId,
            InteractionType = "Email",
            Subject = $"Re: call {Phone}",
            Body = $"Spoke to the CEO, reach her on {FreeTextEmail} or see {FreeTextUrl}. " + new string('x', 600),
            EmailAttachmentUrl = AttachmentUrl,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = new InteractionUserDto { Id = Guid.NewGuid(), FullName = "Keegan Roux" },
        }));

        var task = new TaskDto
        {
            Id = Guid.NewGuid(),
            Title = $"Email {FreeTextEmail} the forms at {FreeTextUrl} " + new string('y', 300),
            Description = $"Call {Phone}",
            DueDate = DateTime.UtcNow,
            Donor = new TaskDonorDto { Id = DonorId, CompanyName = "FoodCorp SA" },
            AssignedTo = new TaskUserDto { Id = Guid.NewGuid(), FullName = "Nomsa Khumalo" },
            CreatedBy = new TaskUserDto { Id = Guid.NewGuid(), FullName = "Keegan Roux" },
        };
        _sender.Send(Arg.Any<GetDonorTasksQuery>(), Arg.Any<CancellationToken>()).Returns(Page(task));
        _sender.Send(Arg.Any<GetMyTasksQuery>(), Arg.Any<CancellationToken>()).Returns(Page(task));

        _sender.Send(Arg.Any<GetApprovalsQuery>(), Arg.Any<CancellationToken>()).Returns(Page(new ApprovalDto
        {
            Id = Guid.NewGuid(),
            Status = "Rejected",
            RejectionReason = $"Tax number {TaxNumber} is invalid, phone {Phone}",
            CreatedAt = DateTime.UtcNow,
            Donor = new ApprovalDonorDto { Id = DonorId, CompanyName = "FoodCorp SA", Status = "Rejected" },
            RequestedBy = new ApprovalUserDto { Id = Guid.NewGuid(), FullName = "Keegan Roux" },
            ReviewedBy = new ApprovalUserDto { Id = Guid.NewGuid(), FullName = "Admin User" },
        }));

        _sender.Send(Arg.Any<GetDashboardStatsQuery>(), Arg.Any<CancellationToken>()).Returns(new DashboardStatsDto { TotalDonors = 40, ActiveDonors = 25 });
        _sender.Send(Arg.Any<GetManagerActivityQuery>(), Arg.Any<CancellationToken>()).Returns(new ManagerActivityDto
        {
            Period = "monthly",
            Items = [new ManagerActivityItemDto { UserId = Guid.NewGuid(), Name = "Thandi Mokoena", DonorsContacted = 7 }],
        });
        _sender.Send(Arg.Any<GetDonorsByStatusReportQuery>(), Arg.Any<CancellationToken>()).Returns([new DonorsByStatusDto { Status = "Active", DonorCount = 25 }]);
        _sender.Send(Arg.Any<GetDonorsByRegionReportQuery>(), Arg.Any<CancellationToken>()).Returns([new DonorsByRegionDto { Region = "GP", RegionName = "Gauteng", DonorCount = 12 }]);
        _sender.Send(Arg.Any<GetDonorsByTypeReportQuery>(), Arg.Any<CancellationToken>()).Returns([new DonorsByTypeDto { DonationType = "Fresh produce", DonorCount = 9 }]);
        _sender.Send(Arg.Any<GetDonorsContactedReportQuery>(), Arg.Any<CancellationToken>()).Returns(new DonorsContactedReportDto
        {
            Period = new ReportPeriodDto { StartDate = new DateOnly(2026, 10, 1), EndDate = new DateOnly(2026, 10, 31) },
            TotalDonorsContacted = 11,
            ByManager = [new ManagerContactedDto { Manager = new ReportManagerDto { Id = Guid.NewGuid(), FullName = "Thandi Mokoena" }, DonorsContacted = 11, TotalInteractions = 15 }],
        });
        _sender.Send(Arg.Any<GetRelationshipManagersQuery>(), Arg.Any<CancellationToken>())
            .Returns([new RelationshipManagerDto { Id = Guid.NewGuid(), FullName = "Thandi Mokoena" }]);

        _sender.Send(Arg.Any<GetSubmissionsQuery>(), Arg.Any<CancellationToken>()).Returns(Page(new VettingSubmissionListItemDto
        {
            Id = SubmissionId,
            FormReference = "VET-2026-0001",
            DonorCompanyName = "FoodCorp SA",
            OfficerFullName = "Thandi Mokoena",
            Province = "Gauteng",
            ApprovalState = "Ready",
            SubmittedAt = DateTime.UtcNow,
            ActiveFlagCount = 1,
        }));
        _sender.Send(Arg.Any<GetSubmissionByIdQuery>(), Arg.Any<CancellationToken>()).Returns(VettingSubmission());
        _sender.Send(Arg.Any<GetSiteVisitsQuery>(), Arg.Any<CancellationToken>()).Returns([new VettingSiteVisitDto
        {
            Id = Guid.NewGuid(),
            DonorId = DonorId,
            DonorCompanyName = "FoodCorp SA",
            OfficerId = Guid.NewGuid(),
            OfficerInitials = "TM",
            ScheduledDate = new DateOnly(2026, 10, 29),
            ScheduledTime = new TimeOnly(9, 0),
            LocationLabel = StreetAddress,
            VisitNotes = $"Gate code from {Phone}, ID {IdNumber}",
            VisitStatus = "InProgress",
        }]);
        _sender.Send(Arg.Any<GetOfficersQuery>(), Arg.Any<CancellationToken>())
            .Returns([new VettingOfficerDto { Id = Guid.NewGuid(), FullName = "Thandi Mokoena", Initials = "TM", AssignedRegion = "Gauteng North", WorkloadCount = 2 }]);
    }

    private static VettingSubmissionDto VettingSubmission() => new()
    {
        Id = SubmissionId,
        FormReference = "VET-2026-0001",
        DonorId = DonorId,
        DonorCompanyName = "FoodCorp SA",
        OfficerId = Guid.NewGuid(),
        OfficerFullName = "Thandi Mokoena",
        ApprovalState = "Declined",
        AdminDecision = "Declined",
        DecisionReason = $"Director ID {IdNumber} does not match, call {Phone}",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        Details = new VettingAssessmentDetailsDto
        {
            LegalName = "FoodCorp NPC",
            ContactName = ContactName,
            ContactEmail = Email,
            ContactPhone = Phone,
            Website = Website,
            AddressPhysical = StreetAddress,
            AddressDetail = StreetAddress,
            Province = "Gauteng",
            What3Words = What3Words,
            ProgrammesSummary = $"Soup kitchen, see {FreeTextUrl}",
            TotalPeopleServed = 120,
            PoliceStationProximity = StreetAddress,
            OfficerComments = $"Registration {RegistrationNumber}, tax {TaxNumber}",
            ProposalCapabilities = $"Email {FreeTextEmail}",
            DigitalCapabilities = Website,
        },
        Flags = [new VettingFlagDto
        {
            Id = Guid.NewGuid(),
            Description = $"Contact {FreeTextEmail} about {FreeTextUrl} " + new string('z', 400),
            Source = "Reviewer",
            CreatedAt = DateTime.UtcNow,
        }],
        Photos = [new VettingPhotoDto { ShotIndex = 0, Category = "Kitchen", IsCaptured = true, FileName = FileName, CapturedAt = DateTime.UtcNow }],
        Files = new VettingFilesDto { NpoCertificate = true },
    };

    private static PaginatedResult<T> Page<T>(T item) => PaginatedResult<T>.Create([item], 1, 25, 1);

    private static DonorDetailDto DonorDetail()
    {
        DonorContactDto Contact(string name) => new() { Name = name, JobTitle = "Manager", Phone = Phone, Email = Email };
        return new DonorDetailDto
        {
            Id = DonorId,
            Status = "Active",
            SubmissionSource = "PublicForm",
            CreatedAt = DateTime.UtcNow,
            Company = new DonorCompanyDto
            {
                CompanyName = "FoodCorp SA",
                CompanyType = new LookupDto { Id = 1, Name = "Retailer" },
                EntityType = new LookupDto { Id = 1, Name = "Pty Ltd" },
                RegisteredCompanyName = "FoodCorp South Africa (Pty) Ltd",
                Website = Website,
                CompanyRegistrationNumber = RegistrationNumber,
                IncomeTaxNumber = TaxNumber,
            },
            PrimaryContact = Contact("Lerato Dlamini"),
            MarketingContact = Contact("Marketing Person"),
            AccountsContact = Contact("Accounts Person"),
            LegalAddress = new DonorLegalAddressDto { StreetNameNumber = StreetAddress, City = "Johannesburg", PostalCode = "2000" },
            Donations = new DonorDonationsDto
            {
                Frequency = new LookupDto { Id = 1, Name = "Monthly" },
                Types = [new LookupDto { Id = 1, Name = "Fresh produce" }],
                CollectionAddress = StreetAddress,
                OperationsLogisticsDetails = $"Gate code via {Email}",
                OperationalRegions = [new RegionDto { Id = 1, Code = "GP", Name = "Gauteng" }],
            },
            Compliance = new DonorComplianceDto
            {
                BbbeeStatus = new LookupDto { Id = 1, Name = "Level 1" },
                Documents =
                [
                    new DonorDocumentDto { Id = Guid.NewGuid(), DocumentType = "BbbeeCertificate", OriginalFileName = FileName, IsActive = true },
                    new DonorDocumentDto { Id = Guid.NewGuid(), DocumentType = "TaxClearance", OriginalFileName = FileName, IsActive = false },
                ],
            },
            Crm = new DonorCrmDto
            {
                RelationshipManager = new RelationshipManagerDto { Id = Guid.NewGuid(), FullName = "Thandi Mokoena" },
                FollowUpDate = DateTime.UtcNow,
                AdditionalInformation = $"Personal cell {Phone}",
                ImpactReportingPreferences = $"Send to {Email}",
            },
        };
    }

    private async Task<string> InvokeAsync(string tool, Dictionary<string, object?>? args = null)
    {
        var result = await _tools[tool].InvokeAsync(new AIFunctionArguments(args ?? []));
        // Unescaped, so a leaked "+27 …" isn't hidden as "+27 …" from the assertions.
        return JsonSerializer.Serialize(result, new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
    }

    public static TheoryData<string, Dictionary<string, object?>> EveryToolCall => new()
    {
        { "search_donors", new() },
        { "get_donor", new() { ["donorId"] = DonorId } },
        { "get_donor_interactions", new() { ["donorId"] = DonorId } },
        { "get_donor_tasks", new() { ["donorId"] = DonorId } },
        { "get_my_tasks", new() },
        { "list_approvals", new() { ["status"] = "Rejected" } },
        { "get_dashboard_stats", new() },
        { "get_manager_activity", new() },
        { "run_report", new() { ["report"] = "by_status" } },
        { "run_report", new() { ["report"] = "by_region" } },
        { "run_report", new() { ["report"] = "by_type" } },
        { "run_report", new() { ["report"] = "contacted" } },
        { "list_relationship_managers", new() },
        { "list_vetting_submissions", new() },
        { "get_vetting_submission", new() { ["submissionId"] = SubmissionId } },
        { "list_site_visits", new() { ["date"] = "2026-10-29" } },
        { "list_vetting_officers", new() },
    };

    [Fact]
    public void AsTools_ExposesAllSpecTools() =>
        Assert.Equal(
            ["get_dashboard_stats", "get_donor", "get_donor_interactions", "get_donor_tasks", "get_manager_activity",
             "get_my_tasks", "get_vetting_submission", "list_approvals", "list_relationship_managers", "list_site_visits",
             "list_vetting_officers", "list_vetting_submissions", "run_report", "search_donors"],
            _tools.Keys.Order());

    [Theory]
    [MemberData(nameof(EveryToolCall))]
    public async Task Tool_OutputNeverContainsPersonalDataOrUrls(string tool, Dictionary<string, object?> args)
    {
        var output = await InvokeAsync(tool, args);

        Assert.DoesNotContain("\"error\"", output);
        foreach (var forbidden in ForbiddenValues)
            Assert.DoesNotContain(forbidden, output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SearchDonors_CapsPageSizeAndPassesFilters()
    {
        await InvokeAsync("search_donors", new() { ["status"] = "Active", ["regionCode"] = "GP", ["page"] = 2 });

        await _sender.Received(1).Send(
            Arg.Is<GetDonorsQuery>(q => q.PageSize == 25 && q.Page == 2 && q.Status == "Active" && q.RegionCode == "GP"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetDonor_ReturnsAllowListedFieldsOnly()
    {
        var output = JsonDocument.Parse(await InvokeAsync("get_donor", new() { ["donorId"] = DonorId })).RootElement;

        Assert.Equal("FoodCorp SA", output.GetProperty("company").GetProperty("name").GetString());
        Assert.Equal("Lerato Dlamini", output.GetProperty("primaryContact").GetProperty("name").GetString());
        Assert.Equal("Level 1", output.GetProperty("bbbeeStatus").GetString());
        Assert.Equal(1, output.GetProperty("documentCount").GetInt32()); // inactive documents not counted
        Assert.False(output.TryGetProperty("marketingContact", out _));
        Assert.False(output.TryGetProperty("legalAddress", out _));
    }

    [Fact]
    public async Task GetDonorInteractions_SendsLimit20_ScrubsAndTruncatesSummary()
    {
        var output = await InvokeAsync("get_donor_interactions", new() { ["donorId"] = DonorId, ["interactionType"] = "Email" });

        await _sender.Received(1).Send(
            Arg.Is<GetDonorInteractionsQuery>(q => q.DonorId == DonorId && q.InteractionType == "Email" && q.PageSize == 20),
            Arg.Any<CancellationToken>());
        var summary = JsonDocument.Parse(output).RootElement[0].GetProperty("summary").GetString()!;
        Assert.Contains("[removed]", summary);
        Assert.True(summary.Length <= 501);
    }

    [Fact]
    public async Task GetDonorTasks_CapsPageSize()
    {
        await InvokeAsync("get_donor_tasks", new() { ["donorId"] = DonorId, ["isCompleted"] = "false" });

        await _sender.Received(1).Send(
            Arg.Is<GetDonorTasksQuery>(q => q.DonorId == DonorId && q.IsCompleted == "false" && q.PageSize == 25),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetDonorTasks_ScrubsAndTruncatesTitle()
    {
        var output = await InvokeAsync("get_donor_tasks", new() { ["donorId"] = DonorId });

        var title = JsonDocument.Parse(output).RootElement.GetProperty("items")[0].GetProperty("title").GetString()!;
        Assert.Contains("[removed]", title);
        Assert.True(title.Length <= 201);
    }

    [Fact]
    public async Task GetMyTasks_CapsPageSize()
    {
        await InvokeAsync("get_my_tasks", new() { ["isCompleted"] = true, ["dueBefore"] = "2026-10-31" });

        await _sender.Received(1).Send(
            Arg.Is<GetMyTasksQuery>(q => q.IsCompleted && q.DueBefore == new DateOnly(2026, 10, 31) && q.PageSize == 25),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListApprovals_DefaultsToPendingAndCapsPageSize()
    {
        await InvokeAsync("list_approvals");

        await _sender.Received(1).Send(
            Arg.Is<GetApprovalsQuery>(q => q.Status == "Pending" && q.PageSize == 25),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunReport_Contacted_PassesFilters()
    {
        var rmId = Guid.NewGuid();

        await InvokeAsync("run_report", new() { ["report"] = "contacted", ["startDate"] = "2026-10-01", ["endDate"] = "2026-10-31", ["relationshipManagerId"] = rmId });

        await _sender.Received(1).Send(
            Arg.Is<GetDonorsContactedReportQuery>(q =>
                q.StartDate == new DateOnly(2026, 10, 1) && q.EndDate == new DateOnly(2026, 10, 31) && q.RelationshipManagerId == rmId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunReport_UnknownReport_ReturnsError()
    {
        var output = await InvokeAsync("run_report", new() { ["report"] = "everything" });

        Assert.Contains("\"error\"", output);
    }

    [Fact]
    public async Task ListVettingSubmissions_CapsPageSizeAndPassesFilters()
    {
        var officerId = Guid.NewGuid();

        await InvokeAsync("list_vetting_submissions", new() { ["approvalState"] = "Ready", ["officerId"] = officerId, ["page"] = 2 });

        await _sender.Received(1).Send(
            Arg.Is<GetSubmissionsQuery>(q => q.PageSize == 25 && q.Page == 2 && q.ApprovalState == "Ready" && q.OfficerId == officerId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetVettingSubmission_ScrubsAndTruncatesFlags()
    {
        var output = JsonDocument.Parse(await InvokeAsync("get_vetting_submission", new() { ["submissionId"] = SubmissionId })).RootElement;

        await _sender.Received(1).Send(Arg.Is<GetSubmissionByIdQuery>(q => q.Id == SubmissionId), Arg.Any<CancellationToken>());
        var flag = output.GetProperty("flags")[0].GetProperty("description").GetString()!;
        Assert.Contains("[removed]", flag);
        Assert.True(flag.Length <= 301);
        Assert.Equal(1, output.GetProperty("photosCaptured").GetInt32());
        Assert.Equal(120, output.GetProperty("assessment").GetProperty("totalPeopleServed").GetInt32());
        Assert.False(output.TryGetProperty("decisionReason", out _));
    }

    [Fact]
    public async Task ListSiteVisits_PassesDateAndOfficer()
    {
        var officerId = Guid.NewGuid();

        await InvokeAsync("list_site_visits", new() { ["date"] = "2026-10-29", ["officerId"] = officerId });

        await _sender.Received(1).Send(
            Arg.Is<GetSiteVisitsQuery>(q => q.Date == new DateOnly(2026, 10, 29) && q.OfficerId == officerId),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("list_site_visits")]
    [InlineData("list_vetting_officers")]
    public async Task UnpagedVettingLists_AreCappedAt25(string tool)
    {
        _sender.Send(Arg.Any<GetSiteVisitsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Enumerable.Range(0, 30).Select(_ => new VettingSiteVisitDto { Id = Guid.NewGuid() }).ToList());
        _sender.Send(Arg.Any<GetOfficersQuery>(), Arg.Any<CancellationToken>())
            .Returns(Enumerable.Range(0, 30).Select(_ => new VettingOfficerDto { Id = Guid.NewGuid() }).ToList());

        var output = JsonDocument.Parse(await InvokeAsync(tool, new() { ["date"] = "2026-10-29" })).RootElement;

        Assert.Equal(30, output.GetProperty("totalCount").GetInt32());
        Assert.Equal(25, output.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task GetVettingSubmission_NotFound_IsReturnedToTheModelAsError()
    {
        _sender.Send(Arg.Any<GetSubmissionByIdQuery>(), Arg.Any<CancellationToken>()).Throws(new NotFoundException("Vetting submission not found."));

        var output = JsonDocument.Parse(await InvokeAsync("get_vetting_submission", new() { ["submissionId"] = Guid.NewGuid() })).RootElement;

        Assert.Equal("Vetting submission not found.", output.GetProperty("error").GetString());
    }

    [Fact]
    public async Task NotFound_IsReturnedToTheModelAsError()
    {
        _sender.Send(Arg.Any<GetDonorByIdQuery>(), Arg.Any<CancellationToken>()).Throws(new NotFoundException("Donor not found."));

        var output = JsonDocument.Parse(await InvokeAsync("get_donor", new() { ["donorId"] = Guid.NewGuid() })).RootElement;

        Assert.Equal("Donor not found.", output.GetProperty("error").GetString());
    }

    [Fact]
    public async Task ValidationFailure_IsReturnedToTheModelAsError()
    {
        _sender.Send(Arg.Any<GetManagerActivityQuery>(), Arg.Any<CancellationToken>())
            .Throws(new ValidationException([new ValidationFailure("Period", "period must be 'weekly' or 'monthly'.")]));

        var output = JsonDocument.Parse(await InvokeAsync("get_manager_activity", new() { ["period"] = "daily" })).RootElement;

        Assert.Equal("period must be 'weekly' or 'monthly'.", output.GetProperty("error").GetString());
    }
}
