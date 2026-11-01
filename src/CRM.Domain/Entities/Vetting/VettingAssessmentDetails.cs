namespace CRM.Domain.Entities.Vetting;

using CRM.Domain.Common;

/// <summary>
/// One-to-one with <see cref="VettingSubmission"/>: the 11 sections of audit answers.
/// List values are display strings from VettingOptions. *Path columns are blob paths,
/// set only by the upload endpoints.
/// </summary>
public class VettingAssessmentDetails : BaseEntity
{
    public Guid SubmissionId { get; set; }
    public VettingSubmission Submission { get; set; } = null!;

    // 1 Organisation & Contact
    public string LegalName { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public string? Website { get; set; }

    // 2 Location
    public string? AddressPhysical { get; set; }
    public string? AddressDetail { get; set; }
    public string Province { get; set; } = string.Empty;
    public string What3Words { get; set; } = string.Empty;

    // 3 Programmes & Services
    public List<string> CoreFocusAreas { get; set; } = [];
    public List<string> TargetPopulation { get; set; } = [];
    public string ProgrammesSummary { get; set; } = string.Empty;
    public string DistributionChannel { get; set; } = string.Empty;

    // 4 Staffing
    public int FullTimeFemales { get; set; }
    public int FullTimeMales { get; set; }
    public int VolunteersCount { get; set; }

    // 5 Registration
    public bool IsRegisteredNpo { get; set; }
    public string? NpoCertificatePath { get; set; }
    public bool IsRegisteredDsd { get; set; }
    public string? PboCertificatePath { get; set; }

    // 6 Demographics
    public List<string> BeneficiaryRaces { get; set; } = [];
    public List<string> BeneficiaryGenders { get; set; } = [];
    public List<string> BeneficiaryAgeGroups { get; set; } = [];

    // 7 Feeding Operation
    public string FeedingFrequency { get; set; } = string.Empty;
    public int TotalPeopleServed { get; set; }
    public int FemalesServed { get; set; }
    public int MalesServed { get; set; }
    public int AfricanServed { get; set; }
    public int ColouredServed { get; set; }
    public int IndianAsianServed { get; set; }
    public int WhiteServed { get; set; }
    public string SaHarvestReliance { get; set; } = string.Empty;
    public string TransportCapacity { get; set; } = string.Empty;
    public List<string> MealsProvided { get; set; } = [];
    public List<string> FeedingDays { get; set; } = [];
    public DateOnly? LastFedDate { get; set; }

    // 8 Facilities & Hygiene
    public List<string> FoodStorageTypes { get; set; } = [];
    public string? KitchenStoragePhotosPath { get; set; }
    public bool KitchenClean { get; set; }
    public bool WaterAccess { get; set; }
    public bool ToiletFacilities { get; set; }
    public bool PestFree { get; set; }
    public List<string> InfrastructureChecks { get; set; } = [];

    // 9 Access & Security
    public bool DeliveryAccessEase { get; set; }
    public bool ParkingAndSecurity { get; set; }
    public string? PoliceStationProximity { get; set; }

    // 10 Capacity Notes
    public string? OfficerComments { get; set; }
    public string? ProposalCapabilities { get; set; }
    public string? DigitalCapabilities { get; set; }
    public string? FacilityPhotosPath { get; set; }

    // 11 Documents
    public bool SlaSigned { get; set; }
    public bool ConsentFormSigned { get; set; }
    public bool PolicyDocumentPresent { get; set; }
    public string? CertificatesPath { get; set; }
}
