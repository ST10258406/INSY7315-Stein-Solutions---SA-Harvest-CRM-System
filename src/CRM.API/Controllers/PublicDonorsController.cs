namespace CRM.API.Controllers;

using CRM.API.Extensions;
using CRM.Application.Modules.PublicDonors.Commands.SubmitPublicDonor;
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
}
