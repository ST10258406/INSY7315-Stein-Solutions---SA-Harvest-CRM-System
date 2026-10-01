namespace CRM.API.Controllers;

using CRM.API.Extensions;
using CRM.Application.Modules.PublicDonors.Commands.SubmitPublicDonor;
using CRM.Application.Modules.PublicDonors.Commands.SubmitPublicDonorDocument;
using CRM.Application.Modules.PublicDonors.Dtos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

/// <summary>
/// Unauthenticated endpoints for the public donor onboarding form. Every action
/// here must be [AllowAnonymous] and carry a rate limit policy from
/// RateLimitingExtensions — see that class's remarks on why (design doc RULE 2).
/// </summary>
[ApiController]
[Route("api/v1/public/donors")]
[AllowAnonymous]
public class PublicDonorsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PublicDonorsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("submit")]
    [EnableRateLimiting(RateLimitingExtensions.PublicSubmitPolicy)]
    public async Task<ActionResult<SubmitPublicDonorResponseDto>> Submit([FromBody] SubmitPublicDonorRequest request)
    {
        var command = new SubmitPublicDonorCommand
        {
            Request = request,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers.UserAgent.ToString()
        };

        var result = await _mediator.Send(command);

        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>
    /// Follow-up to Submit above — uploads the BBBEE certificate for a pending
    /// public-form donor, linked via the opaque SessionToken from that response
    /// rather than the donor's real Id, which this client never sees.
    /// </summary>
    [HttpPost("submit/document")]
    [EnableRateLimiting(RateLimitingExtensions.PublicSubmitPolicy)]
    [RequestSizeLimit(5 * 1024 * 1024 + 1024)] // 5MB + a small buffer for multipart form overhead
    public async Task<ActionResult<SubmitPublicDonorDocumentResponseDto>> SubmitDocument(
        [FromForm] string sessionToken, [FromForm] string documentType, IFormFile file)
    {
        await using var stream = file.OpenReadStream();
        var command = new SubmitPublicDonorDocumentCommand
        {
            SessionToken = sessionToken,
            DocumentType = documentType,
            FileStream = stream,
            OriginalFileName = file.FileName,
            ContentType = file.ContentType,
            FileSizeBytes = file.Length,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers.UserAgent.ToString()
        };

        var result = await _mediator.Send(command);

        return StatusCode(StatusCodes.Status201Created, result);
    }
}
