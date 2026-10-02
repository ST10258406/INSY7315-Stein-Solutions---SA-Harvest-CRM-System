using CRM.API.Extensions;
using CRM.Application.Modules.Lookups.Queries.GetCodedLookup;
using CRM.Application.Modules.Lookups.Queries.GetLookup;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CRM.API.Controllers;

/// <summary>
/// Unauthenticated mirror of LookupsController, for populating dropdowns on the
/// public donor onboarding form before the user is authenticated. Deliberately
/// calls the exact same query handlers as the protected /lookups/* endpoints —
/// same data, just a different route with no [Authorize] and a public rate limit.
/// Intentionally excludes GetRelationshipManagers (staff-only data, no protected
/// equivalent is listed for this endpoint set).
///
/// [ResponseCache] below is a nice-to-have per the issue, not a hard requirement:
/// this reference data changes essentially never, and every visitor loading the
/// public form hits all seven endpoints, so a short client/proxy cache trims load
/// without risking stale dropdowns for more than a few minutes. It only sets the
/// Cache-Control/Expires headers (no server-side ResponseCaching middleware is
/// registered), so it's just a hint to the browser/CDN, not a server-side cache.
/// </summary>
[ApiController]
[Route("api/v1/public/lookups")]
[AllowAnonymous]
[EnableRateLimiting(RateLimitingExtensions.PublicLookupsPolicy)]
[ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
public class PublicLookupsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PublicLookupsController(IMediator mediator)
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
}
