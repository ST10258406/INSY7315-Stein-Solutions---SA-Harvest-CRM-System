namespace CRM.Application.Modules.Vetting.Submissions;

using CRM.Domain.Entities.Vetting;

/// <summary>
/// Compliance checks run on submit. Each returns a fixed reviewer-facing message (≤255).
/// Flags never block a submit; subtotal mismatches are flags, not validation errors, because
/// an officer on site may only have approximate numbers.
/// </summary>
public static class VettingFlagRules
{
    public static List<string> Evaluate(VettingAssessmentDetails d)
    {
        var flags = new List<string>();

        var genderTotal = d.FemalesServed + d.MalesServed;
        if (genderTotal != d.TotalPeopleServed)
            flags.Add($"Gender subtotals ({genderTotal}) do not match total people served ({d.TotalPeopleServed})");

        var raceTotal = d.AfricanServed + d.ColouredServed + d.IndianAsianServed + d.WhiteServed;
        if (raceTotal != d.TotalPeopleServed)
            flags.Add($"Race subtotals ({raceTotal}) do not match total people served ({d.TotalPeopleServed})");

        if (!d.IsRegisteredNpo) flags.Add("Organisation is not a registered NPO");
        if (d.IsRegisteredNpo && d.NpoCertificatePath is null) flags.Add("NPO registration claimed but no NPO certificate uploaded");
        if (!d.IsRegisteredDsd) flags.Add("Organisation is not registered with DSD");

        if (!d.KitchenClean) flags.Add("Kitchen hygiene below standard");
        if (!d.WaterAccess) flags.Add("No access to clean water & sanitation");
        if (!d.ToiletFacilities) flags.Add("Inadequate toilet facilities");
        if (!d.PestFree) flags.Add("Evidence of pests or rodents");

        if (!d.DeliveryAccessEase) flags.Add("Site not accessible to delivery vehicles");
        if (!d.ParkingAndSecurity) flags.Add("No safe loading zone / site security");

        if (!d.SlaSigned) flags.Add("SLA not signed");
        if (!d.ConsentFormSigned) flags.Add("Beneficiary consent form not signed");
        if (!d.PolicyDocumentPresent) flags.Add("Operational policy document not verified");

        if (d.FullTimeFemales + d.FullTimeMales + d.VolunteersCount == 0) flags.Add("No staff or volunteers recorded");

        return flags;
    }
}
