using CRM.Application.Modules.Dashboard.Queries.GetDashboardStats;
using CRM.Application.Modules.Dashboard.Queries.GetManagerActivity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/v1/dashboard")]
[Authorize] // any authenticated user — pendingApprovals is scoped down inside the handler
public class DashboardController : ControllerBase
{
    private readonly IMediator _mediator;

    public DashboardController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var result = await _mediator.Send(new GetDashboardStatsQuery());
        return Ok(new { data = result });
    }

    /// <summary>Donors contacted per user over the last 7 ("weekly") or 30 ("monthly") days.</summary>
    [HttpGet("manager-activity")]
    public async Task<IActionResult> GetManagerActivity([FromQuery] string period = "weekly")
    {
        if (!string.Equals(period, "weekly", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(period, "monthly", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "period must be weekly or monthly." });

        var result = await _mediator.Send(new GetManagerActivityQuery(period));
        return Ok(new { data = result });
    }
}
