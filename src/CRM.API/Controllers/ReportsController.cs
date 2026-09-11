using CRM.Application.Modules.Reports.Queries.GetDonorsContactedReport;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/v1/reports")]
[Authorize(Policy = "AdminOrAbove")] // reporting is Admin-only
public class ReportsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ReportsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("donors-contacted")]
    public async Task<IActionResult> GetDonorsContacted([FromQuery] GetDonorsContactedReportQuery query)
    {
        var result = await _mediator.Send(query);
        return Ok(new { data = result });
    }
}
