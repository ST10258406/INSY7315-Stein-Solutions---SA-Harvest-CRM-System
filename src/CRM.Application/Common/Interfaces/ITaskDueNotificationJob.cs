namespace CRM.Application.Common.Interfaces;

/// <summary>
/// Daily recurring job: notifies assignees when a task's due date has been reached.
/// The Hangfire wiring (schedule, retry policy, storage) lives in CRM.Infrastructure —
/// this interface keeps the Application layer free of any scheduler dependency.
/// </summary>
public interface ITaskDueNotificationJob
{
    /// <summary>
    /// Finds every open task whose due date is today or earlier and creates one
    /// <c>TaskDue</c> notification per task that has not already had one. Idempotent:
    /// safe to run repeatedly — a task is notified at most once, ever.
    /// </summary>
    Task RunAsync(CancellationToken cancellationToken = default);
}
