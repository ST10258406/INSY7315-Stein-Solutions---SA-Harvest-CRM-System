using CRM.Application.Common.Interfaces;
using CRM.Application.Common.Models;
using CRM.Application.Modules.Users.Dtos;
using CRM.Application.Modules.Users.Queries.GetUsers;
using NSubstitute;

namespace CRM.Application.Tests.Modules.Users.Queries;

// Filtering, sorting and pagination SQL live in UserRepository. What remains here is the
// handler's own job: translating the query into UserSearchCriteria and assembling the
// PaginatedResult envelope.
public class GetUsersQueryHandlerTests
{
    private readonly IUserRepository _usersMock = Substitute.For<IUserRepository>();
    private readonly GetUsersQueryHandler _handler;

    private UserSearchCriteria? _capturedCriteria;
    private List<UserListItemDto> _items = new();
    private int _totalCount;

    public GetUsersQueryHandlerTests()
    {
        _usersMock
            .SearchAsync(Arg.Any<UserSearchCriteria>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                _capturedCriteria = ci.ArgAt<UserSearchCriteria>(0);
                return (_items, _totalCount);
            });

        _handler = new GetUsersQueryHandler(_usersMock);
    }

    [Fact]
    public async Task Handle_MapsEveryFilterOntoTheCriteria()
    {
        var roleId = Guid.NewGuid();
        var query = new GetUsersQuery
        {
            Search = "thabo",
            RoleId = roleId,
            IsActive = false,
            SortBy = "email",
            SortDir = "desc",
            Page = 3,
            PageSize = 50
        };

        await _handler.Handle(query, CancellationToken.None);

        Assert.NotNull(_capturedCriteria);
        Assert.Equal("thabo", _capturedCriteria!.Search);
        Assert.Equal(roleId, _capturedCriteria.RoleId);
        Assert.False(_capturedCriteria.IsActive);
        Assert.Equal("email", _capturedCriteria.SortBy);
        Assert.Equal("desc", _capturedCriteria.SortDir);
        Assert.Equal(3, _capturedCriteria.Page);
        Assert.Equal(50, _capturedCriteria.PageSize);
    }

    [Fact]
    public async Task Handle_NoFilters_LeavesOptionalCriteriaNull()
    {
        await _handler.Handle(new GetUsersQuery(), CancellationToken.None);

        Assert.Null(_capturedCriteria!.Search);
        Assert.Null(_capturedCriteria.RoleId);
        Assert.Null(_capturedCriteria.IsActive);
    }

    [Fact]
    public async Task Handle_WrapsRepositoryResultInPaginatedEnvelope()
    {
        _items = new List<UserListItemDto>
        {
            new() { Id = Guid.NewGuid(), FirstName = "Thabo", LastName = "Mokoena", Email = "thabo@saharvest.org", Role = "Admin", IsActive = true }
        };
        _totalCount = 41;

        var result = await _handler.Handle(new GetUsersQuery { Page = 2, PageSize = 20 }, CancellationToken.None);

        Assert.Single(result.Data);
        Assert.Equal(41, result.Pagination.TotalCount);
        Assert.Equal(2, result.Pagination.Page);
        Assert.Equal(20, result.Pagination.PageSize);
        Assert.Equal(3, result.Pagination.TotalPages);
    }
}
