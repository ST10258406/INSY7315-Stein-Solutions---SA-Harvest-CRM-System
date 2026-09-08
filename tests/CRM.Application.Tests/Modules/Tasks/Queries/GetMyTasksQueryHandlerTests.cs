using CRM.Application.Common.Interfaces;
using CRM.Application.Common.Models;
using CRM.Application.Modules.Tasks.Dtos;
using CRM.Application.Modules.Tasks.Queries.GetMyTasks;
using NSubstitute;

namespace CRM.Application.Tests.Modules.Tasks.Queries;

public class GetMyTasksQueryHandlerTests
{
    private readonly ITaskRepository _tasksMock = Substitute.For<ITaskRepository>();
    private readonly ICurrentUserService _currentUserMock = Substitute.For<ICurrentUserService>();
    private readonly GetMyTasksQueryHandler _handler;

    private readonly Guid _currentUserId = Guid.NewGuid();
    private TaskSearchCriteria? _captured;

    public GetMyTasksQueryHandlerTests()
    {
        _currentUserMock.GetCurrentUserId().Returns(_currentUserId);

        _tasksMock.GetTasksAsync(Arg.Any<TaskSearchCriteria>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                _captured = ci.ArgAt<TaskSearchCriteria>(0);
                return (new List<TaskDto>(), 0);
            });

        _handler = new GetMyTasksQueryHandler(_tasksMock, _currentUserMock);
    }

    [Fact]
    public async Task Handle_AlwaysScopesToTheCallerRegardlessOfRequest()
    {
        await _handler.Handle(new GetMyTasksQuery(), CancellationToken.None);

        Assert.Equal(_currentUserId, _captured!.AssignedToUserId);
    }

    [Fact]
    public async Task Handle_MapsFiltersOntoCriteria()
    {
        var donorId = Guid.NewGuid();

        await _handler.Handle(new GetMyTasksQuery
        {
            IsCompleted = true,
            DueBefore = new DateOnly(2026, 9, 30),
            DonorId = donorId,
            Page = 3,
            PageSize = 15
        }, CancellationToken.None);

        Assert.True(_captured!.IsCompleted);
        Assert.Equal(donorId, _captured.DonorId);
        Assert.Equal(new DateOnly(2026, 9, 30).ToDateTime(TimeOnly.MaxValue), _captured.DueBefore);
        Assert.Equal(3, _captured.Page);
        Assert.Equal(15, _captured.PageSize);
    }

    [Fact]
    public async Task Handle_DefaultIsCompleted_IsFalse()
    {
        await _handler.Handle(new GetMyTasksQuery(), CancellationToken.None);

        Assert.False(_captured!.IsCompleted);
    }

    [Fact]
    public async Task Handle_NoDueBefore_LeavesCriteriaNull()
    {
        await _handler.Handle(new GetMyTasksQuery(), CancellationToken.None);

        Assert.Null(_captured!.DueBefore);
    }

    [Fact]
    public async Task Handle_WrapsRepositoryResultInEnvelope()
    {
        var items = new List<TaskDto> { new() { Id = Guid.NewGuid(), Title = "Call donor" } };
        _tasksMock.GetTasksAsync(Arg.Any<TaskSearchCriteria>(), Arg.Any<CancellationToken>()).Returns((items, 12));

        var result = await _handler.Handle(new GetMyTasksQuery { Page = 2, PageSize = 5 }, CancellationToken.None);

        Assert.Same(items[0], result.Data.Single());
        Assert.Equal(2, result.Pagination.Page);
        Assert.Equal(12, result.Pagination.TotalCount);
        Assert.Equal(3, result.Pagination.TotalPages);
    }
}
