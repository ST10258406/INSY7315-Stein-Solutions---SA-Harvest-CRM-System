using CRM.Application.Modules.Vetting.Submissions;
using CRM.Domain.Entities.Vetting;

namespace CRM.Application.Tests.Modules.Vetting;

public class VettingFlagRulesTests
{
    [Fact]
    public void CleanDetails_RaiseNoFlags() => Assert.Empty(VettingFlagRules.Evaluate(VettingFixtures.CleanDetails()));

    public static TheoryData<string, Action<VettingAssessmentDetails>> EachRule => new()
    {
        { "Gender subtotals (49) do not match total people served (50)", d => d.MalesServed = 19 },
        { "Race subtotals (51) do not match total people served (50)", d => d.WhiteServed = 1 },
        { "Organisation is not a registered NPO", d => { d.IsRegisteredNpo = false; d.NpoCertificatePath = null; } },
        { "NPO registration claimed but no NPO certificate uploaded", d => d.NpoCertificatePath = null },
        { "Organisation is not registered with DSD", d => d.IsRegisteredDsd = false },
        { "Kitchen hygiene below standard", d => d.KitchenClean = false },
        { "No access to clean water & sanitation", d => d.WaterAccess = false },
        { "Inadequate toilet facilities", d => d.ToiletFacilities = false },
        { "Evidence of pests or rodents", d => d.PestFree = false },
        { "Site not accessible to delivery vehicles", d => d.DeliveryAccessEase = false },
        { "No safe loading zone / site security", d => d.ParkingAndSecurity = false },
        { "SLA not signed", d => d.SlaSigned = false },
        { "Beneficiary consent form not signed", d => d.ConsentFormSigned = false },
        { "Operational policy document not verified", d => d.PolicyDocumentPresent = false },
        { "No staff or volunteers recorded", d => d.VolunteersCount = 0 },
    };

    [Theory]
    [MemberData(nameof(EachRule))]
    public void EachRule_FiresOnItsOwn(string expected, Action<VettingAssessmentDetails> breakIt)
    {
        var details = VettingFixtures.CleanDetails();
        breakIt(details);

        Assert.Equal([expected], VettingFlagRules.Evaluate(details));
    }
}
