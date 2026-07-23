namespace CRM.Domain.Common;

public abstract class LookupBaseEntity
{
    public short Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public short SortOrder { get; set; }
}
