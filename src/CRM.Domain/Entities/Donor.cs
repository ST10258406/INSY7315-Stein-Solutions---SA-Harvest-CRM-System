namespace CRM.Domain.Entities;

using CRM.Domain.Common;
using CRM.Domain.Entities.Lookups;
using CRM.Domain.Enums;

public class Donor : BaseEntity
{
    public string CompanyName { get; set; } = string.Empty;
    public short CompanyTypeId { get; set; }
    public LookupCompanyType CompanyType { get; set; } = null!;

    public string? Website { get; set; }
    public string RegisteredCompanyName { get; set; } = string.Empty;
    public string? TradingName { get; set; }

    public short EntityTypeId { get; set; }
    public LookupEntityType EntityType { get; set; } = null!;

    public string? CompanyRegistrationNumber { get; set; }
    public string? IncomeTaxNumber { get; set; }

    public short DonationFrequencyId { get; set; }
    public LookupDonationFrequency DonationFrequency { get; set; } = null!;

    public short? BbbeeStatusId { get; set; }
    public LookupBbbeeStatus? BbbeeStatus { get; set; }

    public string? CollectionAddress { get; set; }
    public string? OperationsLogisticsDetails { get; set; }
    public string? AdditionalInformation { get; set; }

    public Guid? RelationshipManagerId { get; set; }
    public User? RelationshipManager { get; set; }

    public DonorStatus Status { get; set; } = DonorStatus.PendingReview;
    public SubmissionSource SubmissionSource { get; set; }

    public bool MarketingConsent { get; set; }
    public DateTime? MarketingConsentDate { get; set; }
    public string? ImpactReportingPreferences { get; set; }
    public DateTime? FollowUpDate { get; set; }
    public string? FoodspaceCompanyId { get; set; }

    public Guid CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;

    public DonorLegalAddress? LegalAddress { get; set; }
    public ICollection<DonorContact> Contacts { get; set; } = new List<DonorContact>();
    public ICollection<DonorOperationalRegion> OperationalRegions { get; set; } = new List<DonorOperationalRegion>();
    public ICollection<DonorDonationType> DonationTypes { get; set; } = new List<DonorDonationType>();
    public ICollection<DonorDocument> Documents { get; set; } = new List<DonorDocument>();
    public ICollection<InteractionLog> InteractionLogs { get; set; } = new List<InteractionLog>();
    public ICollection<DonorTask> Tasks { get; set; } = new List<DonorTask>();
    public ICollection<DonorApproval> Approvals { get; set; } = new List<DonorApproval>();
}
