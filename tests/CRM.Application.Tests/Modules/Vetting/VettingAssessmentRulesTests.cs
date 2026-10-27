using CRM.Application.Modules.Vetting.Submissions.Commands.CreateSubmission;
using CRM.Application.Modules.Vetting.Submissions.Dtos;
using CRM.Application.Modules.Vetting.Submissions.Validation;

namespace CRM.Application.Tests.Modules.Vetting;

public class VettingAssessmentRulesTests
{
    private readonly VettingAssessmentRules _rules = new();

    [Fact]
    public void ValidDetails_Pass() => Assert.True(_rules.Validate(VettingFixtures.ValidDetailsDto()).IsValid);

    [Fact]
    public void EmptyInfrastructureChecks_IsAllowed()
    {
        var d = VettingFixtures.ValidDetailsDto();
        d.InfrastructureChecks = [];
        Assert.True(_rules.Validate(d).IsValid);
    }

    public static TheoryData<string, Action<VettingAssessmentDetailsDto>> OneFailurePerSection => new()
    {
        { "ContactEmail", d => d.ContactEmail = "not-an-email" },                    // 1
        { "Website", d => d.Website = "ftp://example.com" },                          // 1
        { "What3Words", d => d.What3Words = "apple.orange.banana" },                  // 2
        { "Province", d => d.Province = "Gauteng North" },                            // 2
        { "CoreFocusAreas", d => d.CoreFocusAreas = ["Feeding", "Space travel"] },    // 3 value outside its list, in an array
        { "TargetPopulation", d => d.TargetPopulation = [] },                         // 3 empty required array
        { "FullTimeMales", d => d.FullTimeMales = -1 },                               // 4
        { "IsRegisteredNpo", d => d.IsRegisteredNpo = null },                         // 5 missing boolean
        { "BeneficiaryAgeGroups", d => d.BeneficiaryAgeGroups = ["0-5"] },            // 6 hyphen, not en dash
        { "TransportCapacity", d => d.TransportCapacity = "Bicycle" },                // 7
        { "LastFedDate", d => d.LastFedDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1) }, // 7 future
        { "FeedingDays", d => d.FeedingDays = ["Mon", "Mon"] },                       // 7 duplicate
        { "InfrastructureChecks", d => d.InfrastructureChecks = ["Plumbing"] },       // 8
        { "PestFree", d => d.PestFree = null },                                       // 8
        { "ParkingAndSecurity", d => d.ParkingAndSecurity = null },                   // 9
        { "PoliceStationProximity", d => d.PoliceStationProximity = new string('a', 101) }, // 9
        { "OfficerComments", d => d.OfficerComments = new string('a', 4001) },        // 10
        { "SlaSigned", d => d.SlaSigned = null },                                     // 11
    };

    [Theory]
    [MemberData(nameof(OneFailurePerSection))]
    public void InvalidField_FailsOnThatFieldOnly(string field, Action<VettingAssessmentDetailsDto> breakIt)
    {
        var d = VettingFixtures.ValidDetailsDto();
        breakIt(d);

        var result = _rules.Validate(d);

        Assert.False(result.IsValid);
        Assert.All(result.Errors, e => Assert.Equal(field, e.PropertyName));
    }

    [Fact]
    public void CreateCommand_ErrorKeysAreNestedUnderDetails()
    {
        var d = VettingFixtures.ValidDetailsDto();
        d.ContactEmail = "nope";
        var command = new CreateSubmissionCommand { DonorId = Guid.NewGuid(), OfficerId = Guid.NewGuid(), Details = d };

        var result = new CreateSubmissionCommandValidator().Validate(command);

        Assert.Contains(result.Errors, e => e.PropertyName == "Details.ContactEmail");
    }

    [Fact]
    public void CreateCommand_MissingDetails_Fails()
        => Assert.False(new CreateSubmissionCommandValidator()
            .Validate(new CreateSubmissionCommand { DonorId = Guid.NewGuid(), OfficerId = Guid.NewGuid() }).IsValid);
}
