namespace CRM.API.Authorization;

using Microsoft.AspNetCore.Authorization;

/// <summary>
/// Requirement checked against the <em>target</em> user of a Users-admin write
/// (update details, change status, change role) — "may this caller act on this
/// particular user?". See <see cref="UserTargetAuthorizationHandler"/> and
/// <see cref="UserTargetAuthorizationFilter"/>. The endpoint's base
/// [Authorize(Policy = "AdminOrAbove")] is a separate, static gate; this is the
/// per-resource layer on top of it.
/// </summary>
public class ManageTargetUserRequirement : IAuthorizationRequirement
{
}
