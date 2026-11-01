using CRM.Application.Modules.Agent.Commands.SendAgentMessage;
using CRM.API.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CRM.API.Controllers;

/// <summary>In-app CRM assistant (docs/agent-spec.md). Read-only tools, SuperAdmin only.</summary>
[ApiController]
[Route("api/v1/agent")]
[Authorize(Policy = "SuperAdminOnly")]
[EnableRateLimiting(RateLimitingExtensions.AgentPolicy)]
public class AgentController : ControllerBase
{
    private readonly IMediator _mediator;

    public AgentController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("messages")]
    public async Task<IActionResult> SendMessage([FromBody] SendAgentMessageCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }
}
