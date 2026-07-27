namespace CRM.Application.Common.Models;

/// <summary>
/// Base record inherited by every list Query. Frontend sends these as query params.
/// </summary>
public record PaginationParams
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? SortBy { get; init; }
    public string SortDir { get; init; } = "asc";
}
