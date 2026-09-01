using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Tasks.Commands.UpdateTask;
using CRM.Application.Modules.Tasks.Dtos;
using CRM.Domain.Entities;
using NSubstitute;

namespace CRM.Application.Tests.Modules.Tasks.Commands;

public class UpdateTaskCommandHandlerTests
{
    private readonly ITaskRepository _tasksMock = Substitute.For<ITaskRepository>();
    private readonly IUserRepository _usersMock = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _unitOfWorkMock = Substitute.For<IUnitOfWork>();
    private readonly UpdateTaskCommandHandler _handler;

    private DonorTask _task = null!;

    public UpdateTaskCommandHandlerTests()
    {
        _tasksMock.GetDtoByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ci => new TaskDto { Id = ci.ArgAt<Guid>(0) });
        _handler = new UpdateTaskCommandHandler(_tasksMock, _usersMock, _unitOfWorkMock);
    }

    private DonorTask SetupExistingTask()
    {
        _task = new DonorTask
        {
            Id = Guid.NewGuid(),
            DonorId = Guid.NewGuid(),
            Title = "Original",
            Description = "Original desc",
            DueDate = DateTime.UtcNow.Date.AddDays(10),
            AssignedToUserId = Guid.NewGuid(),
            CreatedByUserId = Guid.NewGuid()
        };
        _tasksMock.GetForUpdateAsync(_task.Id, Arg.Any<CancellationToken>()).Returns(_task);
        return _task;
    }

    [Fact]
    public async Task Handle_AppliesOnlyProvidedFields()
    {
        var task = SetupExistingTask();
        var originalAssignee = task.AssignedToUserId;

        await _handler.Handle(new UpdateTaskCommand { Id = task.Id, Title = "New title" }, CancellationToken.None);

        Assert.Equal("New title", task.Title);
        Assert.Equal("Original desc", task.Description);           // untouched
        Assert.Equal(originalAssignee, task.AssignedToUserId);     // untouched
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DueDate_StoredAsUtcMidnight()
    {
        var task = SetupExistingTask();
        var due = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(4);

        await _handler.Handle(new UpdateTaskCommand { Id = task.Id, DueDate = due }, CancellationToken.None);

        Assert.Equal(due, DateOnly.FromDateTime(task.DueDate));
        Assert.Equal(DateTimeKind.Utc, task.DueDate.Kind);
    }

    [Fact]
    public async Task Handle_ReassignToActiveUser_Succeeds()
    {
        var task = SetupExistingTask();
        var newAssignee = Guid.NewGuid();
        _usersMock.ExistsAndActiveAsync(newAssignee, Arg.Any<CancellationToken>()).Returns(true);

        await _handler.Handle(new UpdateTaskCommand { Id = task.Id, AssignedToUserId = newAssignee }, CancellationToken.None);

        Assert.Equal(newAssignee, task.AssignedToUserId);
    }

    [Fact]
    public async Task Handle_ReassignToUnknownOrInactiveUser_ThrowsNotFound_NoSave()
    {
        var task = SetupExistingTask();
        _usersMock.ExistsAndActiveAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(new UpdateTaskCommand { Id = task.Id, AssignedToUserId = Guid.NewGuid() }, CancellationToken.None));

        await _unitOfWorkMock.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownTask_ThrowsNotFound()
    {
        _tasksMock.GetForUpdateAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((DonorTask?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(new UpdateTaskCommand { Id = Guid.NewGuid(), Title = "x" }, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_CapturesOldAndNewValuesForAudit()
    {
        var task = SetupExistingTask();
        var command = new UpdateTaskCommand { Id = task.Id, Title = "Renamed" };

        await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(task.Id, command.EntityId);
        Assert.NotNull(command.OldValues);
        Assert.NotNull(command.NewValues);
    }
}
