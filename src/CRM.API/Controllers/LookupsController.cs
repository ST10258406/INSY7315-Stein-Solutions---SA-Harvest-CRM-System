using CRM.Application.Modules.Lookups.Queries.GetCodedLookup;
using CRM.Application.Modules.Lookups.Queries.GetLookup;
using CRM.Application.Modules.Lookups.Queries.GetRelationshipManagers;
using CRM.Application.Modules.Lookups.Queries.GetRoles;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/v1/lookups")]
[Authorize(Policy = "ProcurementOrAbove")]
public class LookupsController : ControllerBase
{
    private readonly IMediator _mediator;

    public LookupsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("company-types")]
    public async Task<IActionResult> GetCompanyTypes()
        => Ok(new { data = await _mediator.Send(new GetLookupQuery(LookupType.CompanyTypes)) });

    [HttpGet("entity-types")]
    public async Task<IActionResult> GetEntityTypes()
        => Ok(new { data = await _mediator.Send(new GetLookupQuery(LookupType.EntityTypes)) });

    [HttpGet("operational-regions")]
    public async Task<IActionResult> GetOperationalRegions()
        => Ok(new { data = await _mediator.Send(new GetCodedLookupQuery(CodedLookupType.OperationalRegions)) });

    [HttpGet("donation-types")]
    public async Task<IActionResult> GetDonationTypes()
        => Ok(new { data = await _mediator.Send(new GetLookupQuery(LookupType.DonationTypes)) });

    [HttpGet("donation-frequencies")]
    public async Task<IActionResult> GetDonationFrequencies()
        => Ok(new { data = await _mediator.Send(new GetLookupQuery(LookupType.DonationFrequencies)) });

    [HttpGet("bbbee-statuses")]
    public async Task<IActionResult> GetBbbeeStatuses()
        => Ok(new { data = await _mediator.Send(new GetLookupQuery(LookupType.BbbeeStatuses)) });

    [HttpGet("provinces")]
    public async Task<IActionResult> GetProvinces()
        => Ok(new { data = await _mediator.Send(new GetCodedLookupQuery(CodedLookupType.Provinces)) });

    [HttpGet("relationship-managers")]
    public async Task<IActionResult> GetRelationshipManagers()
        => Ok(new { data = await _mediator.Send(new GetRelationshipManagersQuery()) });

    // Tighter than the controller-level policy — role names back the Users screen's
    // dropdowns, which only Admin/SuperAdmin can reach.
    [HttpGet("roles")]
    [Authorize(Policy = "AdminOrAbove")]
    public async Task<IActionResult> GetRoles()
        => Ok(new { data = await _mediator.Send(new GetRolesQuery()) });
}
