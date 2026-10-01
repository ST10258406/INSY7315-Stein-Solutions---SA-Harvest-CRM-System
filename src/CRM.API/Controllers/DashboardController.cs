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

    /// <summary>Donors contacted per user over the last 7 ("weekly") or 30 ("monthly") days. Period is validated by GetManagerActivityQueryValidator.</summary>
    [HttpGet("manager-activity")]
    public async Task<IActionResult> GetManagerActivity([FromQuery] string period = "weekly")
    {
        var result = await _mediator.Send(new GetManagerActivityQuery(period));
        return Ok(new { data = result });
    }
}
