using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Notifications.Commands.MarkAllNotificationsRead;
using CRM.Application.Modules.Notifications.Commands.MarkNotificationRead;
using CRM.Domain.Entities;
using CRM.Domain.Enums;
using NSubstitute;
using ForbiddenException = CRM.Application.Common.Exceptions.ForbiddenException;
using NotFoundException = CRM.Application.Common.Exceptions.NotFoundException;

namespace CRM.Application.Tests.Modules.Notifications.Commands;

public class MarkNotificationReadCommandHandlerTests
{
    private readonly INotificationRepository _repoMock = Substitute.For<INotificationRepository>();
    private readonly IUnitOfWork _unitOfWorkMock = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserService _currentUserMock = Substitute.For<ICurrentUserService>();
    private readonly MarkNotificationReadCommandHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();

    public MarkNotificationReadCommandHandlerTests()
    {
        _currentUserMock.GetCurrentUserId().Returns(_userId);
        _handler = new MarkNotificationReadCommandHandler(_repoMock, _unitOfWorkMock, _currentUserMock);
    }

    private Notification Seed(Guid ownerId, bool isRead = false)
    {
        var n = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = ownerId,
            Title = "t",
            Message = "m",
            NotificationType = NotificationType.TaskAssigned,
            IsRead = isRead,
            ReadAt = isRead ? DateTime.UtcNow.AddDays(-1) : null
        };
        _repoMock.GetForUpdateAsync(n.Id, Arg.Any<CancellationToken>()).Returns(n);
        return n;
    }

    [Fact]
    public async Task Handle_OwnUnreadNotification_MarksReadWithTimestamp_AndSaves()
    {
        var n = Seed(_userId);
        var before = DateTime.UtcNow;

        await _handler.Handle(new MarkNotificationReadCommand { Id = n.Id }, CancellationToken.None);

        Assert.True(n.IsRead);
        Assert.NotNull(n.ReadAt);
        Assert.InRange(n.ReadAt!.Value, before, DateTime.UtcNow.AddSeconds(1));
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AlreadyRead_IsIdempotent_NoSave_KeepsOriginalReadAt()
    {
        var n = Seed(_userId, isRead: true);
        var originalReadAt = n.ReadAt;

        await _handler.Handle(new MarkNotificationReadCommand { Id = n.Id }, CancellationToken.None);

        Assert.Equal(originalReadAt, n.ReadAt);
        await _unitOfWorkMock.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AnotherUsersNotification_ThrowsForbidden_NoSave()
    {
        var n = Seed(ownerId: Guid.NewGuid());

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _handler.Handle(new MarkNotificationReadCommand { Id = n.Id }, CancellationToken.None));

        await _unitOfWorkMock.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MissingNotification_ThrowsNotFound()
    {
        _repoMock.GetForUpdateAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Notification?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(new MarkNotificationReadCommand { Id = Guid.NewGuid() }, CancellationToken.None));
    }
}

public class MarkAllNotificationsReadCommandHandlerTests
{
    [Fact]
    public async Task Handle_DelegatesToRepositoryWithTheCurrentUser()
    {
        var repo = Substitute.For<INotificationRepository>();
        var currentUser = Substitute.For<ICurrentUserService>();
        var userId = Guid.NewGuid();
        currentUser.GetCurrentUserId().Returns(userId);
        repo.MarkAllReadAsync(Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(3);

        var handler = new MarkAllNotificationsReadCommandHandler(repo, currentUser);
        await handler.Handle(new MarkAllNotificationsReadCommand(), CancellationToken.None);

        await repo.Received(1).MarkAllReadAsync(userId, Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }
}
