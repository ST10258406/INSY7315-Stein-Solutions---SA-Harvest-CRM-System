namespace CRM.Infrastructure.Tests.Services;

using CRM.Application.Common.Interfaces;
using CRM.Domain.Entities;
using CRM.Domain.Enums;
using CRM.Infrastructure.Persistence;
using CRM.Infrastructure.Services;
using CRM.Infrastructure.Tests.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

public class NotificationServiceTests
{
    private readonly DbContextOptions<CrmDbContext> _options;

    public NotificationServiceTests()
    {
        _options = new DbContextOptionsBuilder<CrmDbContext>()
            .UseNpgsql(TestPostgres.ConnectionString("crm_test_notificationservice"))
            .Options;
    }

    private static User NewUser() => new()
    {
        Id = Guid.NewGuid(),
        Email = $"notifsvc-{Guid.NewGuid()}@example.com",
        FirstName = "Test",
        LastName = "User",
        PasswordHash = "hash"
    };

    [Fact]
    public async Task CreateAsync_ValidInput_PersistsNotification()
    {
        using var arrange = new CrmDbContext(_options);
        await arrange.Database.EnsureCreatedAsync();
        var user = NewUser();
        arrange.Users.Add(user);
        await arrange.SaveChangesAsync();

        var relatedId = Guid.NewGuid();
        using (var actContext = new CrmDbContext(_options))
        {
            var sut = new NotificationService(actContext);
            await sut.CreateAsync(
                user.Id,
                "New task",
                "You have been assigned a task.",
                NotificationType.TaskAssigned,
                relatedId,
                "Task");
        }

        // Fire-and-forget: the row is there with no follow-up SaveChanges from the caller.
        using var verify = new CrmDbContext(_options);
        var stored = await verify.Notifications.AsNoTracking().SingleAsync(n => n.UserId == user.Id);
        Assert.Equal("New task", stored.Title);
        Assert.Equal("You have been assigned a task.", stored.Message);
        Assert.Equal(NotificationType.TaskAssigned, stored.NotificationType);
        Assert.Equal(relatedId, stored.RelatedEntityId);
        Assert.Equal("Task", stored.RelatedEntityType);
        Assert.False(stored.IsRead);
        Assert.Null(stored.ReadAt);
        Assert.NotEqual(default, stored.CreatedAt);
    }

    [Fact]
    public async Task CreateAsync_NullOptionalArgs_PersistsWithNullRelatedEntity()
    {
        using var arrange = new CrmDbContext(_options);
        await arrange.Database.EnsureCreatedAsync();
        var user = NewUser();
        arrange.Users.Add(user);
        await arrange.SaveChangesAsync();

        using (var actContext = new CrmDbContext(_options))
        {
            var sut = new NotificationService(actContext);
            await sut.CreateAsync(user.Id, "Reminder", "Follow up today", NotificationType.FollowUpReminder);
        }

        using var verify = new CrmDbContext(_options);
        var stored = await verify.Notifications.AsNoTracking().SingleAsync(n => n.UserId == user.Id);
        Assert.Null(stored.RelatedEntityId);
        Assert.Null(stored.RelatedEntityType);
        Assert.Equal(NotificationType.FollowUpReminder, stored.NotificationType);
    }

    [Fact]
    public void CreateAsync_TypeParameterIsEnum_SoUnknownTypesAreImpossibleByConstruction()
    {
        var typeParam = typeof(INotificationService)
            .GetMethod(nameof(INotificationService.CreateAsync))!
            .GetParameters()
            .Single(p => p.Name == "type");

        Assert.Equal(typeof(NotificationType), typeParam.ParameterType);
        Assert.True(typeParam.ParameterType.IsEnum);
    }
}
