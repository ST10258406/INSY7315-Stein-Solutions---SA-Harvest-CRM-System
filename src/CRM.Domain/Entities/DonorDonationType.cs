namespace CRM.Domain.Entities;

using CRM.Domain.Entities.Lookups;

/// <summary>
/// Junction table resolving the many-to-many between Donor and donation type.
/// Composite key (DonorId, DonationTypeId) — no surrogate Id.
/// </summary>
public class DonorDonationType
{
    public Guid DonorId { get; set; }
    public Donor Donor { get; set; } = null!;

    public short DonationTypeId { get; set; }
    public LookupDonationType DonationType { get; set; } = null!;
}
