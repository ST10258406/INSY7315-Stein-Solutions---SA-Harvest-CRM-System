using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Tasks.Commands.CreateTask;
using CRM.Application.Modules.Tasks.Dtos;
using CRM.Domain.Entities;
using CRM.Domain.Enums;
using NSubstitute;

namespace CRM.Application.Tests.Modules.Tasks.Commands;

public class CreateTaskCommandHandlerTests
{
    private readonly IDonorRepository _donorsMock = Substitute.For<IDonorRepository>();
    private readonly IUserRepository _usersMock = Substitute.For<IUserRepository>();
    private readonly ITaskRepository _tasksMock = Substitute.For<ITaskRepository>();
    private readonly IUnitOfWork _unitOfWorkMock = Substitute.For<IUnitOfWork>();
    private readonly INotificationService _notificationsMock = Substitute.For<INotificationService>();
    private readonly ICurrentUserService _currentUserMock = Substitute.For<ICurrentUserService>();
    private readonly CreateTaskCommandHandler _handler;

    private readonly Guid _currentUserId = Guid.NewGuid();
    private readonly List<DonorTask> _added = [];

    public CreateTaskCommandHandlerTests()
    {
        _currentUserMock.GetCurrentUserId().Returns(_currentUserId);

        _tasksMock.AddAsync(Arg.Do<DonorTask>(t => _added.Add(t)), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        _tasksMock.GetDtoByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ci => new TaskDto { Id = ci.ArgAt<Guid>(0), Title = "Follow up" });

        _handler = new CreateTaskCommandHandler(
            _donorsMock, _usersMock, _tasksMock, _unitOfWorkMock, _notificationsMock, _currentUserMock);
    }

    private (Guid donorId, Guid assigneeId) SetupHappyPath()
    {
        var donorId = Guid.NewGuid();
        var assigneeId = Guid.NewGuid();
        _donorsMock.ExistsAsync(donorId, Arg.Any<CancellationToken>()).Returns(true);
        _usersMock.ExistsAndActiveAsync(assigneeId, Arg.Any<CancellationToken>()).Returns(true);
        return (donorId, assigneeId);
    }

    private static CreateTaskCommand MakeCommand(Guid donorId, Guid assigneeId) => new()
    {
        DonorId = donorId,
        Title = "Follow up on August donation",
        Description = "Confirm pickup schedule",
        AssignedToUserId = assigneeId,
        DueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(5)
    };

    [Fact]
    public async Task Handle_ValidCommand_StagesTaskSavesOnceAndReturnsDto()
    {
        var (donorId, assigneeId) = SetupHappyPath();
        var command = MakeCommand(donorId, assigneeId);

        var result = await _handler.Handle(command, CancellationToken.None);

        var task = Assert.Single(_added);
        Assert.Equal(donorId, task.DonorId);
        Assert.Equal(assigneeId, task.AssignedToUserId);
        Assert.Equal(_currentUserId, task.CreatedByUserId);
        Assert.Equal("Follow up on August donation", task.Title);
        Assert.False(task.IsCompleted);
        Assert.Equal(command.DueDate, DateOnly.FromDateTime(task.DueDate));
        Assert.Equal(DateTimeKind.Utc, task.DueDate.Kind);

        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        Assert.Equal(task.Id, result.Id);
        await _tasksMock.Received(1).GetDtoByIdAsync(task.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_FiresTaskAssignedNotificationToTheAssignee()
    {
        var (donorId, assigneeId) = SetupHappyPath();

        await _handler.Handle(MakeCommand(donorId, assigneeId), CancellationToken.None);

        await _notificationsMock.Received(1).CreateAsync(
            assigneeId,
            Arg.Any<string>(),
            Arg.Is<string>(m => m.Contains("Follow up on August donation")),
            NotificationType.TaskAssigned,
            _added[0].Id,
            "DonorTask",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SetsAuditEntityIdAndNewValues()
    {
        var (donorId, assigneeId) = SetupHappyPath();
        var command = MakeCommand(donorId, assigneeId);

        await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(_added[0].Id, command.EntityId);
        Assert.NotNull(command.NewValues);
    }

    [Fact]
    public async Task Handle_UnknownDonor_ThrowsNotFound_NoStagingNoNotify()
    {
        _donorsMock.ExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _handler.Handle(MakeCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None));

        Assert.Empty(_added);
        await _unitOfWorkMock.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _notificationsMock.DidNotReceive().CreateAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<NotificationType>(),
            Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownOrInactiveAssignedUser_ThrowsNotFound()
    {
        var donorId = Guid.NewGuid();
        _donorsMock.ExistsAsync(donorId, Arg.Any<CancellationToken>()).Returns(true);
        _usersMock.ExistsAndActiveAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _handler.Handle(MakeCommand(donorId, Guid.NewGuid()), CancellationToken.None));

        Assert.Empty(_added);
        await _unitOfWorkMock.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
