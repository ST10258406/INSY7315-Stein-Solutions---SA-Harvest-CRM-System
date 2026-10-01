namespace CRM.Application.Modules.Users.Dtos;

using CRM.Domain.Constants;

/// <summary>
/// The user a Users-admin request is acting <em>on</em>, as loaded from the database —
/// the resource for the per-target authorization check (see
/// CRM.API.Authorization.UserTargetAuthorizationHandler). Never built from request data.
/// </summary>
public class UserAuthorizationTarget
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;

    /// <summary>Every role the target holds (all rows become role claims at login, so any one counts).</summary>
    public List<string> RoleNames { get; set; } = [];

    public bool IsSystemUser => SystemUsers.IsSystemUserEmail(Email);

    public bool IsSuperAdmin => RoleNames.Contains("SuperAdmin", StringComparer.Ordinal);
}
