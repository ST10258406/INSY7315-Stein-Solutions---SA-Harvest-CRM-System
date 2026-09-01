using AutoMapper;
using CRM.Application.Modules.Notifications.Mappings;
using CRM.Domain.Entities;
using CRM.Domain.Enums;
using CRM.Infrastructure.Persistence;
using CRM.Infrastructure.Persistence.Repositories;
using CRM.Infrastructure.Tests.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace CRM.Infrastructure.Tests.Persistence.Repositories;

public class NotificationRepositoryTests
{
    private readonly DbContextOptions<CrmDbContext> _options;
    private readonly IMapper _mapper;

    private Guid _userId;
    private Guid _otherUserId;

    public NotificationRepositoryTests()
    {
        _options = new DbContextOptionsBuilder<CrmDbContext>()
            .UseNpgsql(TestPostgres.ConnectionString("crm_test_notificationrepo"))
            .Options;

        var config = new MapperConfiguration(cfg => cfg.AddProfile<NotificationMappingProfile>(), NullLoggerFactory.Instance);
        _mapper = config.CreateMapper();
    }

    private async Task<CrmDbContext> CreateSeededContextAsync()
    {
        var context = new CrmDbContext(_options);
        await context.Database.EnsureCreatedAsync();

        context.Notifications.RemoveRange(context.Notifications);
        await context.SaveChangesAsync();

        _userId = (await GetOrAddUser(context, "notif-owner@example.com")).Id;
        _otherUserId = (await GetOrAddUser(context, "notif-other@example.com")).Id;
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();
        return context;
    }

    private static async Task<User> GetOrAddUser(CrmDbContext context, string email)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user is null)
        {
            user = new User { Id = Guid.NewGuid(), Email = email, FirstName = "N", LastName = "U", PasswordHash = "hash", IsActive = true };
            context.Users.Add(user);
        }
        return user;
    }

    private Notification MakeNotification(Guid userId, bool isRead, DateTime createdAt) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        Title = "t",
        Message = "m",
        NotificationType = NotificationType.TaskAssigned,
        IsRead = isRead,
        ReadAt = isRead ? createdAt.AddMinutes(1) : null,
        CreatedAt = createdAt
    };

    [Fact]
    public async Task GetForUserAsync_ScopedToUser_NewestFirst_WithReadFilter()
    {
        using var context = await CreateSeededContextAsync();
        var now = DateTime.UtcNow;
        context.Notifications.AddRange(
            MakeNotification(_userId, isRead: false, now.AddMinutes(-3)),
            MakeNotification(_userId, isRead: true, now.AddMinutes(-2)),
            MakeNotification(_userId, isRead: false, now.AddMinutes(-1)),
            MakeNotification(_otherUserId, isRead: false, now));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repo = new NotificationRepository(context, _mapper);

        var (all, allTotal) = await repo.GetForUserAsync(_userId, null, 1, 20);
        Assert.Equal(3, allTotal);
        Assert.True(all[0].CreatedAt > all[1].CreatedAt && all[1].CreatedAt > all[2].CreatedAt);

        var (unread, unreadTotal) = await repo.GetForUserAsync(_userId, isRead: false, 1, 20);
        Assert.Equal(2, unreadTotal);
        Assert.All(unread, n => Assert.False(n.IsRead));
    }

    [Fact]
    public async Task GetUnreadCountAsync_CountsOnlyThatUsersUnread()
    {
        using var context = await CreateSeededContextAsync();
        var now = DateTime.UtcNow;
        context.Notifications.AddRange(
            MakeNotification(_userId, isRead: false, now),
            MakeNotification(_userId, isRead: false, now),
            MakeNotification(_userId, isRead: true, now),
            MakeNotification(_otherUserId, isRead: false, now));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repo = new NotificationRepository(context, _mapper);

        Assert.Equal(2, await repo.GetUnreadCountAsync(_userId));
    }

    [Fact]
    public async Task MarkAllReadAsync_FlipsOnlyThatUsersUnread_SetsReadAt_ReturnsCount()
    {
        using var context = await CreateSeededContextAsync();
        var now = DateTime.UtcNow;
        context.Notifications.AddRange(
            MakeNotification(_userId, isRead: false, now),
            MakeNotification(_userId, isRead: false, now),
            MakeNotification(_userId, isRead: true, now),
            MakeNotification(_otherUserId, isRead: false, now));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repo = new NotificationRepository(context, _mapper);
        var readAt = DateTime.UtcNow;
        var affected = await repo.MarkAllReadAsync(_userId, readAt);

        Assert.Equal(2, affected);

        using var verify = new CrmDbContext(_options);
        Assert.Equal(0, await verify.Notifications.AsNoTracking().CountAsync(n => n.UserId == _userId && !n.IsRead));
        Assert.True(await verify.Notifications.AsNoTracking().AnyAsync(n => n.UserId == _otherUserId && !n.IsRead));
        Assert.All(
            await verify.Notifications.AsNoTracking().Where(n => n.UserId == _userId).ToListAsync(),
            n => Assert.NotNull(n.ReadAt));
    }

    [Fact]
    public async Task GetForUpdateAsync_ReturnsTracked_OrNull()
    {
        using var context = await CreateSeededContextAsync();
        var n = MakeNotification(_userId, isRead: false, DateTime.UtcNow);
        context.Notifications.Add(n);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repo = new NotificationRepository(context, _mapper);

        var tracked = await repo.GetForUpdateAsync(n.Id);
        Assert.NotNull(tracked);
        tracked!.IsRead = true;
        await context.SaveChangesAsync();

        using var verify = new CrmDbContext(_options);
        Assert.True((await verify.Notifications.AsNoTracking().SingleAsync(x => x.Id == n.Id)).IsRead);

        Assert.Null(await repo.GetForUpdateAsync(Guid.NewGuid()));
    }
}
