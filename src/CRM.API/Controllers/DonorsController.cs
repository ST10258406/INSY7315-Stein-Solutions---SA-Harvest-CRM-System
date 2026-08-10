using CRM.Application.Common.Models;
using CRM.Application.Modules.Donors.Dtos;
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
}
