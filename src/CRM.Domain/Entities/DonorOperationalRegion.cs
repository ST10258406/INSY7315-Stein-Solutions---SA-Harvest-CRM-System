namespace CRM.Domain.Entities;

using CRM.Domain.Entities.Lookups;

/// <summary>
/// Junction table resolving the many-to-many between Donor and operational region.
/// Composite key (DonorId, OperationalRegionId) — no surrogate Id.
/// </summary>
public class DonorOperationalRegion
{
    public Guid DonorId { get; set; }
    public Donor Donor { get; set; } = null!;

    public short OperationalRegionId { get; set; }
    public LookupOperationalRegion OperationalRegion { get; set; } = null!;
}
