using CRM.Application.Modules.Vetting.Submissions.Commands.CreateSubmission;
using CRM.Application.Modules.Vetting.Submissions.Commands.SubmitSubmission;
using CRM.Application.Modules.Vetting.Submissions.Commands.UpdateSubmission;
using CRM.Application.Modules.Vetting.Submissions.Dtos;
using CRM.Application.Modules.Vetting.Submissions.Queries.GetSubmissionById;
using CRM.Application.Modules.Vetting.Submissions.Queries.GetSubmissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/v1/vetting/submissions")]
[Authorize(Policy = "ProcurementOrAbove")]
public class VettingSubmissionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public VettingSubmissionsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetSubmissions([FromQuery] GetSubmissionsQuery query)
        => Ok(await _mediator.Send(query));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetSubmission(Guid id)
        => Ok(new { data = await _mediator.Send(new GetSubmissionByIdQuery(id)) });

    [HttpPost]
    public async Task<IActionResult> CreateSubmission([FromBody] CreateSubmissionRequest request)
    {
        var result = await _mediator.Send(new CreateSubmissionCommand
        {
            DonorId = request.DonorId,
            OfficerId = request.OfficerId,
            SiteVisitId = request.SiteVisitId,
            Details = request.Details
        });
        return CreatedAtAction(nameof(GetSubmission), new { id = result.Id }, new { data = result });
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> UpdateSubmission(Guid id, [FromBody] UpdateSubmissionRequest request)
    {
        var result = await _mediator.Send(new UpdateSubmissionCommand
        {
            Id = id,
            OfficerId = request.OfficerId,
            Details = request.Details
        });
        return Ok(new { data = result });
    }

    [HttpPost("{id:guid}/submit")]
    public async Task<IActionResult> Submit(Guid id)
        => Ok(new { data = await _mediator.Send(new SubmitSubmissionCommand { Id = id }) });
}
