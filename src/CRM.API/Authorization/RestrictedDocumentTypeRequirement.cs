namespace CRM.API.Authorization;

using Microsoft.AspNetCore.Authorization;

/// <summary>
/// Marker requirement for "does this user's role clear the bar for this
/// specific document type" — evaluated by <see cref="DocumentTypeAuthorizationHandler"/>
/// against a <c>DocumentType</c> resource. The base
/// [Authorize(Policy = "ProcurementOrAbove")] on the endpoint is a separate,
/// static gate; this is the per-resource layer on top of it.
/// </summary>
public class RestrictedDocumentTypeRequirement : IAuthorizationRequirement
{
}
