namespace CRM.Domain.Entities;

using CRM.Domain.Common;
using CRM.Domain.Enums;

public class DonorContact : BaseEntity
{
    public Guid DonorId { get; set; }
    public Donor Donor { get; set; } = null!;

    public ContactType ContactType { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? JobTitle { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
}
