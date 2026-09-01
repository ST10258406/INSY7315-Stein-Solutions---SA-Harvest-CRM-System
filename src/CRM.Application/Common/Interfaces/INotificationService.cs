namespace CRM.Application.Common.Interfaces;

using CRM.Domain.Enums;

/// <summary>
/// Creates in-app notifications for a user. Deliberately dumb: it writes a single
/// <c>notifications</c> row and nothing more. Whether a given donor/task/approval
/// <i>should</i> produce a notification is a decision for the calling handler, not this service.
/// </summary>
/// <remarks>
/// <para>
/// Mirrors the fire-and-forget shape of <see cref="IEmailService"/>: <see cref="CreateAsync"/>
/// persists the row itself, so callers do not have to remember a follow-up
/// <see cref="IUnitOfWork.SaveChangesAsync"/>. It is safe to call from inside a handler —
/// the established pattern (see <c>CreateDonorCommandHandler</c>) is to fire notifications
/// after the handler's own unit of work has already committed its main work.
/// </para>
/// <para>
/// The signature is fixed — several Sprint 4 features (interactions, task creation,
/// approvals, the daily TaskDue job) call it. An invalid <see cref="NotificationType"/>
/// is impossible by construction: the type is an enum parameter, not a raw string.
/// </para>
/// </remarks>
public interface INotificationService
{
    /// <param name="userId">The user who receives the notification.</param>
    /// <param name="title">Short headline shown in the notification bell.</param>
    /// <param name="message">Body text.</param>
    /// <param name="type">One of the known <see cref="NotificationType"/> values.</param>
    /// <param name="relatedEntityId">Optional donor id or task id the frontend navigates to on click.</param>
    /// <param name="relatedEntityType">Optional loose discriminator for <paramref name="relatedEntityId"/> — e.g. "Donor" or "Task".</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task CreateAsync(
        Guid userId,
        string title,
        string message,
        NotificationType type,
        Guid? relatedEntityId = null,
        string? relatedEntityType = null,
        CancellationToken cancellationToken = default);
}
