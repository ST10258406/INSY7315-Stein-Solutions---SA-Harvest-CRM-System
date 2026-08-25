namespace CRM.API.Authorization;

using CRM.Domain.Enums;
using Microsoft.AspNetCore.Authorization;

/// <summary>
/// The one place "which document types need an elevated role" is decided.
/// BBBEE certificates require Admin/SuperAdmin; everything else only needs
/// to clear the endpoint's base [Authorize(Policy = "ProcurementOrAbove")] gate.
/// </summary>
public class DocumentTypeAuthorizationHandler
    : AuthorizationHandler<RestrictedDocumentTypeRequirement, DocumentType>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RestrictedDocumentTypeRequirement requirement,
        DocumentType resource)
    {
        var needsElevatedRole = resource == DocumentType.BBBEECertificate;

        if (!needsElevatedRole || context.User.IsInRole("Admin") || context.User.IsInRole("SuperAdmin"))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
