namespace CRM.Application.Modules.Tasks.Dtos;

/// <summary>Request body for POST /api/v1/donors/{id}/tasks.</summary>
public class CreateTaskRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid AssignedToUserId { get; set; }
    public DateOnly DueDate { get; set; }
}
