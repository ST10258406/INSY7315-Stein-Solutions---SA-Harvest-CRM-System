namespace CRM.Application.Common.Models;

/// <summary>
/// Parameter object for <see cref="Interfaces.IUserRepository.SearchAsync"/>.
/// Carries only already-parsed, already-validated values, mirroring <see cref="DonorSearchCriteria"/>.
/// </summary>
public record UserSearchCriteria
{
    public string? Search { get; init; }
    public Guid? RoleId { get; init; }
    public bool? IsActive { get; init; }

    public string? SortBy { get; init; }
    public string SortDir { get; init; } = "asc";

    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
