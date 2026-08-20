using CRM.Application.Common.Models;
using CRM.Application.Modules.Donors.Commands.CreateDonor;
using CRM.Application.Modules.Donors.Dtos;
using CRM.Application.Modules.Donors.Queries.GetDonorById;
using CRM.Application.Modules.Donors.Queries.GetDonors;
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
}
