namespace CRM.Domain.Entities;

using CRM.Domain.Common;
using CRM.Domain.Enums;

/// <summary>
/// A user's named donor-list filter (Issue 103): the list page's query string, saved per user so it
/// follows them across devices. Soft-deleted via <see cref="IsActive"/>, never hard-deleted.
/// </summary>
public class SavedFilter : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public SavedFilterPage Page { get; set; }

    /// <summary>Validated, allow-listed query string without a leading '?', e.g. "status=Active&amp;regionCode=GP".</summary>
    public string QueryString { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
