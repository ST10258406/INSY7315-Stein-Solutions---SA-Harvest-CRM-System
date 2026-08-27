namespace CRM.Application.Common.Models;

using CRM.Domain.Enums;

/// <summary>
/// Parameter object for <see cref="Common.Interfaces.IDonorRepository.SearchAsync"/>.
/// Carries only already-parsed, already-validated values so the repository never has to
/// interpret raw request strings — except <see cref="SortBy"/>/<see cref="SortDir"/>,
/// which the repository matches against a fixed whitelist.
/// </summary>
public record DonorSearchCriteria
{
    public string? Search { get; init; }
    public DonorStatus? Status { get; init; }
    public short? CompanyTypeId { get; init; }
    public string? RegionCode { get; init; }
    public short? DonationTypeId { get; init; }
    public short? DonationFrequencyId { get; init; }
    public Guid? RelationshipManagerId { get; init; }
    public DateTime? FollowUpBefore { get; init; }

    public string? SortBy { get; init; }
    public string SortDir { get; init; } = "asc";

    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
