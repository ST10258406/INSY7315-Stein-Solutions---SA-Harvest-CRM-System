namespace CRM.Infrastructure.Jobs;

using CRM.Application.Common.Interfaces;
using CRM.Domain.Enums;
using CRM.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

/// <summary>
/// Hangfire implementation of <see cref="ITaskDueNotificationJob"/>. First recurring job
/// in the project — the pattern for future ones.
/// </summary>
/// <remarks>
/// <b>Idempotency:</b> a task is notified once, ever. Each run skips any task that already
/// has a <see cref="NotificationType.TaskDue"/> notification pointing at it, so repeated
/// runs (and Hangfire retries) never spam an assignee. Exceptions are deliberately not
/// caught — a failure surfaces in the Hangfire dashboard and is retried.
/// </remarks>
[AutomaticRetry(Attempts = 3)]
[DisableConcurrentExecution(timeoutInSeconds: 300)]
public class TaskDueNotificationJob : ITaskDueNotificationJob
{
    private readonly CrmDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly ILogger<TaskDueNotificationJob> _logger;

    public TaskDueNotificationJob(
        CrmDbContext context,
        INotificationService notificationService,
        ILogger<TaskDueNotificationJob> logger)
    {
        _context = context;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        // "due_date <= today" — due dates are stored at midnight UTC, so anything
        // strictly before tomorrow is due today or earlier.
        var tomorrow = DateTime.UtcNow.Date.AddDays(1);

        var dueTasks = await _context.DonorTasks
            .AsNoTracking()
            .Where(t => !t.IsCompleted && t.DueDate < tomorrow)
            .Select(t => new { t.Id, t.AssignedToUserId, t.Title })
            .ToListAsync(cancellationToken);

        if (dueTasks.Count == 0)
        {
            _logger.LogInformation("TaskDue job: no open tasks are due.");
            return;
        }

        var dueIds = dueTasks.Select(t => t.Id).ToList();

        var alreadyNotified = await _context.Notifications
            .AsNoTracking()
            .Where(n => n.NotificationType == NotificationType.TaskDue
                        && n.RelatedEntityId != null
                        && dueIds.Contains(n.RelatedEntityId.Value))
            .Select(n => n.RelatedEntityId!.Value)
            .ToListAsync(cancellationToken);

        var alreadyNotifiedSet = alreadyNotified.ToHashSet();
        var toNotify = dueTasks.Where(t => !alreadyNotifiedSet.Contains(t.Id)).ToList();

        foreach (var task in toNotify)
        {
            await _notificationService.CreateAsync(
                task.AssignedToUserId,
                "Task due",
                $"A task assigned to you is now due: {task.Title}",
                NotificationType.TaskDue,
                task.Id,
                nameof(Domain.Entities.DonorTask),
                cancellationToken);
        }

        _logger.LogInformation(
            "TaskDue job: {Created} notification(s) created, {Skipped} task(s) already notified.",
            toNotify.Count, dueTasks.Count - toNotify.Count);
    }
}
