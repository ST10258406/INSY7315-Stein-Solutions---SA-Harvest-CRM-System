using CRM.Domain.Entities;
using CRM.Domain.Enums;
using CRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CRM.Infrastructure.Tests.Persistence;

public class NotificationConfigurationTests
{
    private readonly DbContextOptions<CrmDbContext> _options;

    public NotificationConfigurationTests()
    {
        _options = new DbContextOptionsBuilder<CrmDbContext>()
            .UseNpgsql(TestPostgres.ConnectionString("crm_test_notification"))
            .Options;
    }

    private static User NewUser() => new()
    {
        Id = Guid.NewGuid(),
        Email = $"notif-{Guid.NewGuid()}@example.com",
        FirstName = "Test",
        LastName = "User",
        PasswordHash = "hash"
    };

    [Fact]
    public async Task Insert_ValidNotification_Succeeds()
    {
        using var context = new CrmDbContext(_options);
        await context.Database.EnsureCreatedAsync();

        var user = NewUser();
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Title = "Task assigned",
            Message = "You have a new task.",
            NotificationType = NotificationType.TaskAssigned,
            RelatedEntityId = Guid.NewGuid(),
            RelatedEntityType = "Task",
            CreatedAt = DateTime.UtcNow
        };
        context.Notifications.Add(notification);
        await context.SaveChangesAsync();

        var stored = await context.Notifications.AsNoTracking().SingleAsync(n => n.Id == notification.Id);
        Assert.False(stored.IsRead);
        Assert.Null(stored.ReadAt);
        Assert.Equal(NotificationType.TaskAssigned, stored.NotificationType);
    }

    [Fact]
    public async Task Insert_UnknownNotificationType_ViolatesCheckConstraint()
    {
        using var context = new CrmDbContext(_options);
        await context.Database.EnsureCreatedAsync();

        var user = NewUser();
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // The enum param makes an invalid type impossible from application code,
        // so go straight to raw SQL to prove the DB-level CHECK constraint holds.
        var ex = await Assert.ThrowsAsync<PostgresException>(async () =>
        {
            await context.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO notifications
                    (id, user_id, title, message, is_read, notification_type, created_at)
                VALUES
                    ({0}, {1}, 'bad', 'bad', false, 'NotARealType', now())
                """,
                Guid.NewGuid(), user.Id);
        });

        Assert.Equal("23514", ex.SqlState); // check_violation
        Assert.Equal("chk_notifications_type", ex.ConstraintName);
    }

    [Fact]
    public void Notification_HasNoUpdatedAtColumn()
    {
        using var context = new CrmDbContext(_options);

        var entityType = context.Model.FindEntityType(typeof(Notification))!;
        Assert.NotNull(entityType.FindProperty(nameof(Notification.CreatedAt)));
        Assert.DoesNotContain(entityType.GetProperties(), p => p.Name == "UpdatedAt");
        Assert.DoesNotContain(entityType.GetProperties(), p => p.GetColumnName() == "updated_at");
    }
}
