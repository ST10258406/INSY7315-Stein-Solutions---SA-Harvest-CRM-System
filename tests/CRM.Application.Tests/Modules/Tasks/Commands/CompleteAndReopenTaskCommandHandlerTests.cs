using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Tasks.Commands.CompleteTask;
using CRM.Application.Modules.Tasks.Commands.ReopenTask;
using CRM.Domain.Entities;
using NSubstitute;
using AppValidationException = CRM.Application.Common.Exceptions.ValidationException;
using NotFoundException = CRM.Application.Common.Exceptions.NotFoundException;

namespace CRM.Application.Tests.Modules.Tasks.Commands;

public class CompleteAndReopenTaskCommandHandlerTests
{
    private readonly ITaskRepository _tasksMock = Substitute.For<ITaskRepository>();
    private readonly IUnitOfWork _unitOfWorkMock = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserService _currentUserMock = Substitute.For<ICurrentUserService>();

    private readonly Guid _currentUserId = Guid.NewGuid();
    private readonly CompleteTaskCommandHandler _complete;
    private readonly ReopenTaskCommandHandler _reopen;

    public CompleteAndReopenTaskCommandHandlerTests()
    {
        _currentUserMock.GetCurrentUserId().Returns(_currentUserId);

        _complete = new CompleteTaskCommandHandler(_tasksMock, _unitOfWorkMock, _currentUserMock);
        _reopen = new ReopenTaskCommandHandler(_tasksMock, _unitOfWorkMock);
    }

    private DonorTask Seed(bool completed)
    {
        var task = new DonorTask
        {
            Id = Guid.NewGuid(),
            Title = "T",
            AssignedToUserId = Guid.NewGuid(),
            CreatedByUserId = Guid.NewGuid(),
            DueDate = DateTime.UtcNow.Date.AddDays(3),
            IsCompleted = completed,
            CompletedAt = completed ? DateTime.UtcNow.AddDays(-1) : null,
            CompletedByUserId = completed ? Guid.NewGuid() : null
        };
        _tasksMock.GetForUpdateAsync(task.Id, Arg.Any<CancellationToken>()).Returns(task);
        return task;
    }

    [Fact]
    public async Task Complete_OpenTask_SetsCompletionFieldsToTheCaller()
    {
        var task = Seed(completed: false);
        var before = DateTime.UtcNow;

        await _complete.Handle(new CompleteTaskCommand { Id = task.Id }, CancellationToken.None);

        Assert.True(task.IsCompleted);
        Assert.NotNull(task.CompletedAt);
        Assert.InRange(task.CompletedAt!.Value, before, DateTime.UtcNow.AddSeconds(1));
        Assert.Equal(_currentUserId, task.CompletedByUserId);
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Complete_AlreadyComplete_Throws400()
    {
        var task = Seed(completed: true);

        await Assert.ThrowsAsync<AppValidationException>(() =>
            _complete.Handle(new CompleteTaskCommand { Id = task.Id }, CancellationToken.None));

        await _unitOfWorkMock.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Complete_UnknownTask_ThrowsNotFound()
    {
        _tasksMock.GetForUpdateAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((DonorTask?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _complete.Handle(new CompleteTaskCommand { Id = Guid.NewGuid() }, CancellationToken.None));
    }

    [Fact]
    public async Task Reopen_CompletedTask_ClearsAllCompletionFields()
    {
        var task = Seed(completed: true);

        await _reopen.Handle(new ReopenTaskCommand { Id = task.Id }, CancellationToken.None);

        Assert.False(task.IsCompleted);
        Assert.Null(task.CompletedAt);
        Assert.Null(task.CompletedByUserId);
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Reopen_NotCompleted_Throws400()
    {
        var task = Seed(completed: false);

        await Assert.ThrowsAsync<AppValidationException>(() =>
            _reopen.Handle(new ReopenTaskCommand { Id = task.Id }, CancellationToken.None));

        await _unitOfWorkMock.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Reopen_UnknownTask_ThrowsNotFound()
    {
        _tasksMock.GetForUpdateAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((DonorTask?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _reopen.Handle(new ReopenTaskCommand { Id = Guid.NewGuid() }, CancellationToken.None));
    }
}
