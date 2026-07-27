namespace CRM.Application.Common.Models;

public record PaginatedResult<T>
{
    public IReadOnlyList<T> Data { get; init; } = [];
    public PaginationMeta Pagination { get; init; } = null!;

    public static PaginatedResult<T> Create(IReadOnlyList<T> data, int page, int pageSize, int totalCount) =>
        new()
        {
            Data = data,
            Pagination = new PaginationMeta { Page = page, PageSize = pageSize, TotalCount = totalCount }
        };
}
