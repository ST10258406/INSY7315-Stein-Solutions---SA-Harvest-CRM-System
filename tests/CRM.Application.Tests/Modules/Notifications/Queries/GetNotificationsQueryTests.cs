using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Notifications.Dtos;
using CRM.Application.Modules.Notifications.Queries.GetNotifications;
using NSubstitute;

namespace CRM.Application.Tests.Modules.Notifications.Queries;

public class GetNotificationsQueryValidatorTests
{
    private readonly GetNotificationsQueryValidator _validator = new();

    [Fact]
    public void Validate_Defaults_Passes() => Assert.True(_validator.Validate(new GetNotificationsQuery()).IsValid);

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_PageBelowOne_Fails(int page)
        => Assert.False(_validator.Validate(new GetNotificationsQuery { Page = page }).IsValid);

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Validate_PageSizeOutOfRange_Fails(int pageSize)
        => Assert.False(_validator.Validate(new GetNotificationsQuery { PageSize = pageSize }).IsValid);
}

public class GetNotificationsQueryHandlerTests
{
    private readonly INotificationRepository _repoMock = Substitute.For<INotificationRepository>();
    private readonly ICurrentUserService _currentUserMock = Substitute.For<ICurrentUserService>();
    private readonly GetNotificationsQueryHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();
    private (Guid userId, bool? isRead, int page, int pageSize)? _captured;

    public GetNotificationsQueryHandlerTests()
    {
        _currentUserMock.GetCurrentUserId().Returns(_userId);

        _repoMock.GetForUserAsync(Arg.Any<Guid>(), Arg.Any<bool?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                _captured = (ci.ArgAt<Guid>(0), ci.ArgAt<bool?>(1), ci.ArgAt<int>(2), ci.ArgAt<int>(3));
                return (new List<NotificationDto>(), 0);
            });
        _repoMock.GetUnreadCountAsync(_userId, Arg.Any<CancellationToken>()).Returns(7);

        _handler = new GetNotificationsQueryHandler(_repoMock, _currentUserMock);
    }

    [Fact]
    public async Task Handle_AlwaysScopesToTheCaller()
    {
        await _handler.Handle(new GetNotificationsQuery(), CancellationToken.None);
        Assert.Equal(_userId, _captured!.Value.userId);
    }

    [Fact]
    public async Task Handle_PassesIsReadAndPageThrough()
    {
        await _handler.Handle(new GetNotificationsQuery { IsRead = false, Page = 3, PageSize = 15 }, CancellationToken.None);

        Assert.False(_captured!.Value.isRead);
        Assert.Equal(3, _captured.Value.page);
        Assert.Equal(15, _captured.Value.pageSize);
    }

    [Fact]
    public async Task Handle_PutsUnreadCountOutsideThePaginationEnvelope()
    {
        var items = new List<NotificationDto> { new() { Id = Guid.NewGuid() } };
        _repoMock.GetForUserAsync(Arg.Any<Guid>(), Arg.Any<bool?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((items, 40));

        var result = await _handler.Handle(new GetNotificationsQuery { Page = 2, PageSize = 10 }, CancellationToken.None);

        Assert.Same(items[0], result.Data.Single());
        Assert.Equal(2, result.Pagination.Page);
        Assert.Equal(40, result.Pagination.TotalCount);
        Assert.Equal(4, result.Pagination.TotalPages);
        Assert.Equal(7, result.UnreadCount);
    }
}
