using CRM.Application.Modules.Vetting.Submissions.Dtos;
using CRM.Domain.Entities.Vetting;

namespace CRM.Application.Tests.Modules.Vetting;

/// <summary>A fully valid, flag-free assessment, as request DTO and as entity.</summary>
internal static class VettingFixtures
{
    public static VettingAssessmentDetailsDto ValidDetailsDto() => new()
    {
        LegalName = "Ubuntu Food Co (Pty) Ltd",
        ContactName = "Sipho Dlamini",
        ContactEmail = "sipho@example.com",
        ContactPhone = "+27 82 000 0000",
        Website = "https://ubuntu.example.com",
        Province = "Gauteng",
        What3Words = "///apple.orange.banana",
        CoreFocusAreas = ["Feeding"],
        TargetPopulation = ["Children", "Elderly"],
        ProgrammesSummary = "Daily soup kitchen.",
        DistributionChannel = "Direct to beneficiaries",
        FullTimeFemales = 2,
        FullTimeMales = 1,
        VolunteersCount = 5,
        IsRegisteredNpo = false,
        IsRegisteredDsd = true,
        BeneficiaryRaces = ["African", "Indian/Asian"],
        BeneficiaryGenders = ["Female", "Male"],
        BeneficiaryAgeGroups = ["0–5", "60+"],
        FeedingFrequency = "Weekdays only",
        TotalPeopleServed = 50,
        FemalesServed = 30,
        MalesServed = 20,
        AfricanServed = 40,
        ColouredServed = 5,
        IndianAsianServed = 5,
        WhiteServed = 0,
        SaHarvestReliance = "High",
        TransportCapacity = "None — delivery required",
        MealsProvided = ["Lunch"],
        FeedingDays = ["Mon", "Wed", "Fri"],
        LastFedDate = new DateOnly(2026, 1, 15),
        FoodStorageTypes = ["Dry", "Chilled"],
        KitchenClean = true,
        WaterAccess = true,
        ToiletFacilities = true,
        PestFree = true,
        InfrastructureChecks = [],
        DeliveryAccessEase = true,
        ParkingAndSecurity = true,
        SlaSigned = true,
        ConsentFormSigned = true,
        PolicyDocumentPresent = true
    };

    /// <summary>Same answers as an entity, but registered as an NPO with its certificate, so no rule fires.</summary>
    public static VettingAssessmentDetails CleanDetails() => new()
    {
        TotalPeopleServed = 50,
        FemalesServed = 30,
        MalesServed = 20,
        AfricanServed = 40,
        ColouredServed = 5,
        IndianAsianServed = 5,
        IsRegisteredNpo = true,
        NpoCertificatePath = "vetting/x/files/npo-certificate.pdf",
        IsRegisteredDsd = true,
        KitchenClean = true,
        WaterAccess = true,
        ToiletFacilities = true,
        PestFree = true,
        DeliveryAccessEase = true,
        ParkingAndSecurity = true,
        SlaSigned = true,
        ConsentFormSigned = true,
        PolicyDocumentPresent = true,
        VolunteersCount = 1
    };
}
