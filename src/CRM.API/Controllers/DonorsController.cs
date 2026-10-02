using CRM.API.Authorization;
using CRM.API.Extensions;
using CRM.Application.Common.Models;
using CRM.Application.Modules.Donors.Commands.CreateDonor;
using CRM.Application.Modules.Donors.Commands.DeleteDonorDocument;
using CRM.Application.Modules.Donors.Commands.SendDonorEmail;
using CRM.Application.Modules.Donors.Commands.SendPublicFormInvite;
using CRM.Application.Modules.Donors.Commands.UpdateDonor;
using CRM.Application.Modules.Donors.Commands.UploadDonorDocument;
using CRM.Application.Modules.Donors.Dtos;
using CRM.Application.Modules.Donors.Queries.GetDocumentDownloadUrl;
using CRM.Application.Modules.Donors.Queries.GetDonorById;
using CRM.Application.Modules.Donors.Queries.GetDonors;
using CRM.Application.Modules.Interactions.Commands.LogInteraction;
using CRM.Application.Modules.Interactions.Dtos;
using CRM.Application.Modules.Interactions.Queries.GetDonorInteractions;
using CRM.Application.Modules.Tasks.Commands.CreateTask;
using CRM.Application.Modules.Tasks.Dtos;
using CRM.Application.Modules.Tasks.Queries.GetDonorTasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/v1/donors")]
public class DonorsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IConfiguration _configuration;

    public DonorsController(IMediator mediator, IConfiguration configuration)
    {
        _mediator = mediator;
        _configuration = configuration;
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

    [HttpPost("public-form-invite")]
    [Authorize(Policy = "ProcurementOrAbove")]
    [EnableRateLimiting(RateLimitingExtensions.DonorEmailPolicy)]
    public async Task<IActionResult> SendPublicFormInvite([FromBody] SendPublicFormInviteRequest request)
    {
        var baseUrl = _configuration["Frontend:BaseUrl"]
            ?? throw new InvalidOperationException("Frontend:BaseUrl is not configured.");

        var result = await _mediator.Send(new SendPublicFormInviteCommand
        {
            To = request.To,
            Subject = request.Subject,
            Body = request.Body,
            PublicFormUrl = $"{baseUrl.TrimEnd('/')}/donate"
        });
        return Ok(new { data = result });
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

    [HttpPost("{id:guid}/interactions/email")]
    [Authorize(Policy = "ProcurementOrAbove")]
    [EnableRateLimiting(RateLimitingExtensions.DonorEmailPolicy)]
    public async Task<IActionResult> SendEmail(Guid id, [FromBody] SendDonorEmailRequest request)
    {
        var result = await _mediator.Send(new SendDonorEmailCommand
        {
            DonorId = id,
            To = request.To,
            Subject = request.Subject,
            Body = request.Body
        });
        return StatusCode(201, new { data = result });
    }

    [HttpGet("{id:guid}/tasks")]
    [Authorize(Policy = "ProcurementOrAbove")]
    public async Task<IActionResult> GetDonorTasks(Guid id, [FromQuery] GetDonorTasksQuery query)
    {
        var result = await _mediator.Send(query with { DonorId = id });
        return Ok(result);
    }

    [HttpPost("{id:guid}/tasks")]
    [Authorize(Policy = "ProcurementOrAbove")]
    public async Task<IActionResult> CreateDonorTask(Guid id, [FromBody] CreateTaskRequest request)
    {
        var result = await _mediator.Send(new CreateTaskCommand
        {
            DonorId = id,
            Title = request.Title,
            Description = request.Description,
            AssignedToUserId = request.AssignedToUserId,
            DueDate = request.DueDate
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
            OriginalFileName = file.FileName,
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
