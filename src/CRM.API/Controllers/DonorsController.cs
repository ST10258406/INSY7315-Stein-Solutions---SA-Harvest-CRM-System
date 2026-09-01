using CRM.API.Authorization;
using CRM.Application.Common.Models;
using CRM.Application.Modules.Donors.Commands.CreateDonor;
using CRM.Application.Modules.Donors.Commands.DeleteDonorDocument;
using CRM.Application.Modules.Donors.Commands.UpdateDonor;
using CRM.Application.Modules.Donors.Commands.UploadDonorDocument;
using CRM.Application.Modules.Donors.Dtos;
using CRM.Application.Modules.Donors.Queries.GetDocumentDownloadUrl;
using CRM.Application.Modules.Donors.Queries.GetDonorById;
using CRM.Application.Modules.Donors.Queries.GetDonors;
using CRM.Application.Modules.Interactions.Commands.LogInteraction;
using CRM.Application.Modules.Interactions.Dtos;
using CRM.Application.Modules.Interactions.Queries.GetDonorInteractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/v1/donors")]
public class DonorsController : ControllerBase
{
    private readonly IMediator _mediator;

    public DonorsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [Authorize(Policy = "ProcurementOrAbove")]
    public async Task<ActionResult<PaginatedResult<DonorListItemDto>>> GetDonors([FromQuery] GetDonorsQuery query)
    {
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "ProcurementOrAbove")]
    public async Task<IActionResult> GetDonorById(Guid id)
    {
        var result = await _mediator.Send(new GetDonorByIdQuery(id));
        return Ok(new { data = result });
    }

    [HttpPost]
    [Authorize(Policy = "ProcurementOrAbove")]
    public async Task<IActionResult> CreateDonor([FromBody] CreateDonorRequest request)
    {
        var result = await _mediator.Send(new CreateDonorCommand { Request = request });
        return CreatedAtAction(nameof(GetDonorById), new { id = result.Id }, new { data = result });
    }

    [HttpPatch("{id:guid}")]
    [Authorize(Policy = "ProcurementOrAbove")]
    public async Task<IActionResult> UpdateDonor(Guid id, [FromBody] UpdateDonorRequest request)
    {
        var result = await _mediator.Send(new UpdateDonorCommand { Id = id, Request = request });
        return Ok(new { data = result });
    }

    [HttpGet("{id:guid}/interactions")]
    [Authorize(Policy = "ProcurementOrAbove")]
    public async Task<IActionResult> GetInteractions(Guid id, [FromQuery] GetDonorInteractionsQuery query)
    {
        var result = await _mediator.Send(query with { DonorId = id });
        return Ok(result);
    }

    [HttpPost("{id:guid}/interactions")]
    [Authorize(Policy = "ProcurementOrAbove")]
    public async Task<IActionResult> LogInteraction(Guid id, [FromBody] LogInteractionRequest request)
    {
        var result = await _mediator.Send(new LogInteractionCommand
        {
            DonorId = id,
            InteractionType = request.InteractionType,
            Subject = request.Subject,
            Body = request.Body,
            FollowUpDate = request.FollowUpDate
        });
        return StatusCode(201, new { data = result });
    }

    [HttpPost("{id:guid}/documents")]
    [Authorize(Policy = "AdminOrAbove")]
    [RequestSizeLimit(5 * 1024 * 1024 + 1024)] // 5MB + a small buffer for multipart form overhead
    public async Task<IActionResult> UploadDocument(Guid id, [FromForm] string documentType, IFormFile file)
    {
        await using var stream = file.OpenReadStream();
        var result = await _mediator.Send(new UploadDonorDocumentCommand
        {
            DonorId = id,
            DocumentType = documentType,
            FileStream = stream,
            OriginalFileName = Path.GetFileName(file.FileName),
            ContentType = file.ContentType,
            FileSizeBytes = file.Length
        });
        return StatusCode(201, new { data = result });
    }

    [HttpGet("{id:guid}/documents/{docId:guid}/download")]
    [Authorize(Policy = "ProcurementOrAbove")]
    [TypeFilter(typeof(DocumentTypeAuthorizationFilter))]
    public async Task<IActionResult> GetDocumentDownloadUrl(Guid id, Guid docId)
    {
        var result = await _mediator.Send(new GetDocumentDownloadUrlQuery { DonorId = id, DocumentId = docId });
        return Ok(new { data = result });
    }

    [HttpDelete("{id:guid}/documents/{docId:guid}")]
    [Authorize(Policy = "AdminOrAbove")]
    public async Task<IActionResult> DeleteDocument(Guid id, Guid docId)
    {
        await _mediator.Send(new DeleteDonorDocumentCommand { DonorId = id, DocumentId = docId });
        return NoContent();
    }
}
