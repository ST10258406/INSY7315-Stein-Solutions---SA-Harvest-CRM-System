using CRM.Application.Modules.Vetting.SiteVisits.Commands.CreateSiteVisit;
using CRM.Application.Modules.Vetting.SiteVisits.Commands.UpdateSiteVisit;
using CRM.Application.Modules.Vetting.SiteVisits.Dtos;
using CRM.Application.Modules.Vetting.SiteVisits.Queries.GetSiteVisits;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/v1/vetting/site-visits")]
[Authorize(Policy = "ProcurementOrAbove")]
public class VettingSiteVisitsController : ControllerBase
{
    private readonly IMediator _mediator;

    public VettingSiteVisitsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetSiteVisits([FromQuery] GetSiteVisitsQuery query)
        => Ok(new { data = await _mediator.Send(query) });

    [HttpPost]
    public async Task<IActionResult> CreateSiteVisit([FromBody] CreateSiteVisitRequest request)
    {
        var result = await _mediator.Send(new CreateSiteVisitCommand
        {
            DonorId = request.DonorId,
            OfficerId = request.OfficerId,
            ScheduledDate = request.ScheduledDate,
            ScheduledTime = request.ScheduledTime,
            LocationLabel = request.LocationLabel,
            VisitNotes = request.VisitNotes
        });
        return StatusCode(StatusCodes.Status201Created, new { data = result });
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> UpdateSiteVisit(Guid id, [FromBody] UpdateSiteVisitRequest request)
    {
        var result = await _mediator.Send(new UpdateSiteVisitCommand
        {
            Id = id,
            ScheduledDate = request.ScheduledDate,
            ScheduledTime = request.ScheduledTime,
            LocationLabel = request.LocationLabel,
            VisitNotes = request.VisitNotes,
            VisitStatus = request.VisitStatus,
            OfficerId = request.OfficerId
        });
        return Ok(new { data = result });
    }
}
