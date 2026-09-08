namespace CRM.Application.Modules.Tasks.Dtos;

/// <summary>
/// Request body for PATCH /api/v1/tasks/{id}. Every field is optional — a null field is
/// left unchanged. (Because <c>description</c> is itself nullable, PATCH cannot clear it;
/// that is an accepted limitation of the partial-update convention used across this API.)
/// </summary>
public class UpdateTaskRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public DateOnly? DueDate { get; set; }
    public Guid? AssignedToUserId { get; set; }
}
