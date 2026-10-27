using CRM.Application.Modules.Vetting.Officers.Commands.CreateOfficer;
using CRM.Application.Modules.Vetting.Officers.Commands.UpdateOfficer;
using CRM.Application.Modules.Vetting.Officers.Dtos;
using CRM.Application.Modules.Vetting.Officers.Queries.GetOfficerById;
using CRM.Application.Modules.Vetting.Officers.Queries.GetOfficers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/v1/vetting/officers")]
public class VettingOfficersController : ControllerBase
{
    private readonly IMediator _mediator;

    public VettingOfficersController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [Authorize(Policy = "ProcurementOrAbove")] // fills the officer dropdown
    public async Task<IActionResult> GetOfficers()
        => Ok(new { data = await _mediator.Send(new GetOfficersQuery()) });

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "ProcurementOrAbove")]
    public async Task<IActionResult> GetOfficer(Guid id)
        => Ok(new { data = await _mediator.Send(new GetOfficerByIdQuery(id)) });

    [HttpPost]
    [Authorize(Policy = "AdminOrAbove")]
    public async Task<IActionResult> CreateOfficer([FromBody] CreateOfficerRequest request)
    {
        var result = await _mediator.Send(new CreateOfficerCommand
        {
            FullName = request.FullName,
            Initials = request.Initials,
            AssignedRegion = request.AssignedRegion
        });
        return CreatedAtAction(nameof(GetOfficer), new { id = result.Id }, new { data = result });
    }

    [HttpPatch("{id:guid}")]
    [Authorize(Policy = "AdminOrAbove")]
    public async Task<IActionResult> UpdateOfficer(Guid id, [FromBody] UpdateOfficerRequest request)
    {
        var result = await _mediator.Send(new UpdateOfficerCommand
        {
            Id = id,
            FullName = request.FullName,
            Initials = request.Initials,
            AssignedRegion = request.AssignedRegion
        });
        return Ok(new { data = result });
    }
}
