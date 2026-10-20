using CRM.Application.Modules.Vetting.Submissions.Commands.CreateSubmission;
using CRM.Application.Modules.Vetting.Submissions.Commands.SubmitSubmission;
using CRM.Application.Modules.Vetting.Submissions.Commands.UpdateSubmission;
using CRM.Application.Modules.Vetting.Submissions.Dtos;
using CRM.Application.Modules.Vetting.Submissions.Queries.GetSubmissionById;
using CRM.Application.Modules.Vetting.Submissions.Queries.GetSubmissions;
using CRM.Application.Modules.Vetting.Uploads.Commands.UploadAssessmentFile;
using CRM.Application.Modules.Vetting.Uploads.Commands.UploadEvidencePhoto;
using CRM.Application.Modules.Vetting.Uploads.Queries.GetUploadDownloadUrl;
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

    // range(0,3): any other shot index is a 404 at routing.
    [HttpPost("{id:guid}/photos/{shotIndex:int:range(0,3)}")]
    [RequestSizeLimit(5 * 1024 * 1024 + 1024)] // same as POST /donors/{id}/documents: 5MB + multipart overhead
    public async Task<IActionResult> UploadPhoto(Guid id, int shotIndex, IFormFile file)
    {
        await using var stream = file.OpenReadStream();
        var result = await _mediator.Send(new UploadEvidencePhotoCommand
        {
            SubmissionId = id,
            ShotIndex = shotIndex,
            FileStream = stream,
            OriginalFileName = file.FileName,
            ContentType = file.ContentType,
            FileSizeBytes = file.Length
        });
        return Ok(new { data = result });
    }

    [HttpGet("{id:guid}/photos/{shotIndex:int:range(0,3)}/download")]
    public async Task<IActionResult> DownloadPhoto(Guid id, int shotIndex)
        => Ok(new { data = await _mediator.Send(new GetUploadDownloadUrlQuery { SubmissionId = id, ShotIndex = shotIndex }) });

    [HttpPost("{id:guid}/files/{slot}")]
    [RequestSizeLimit(5 * 1024 * 1024 + 1024)]
    public async Task<IActionResult> UploadFile(Guid id, string slot, IFormFile file)
    {
        await using var stream = file.OpenReadStream();
        var result = await _mediator.Send(new UploadAssessmentFileCommand
        {
            SubmissionId = id,
            Slot = slot,
            FileStream = stream,
            OriginalFileName = file.FileName,
            ContentType = file.ContentType,
            FileSizeBytes = file.Length
        });
        return Ok(new { data = result });
    }

    [HttpGet("{id:guid}/files/{slot}/download")]
    public async Task<IActionResult> DownloadFile(Guid id, string slot)
        => Ok(new { data = await _mediator.Send(new GetUploadDownloadUrlQuery { SubmissionId = id, Slot = slot }) });
}
