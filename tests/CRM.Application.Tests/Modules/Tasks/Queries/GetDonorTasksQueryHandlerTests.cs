using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Common.Models;
using CRM.Application.Modules.Tasks.Dtos;
using CRM.Application.Modules.Tasks.Queries.GetDonorTasks;
using NSubstitute;

namespace CRM.Application.Tests.Modules.Tasks.Queries;

public class GetDonorTasksQueryHandlerTests
{
    private readonly IDonorRepository _donorsMock = Substitute.For<IDonorRepository>();
    private readonly ITaskRepository _tasksMock = Substitute.For<ITaskRepository>();
    private readonly GetDonorTasksQueryHandler _handler;

    private TaskSearchCriteria? _captured;

    public GetDonorTasksQueryHandlerTests()
    {
        _tasksMock.GetTasksAsync(Arg.Any<TaskSearchCriteria>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                _captured = ci.ArgAt<TaskSearchCriteria>(0);
                return (new List<TaskDto>(), 0);
            });

        _handler = new GetDonorTasksQueryHandler(_donorsMock, _tasksMock);
    }

    private Guid SetupExistingDonor()
    {
        var donorId = Guid.NewGuid();
        _donorsMock.ExistsAsync(donorId, Arg.Any<CancellationToken>()).Returns(true);
        return donorId;
    }

    [Fact]
    public async Task Handle_UnknownDonor_ThrowsNotFoundAndNeverQueriesTasks()
    {
        _donorsMock.ExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(new GetDonorTasksQuery { DonorId = Guid.NewGuid() }, CancellationToken.None));

        await _tasksMock.DidNotReceive().GetTasksAsync(Arg.Any<TaskSearchCriteria>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ScopesToTheDonorAndNeverToAnAssignee()
    {
        var donorId = SetupExistingDonor();

        await _handler.Handle(new GetDonorTasksQuery { DonorId = donorId }, CancellationToken.None);

        Assert.Equal(donorId, _captured!.DonorId);
        Assert.Null(_captured.AssignedToUserId);
    }

    [Theory]
    [InlineData("all", null)]
    [InlineData("true", true)]
    [InlineData("false", false)]
    [InlineData("TRUE", true)]
    public async Task Handle_TranslatesTriStateIsCompleted(string raw, bool? expected)
    {
        var donorId = SetupExistingDonor();

        await _handler.Handle(new GetDonorTasksQuery { DonorId = donorId, IsCompleted = raw }, CancellationToken.None);

        Assert.Equal(expected, _captured!.IsCompleted);
    }

    [Fact]
    public async Task Handle_DefaultIsCompleted_IsAll()
    {
        var donorId = SetupExistingDonor();

        await _handler.Handle(new GetDonorTasksQuery { DonorId = donorId }, CancellationToken.None);

        Assert.Null(_captured!.IsCompleted);
    }

    [Fact]
    public async Task Handle_WrapsResultInEnvelope()
    {
        var donorId = SetupExistingDonor();
        var items = new List<TaskDto> { new() { Id = Guid.NewGuid() } };
        _tasksMock.GetTasksAsync(Arg.Any<TaskSearchCriteria>(), Arg.Any<CancellationToken>()).Returns((items, 7));

        var result = await _handler.Handle(
            new GetDonorTasksQuery { DonorId = donorId, Page = 1, PageSize = 5 }, CancellationToken.None);

        Assert.Same(items[0], result.Data.Single());
        Assert.Equal(7, result.Pagination.TotalCount);
        Assert.Equal(2, result.Pagination.TotalPages);
    }
}
