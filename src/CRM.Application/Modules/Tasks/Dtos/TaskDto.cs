namespace CRM.Application.Modules.Tasks.Dtos;

/// <summary>
/// The single shared task shape returned by both GET /tasks and GET /donors/{id}/tasks.
/// Task creation (#100) and lifecycle (#101) build on this same DTO — don't fork it.
/// </summary>
public class TaskDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime DueDate { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public TaskDonorDto Donor { get; set; } = null!;
    public TaskUserDto AssignedTo { get; set; } = null!;
    public TaskUserDto CreatedBy { get; set; } = null!;
    public TaskUserDto? CompletedBy { get; set; }
}

public class TaskDonorDto
{
    public Guid Id { get; set; }
    public string CompanyName { get; set; } = string.Empty;
}

public class TaskUserDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
}
