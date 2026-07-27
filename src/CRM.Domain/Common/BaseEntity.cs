namespace CRM.Domain.Common;

public abstract class BaseEntity : IHasUpdatedAt
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
