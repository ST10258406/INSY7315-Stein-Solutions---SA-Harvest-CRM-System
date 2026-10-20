using CRM.Application.Modules.Vetting.Officers.Commands.CreateOfficer;
using CRM.Application.Modules.Vetting.Officers.Commands.UpdateOfficer;
using CRM.Application.Modules.Vetting.SiteVisits.Commands.CreateSiteVisit;
using CRM.Application.Modules.Vetting.SiteVisits.Commands.UpdateSiteVisit;
using CRM.Application.Modules.Vetting.SiteVisits.Queries.GetSiteVisits;
using CRM.Application.Modules.Vetting.Submissions.Queries.GetSubmissions;

namespace CRM.Application.Tests.Modules.Vetting;

public class VettingValidatorTests
{
    private static CreateOfficerCommand ValidOfficer() => new()
    {
        FullName = "Thandi Mokoena",
        Initials = "TM",
        AssignedRegion = "Gauteng North"
    };

    private static CreateSiteVisitCommand ValidVisit() => new()
    {
        DonorId = Guid.NewGuid(),
        OfficerId = Guid.NewGuid(),
        ScheduledDate = new DateOnly(2026, 10, 21),
        ScheduledTime = new TimeOnly(9, 30)
    };

    [Fact]
    public void CreateOfficer_Valid_Passes() => Assert.True(new CreateOfficerCommandValidator().Validate(ValidOfficer()).IsValid);

    [Theory]
    [InlineData("ABCDEF")] // over 5
    [InlineData("tm")]     // not uppercase
    [InlineData("T.M")]    // not letters
    [InlineData("")]
    public void CreateOfficer_BadInitials_Fails(string initials)
    {
        var c = ValidOfficer();
        c.Initials = initials;
        Assert.False(new CreateOfficerCommandValidator().Validate(c).IsValid);
    }

    [Fact]
    public void CreateOfficer_NameOver100_Fails()
    {
        var c = ValidOfficer();
        c.FullName = new string('a', 101);
        Assert.False(new CreateOfficerCommandValidator().Validate(c).IsValid);
    }

    [Fact]
    public void CreateOfficer_RegionOver50_Fails()
    {
        var c = ValidOfficer();
        c.AssignedRegion = new string('a', 51);
        Assert.False(new CreateOfficerCommandValidator().Validate(c).IsValid);
    }

    [Fact]
    public void UpdateOfficer_OnlyNullFields_Passes()
        => Assert.True(new UpdateOfficerCommandValidator().Validate(new UpdateOfficerCommand { Id = Guid.NewGuid() }).IsValid);

    [Fact]
    public void UpdateOfficer_LowercaseInitials_Fails()
        => Assert.False(new UpdateOfficerCommandValidator().Validate(new UpdateOfficerCommand { Id = Guid.NewGuid(), Initials = "ab" }).IsValid);

    [Fact]
    public void CreateSiteVisit_Valid_WithoutLocationLabel_Passes()
        => Assert.True(new CreateSiteVisitCommandValidator().Validate(ValidVisit()).IsValid);

    [Fact]
    public void CreateSiteVisit_MissingDateOrTime_Fails()
    {
        var noDate = ValidVisit();
        noDate.ScheduledDate = null;
        var noTime = ValidVisit();
        noTime.ScheduledTime = null;
        Assert.False(new CreateSiteVisitCommandValidator().Validate(noDate).IsValid);
        Assert.False(new CreateSiteVisitCommandValidator().Validate(noTime).IsValid);
    }

    [Fact]
    public void CreateSiteVisit_LabelOver100OrNotesOver255_Fails()
    {
        var longLabel = ValidVisit();
        longLabel.LocationLabel = new string('a', 101);
        var longNotes = ValidVisit();
        longNotes.VisitNotes = new string('a', 256);
        Assert.False(new CreateSiteVisitCommandValidator().Validate(longLabel).IsValid);
        Assert.False(new CreateSiteVisitCommandValidator().Validate(longNotes).IsValid);
    }

    [Theory]
    [InlineData("InProgress", true)]
    [InlineData("completed", true)]
    [InlineData("Cancelled", false)]
    public void UpdateSiteVisit_VisitStatus_MustBeKnown(string status, bool valid)
        => Assert.Equal(valid, new UpdateSiteVisitCommandValidator().Validate(new UpdateSiteVisitCommand { Id = Guid.NewGuid(), VisitStatus = status }).IsValid);

    [Fact]
    public void GetSiteVisits_DateIsRequired()
        => Assert.False(new GetSiteVisitsQueryValidator().Validate(new GetSiteVisitsQuery()).IsValid);

    [Theory]
    [InlineData(null, "asc", true)]
    [InlineData("submittedAt", "asc", true)]
    [InlineData("SubmittedAt", "desc", true)]
    [InlineData("createdAt", "asc", false)]
    [InlineData("submittedAt", "sideways", false)]
    public void GetSubmissions_SortIsSubmittedAtOnly(string? sortBy, string sortDir, bool valid)
        => Assert.Equal(valid, new GetSubmissionsQueryValidator().Validate(new GetSubmissionsQuery { SortBy = sortBy, SortDir = sortDir }).IsValid);
}
