namespace CRM.API.Authorization;

using CRM.Application.Common.Exceptions;
using CRM.Application.Modules.Donors.Queries.GetDonorDocumentType;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;

/// <summary>
/// Runs the document-type-aware role check *before* the controller action
/// executes, so the action itself stays to the four canonical steps (receive
/// request, map to query, send, return result) with no if/else of its own —
/// role logic belongs in <see cref="DocumentTypeAuthorizationHandler"/>, not
/// in Application handlers and not inline in the controller.
/// Apply via [TypeFilter(typeof(DocumentTypeAuthorizationFilter))] on an
/// action whose route has "id" (donor) and "docId" (document) parameters.
/// </summary>
public class DocumentTypeAuthorizationFilter : IAsyncAuthorizationFilter
{
    private readonly IMediator _mediator;
    private readonly IAuthorizationService _authorizationService;

    public DocumentTypeAuthorizationFilter(IMediator mediator, IAuthorizationService authorizationService)
    {
        _mediator = mediator;
        _authorizationService = authorizationService;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var donorId = Guid.Parse(context.RouteData.Values["id"]!.ToString()!);
        var documentId = Guid.Parse(context.RouteData.Values["docId"]!.ToString()!);

        var documentType = await _mediator.Send(
            new GetDonorDocumentTypeQuery { DonorId = donorId, DocumentId = documentId });

        var result = await _authorizationService.AuthorizeAsync(
            context.HttpContext.User, documentType, new RestrictedDocumentTypeRequirement());

        if (!result.Succeeded)
            throw new ForbiddenException("Insufficient role to download this document type.");
    }
}
