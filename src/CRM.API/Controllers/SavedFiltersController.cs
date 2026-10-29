using CRM.Application.Modules.SavedFilters.Commands.CreateSavedFilter;
using CRM.Application.Modules.SavedFilters.Commands.DeleteSavedFilter;
using CRM.Application.Modules.SavedFilters.Queries.GetSavedFilters;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers;

/// <summary>
/// Saved donor-list filters (Issue 103). Same policy as the donor list they apply to. Every
/// operation is scoped to the caller inside the handlers.
/// </summary>
[ApiController]
[Route("api/v1/saved-filters")]
[Authorize(Policy = "ProcurementOrAbove")]
public class SavedFiltersController : ControllerBase
{
    private readonly IMediator _mediator;

    public SavedFiltersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetSavedFilters([FromQuery] GetSavedFiltersQuery query)
    {
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateSavedFilter([FromBody] CreateSavedFilterCommand command)
    {
        var result = await _mediator.Send(command);
        return StatusCode(201, new { data = result });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteSavedFilter(Guid id)
    {
        await _mediator.Send(new DeleteSavedFilterCommand { Id = id });
        return NoContent();
    }
}
