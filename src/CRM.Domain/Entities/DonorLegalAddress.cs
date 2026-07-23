namespace CRM.Domain.Entities;

using CRM.Domain.Common;
using CRM.Domain.Entities.Lookups;

public class DonorLegalAddress : BaseEntity
{
    public Guid DonorId { get; set; }
    public Donor Donor { get; set; } = null!;

    public string StreetAddress { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;

    public short ProvinceId { get; set; }
    public LookupProvince Province { get; set; } = null!;

    public string PostalCode { get; set; } = string.Empty;
}
