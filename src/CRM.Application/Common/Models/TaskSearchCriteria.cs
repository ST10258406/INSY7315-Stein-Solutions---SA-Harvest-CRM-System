namespace CRM.Application.Common.Models;

/// <summary>
/// Parameter object for <see cref="Common.Interfaces.ITaskRepository.GetTasksAsync"/>.
/// Carries only already-parsed, already-validated values — the repository never
/// interprets raw request strings.
/// </summary>
public record TaskSearchCriteria
{
    /// <summary>Restrict to tasks assigned to this user ("My Tasks"). Null = any assignee.</summary>
    public Guid? AssignedToUserId { get; init; }

    /// <summary>Restrict to a single donor. Null = any donor.</summary>
    public Guid? DonorId { get; init; }

    /// <summary>true / false filter on completion; null = both.</summary>
    public bool? IsCompleted { get; init; }

    /// <summary>Only tasks due on or before this instant.</summary>
    public DateTime? DueBefore { get; init; }

    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
