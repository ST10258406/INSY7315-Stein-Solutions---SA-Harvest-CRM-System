namespace CRM.Domain.Entities;

using CRM.Domain.Common;

public class DonorTask : BaseEntity
{
    public Guid DonorId { get; set; }
    public Donor Donor { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    public DateTime DueDate { get; set; }
    public bool IsCompleted { get; set; }

    public Guid AssignedToUserId { get; set; }
    public User AssignedToUser { get; set; } = null!;

    public Guid CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;

    public Guid? CompletedByUserId { get; set; }
    public User? CompletedByUser { get; set; }
}
