namespace CRM.API.Controllers;

using CRM.API.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

/// <summary>
/// Exercises the named public rate limit policies ahead of the real /public/*
/// controllers (see #74, #2, #3, #4) so the limiter configuration in
/// RateLimitingExtensions.cs can be verified now, the same way
/// PolicyTestController lets authorization policies be checked in isolation.
/// The real donor-submission and lookups controllers apply these same named
/// policies via [EnableRateLimiting(...)] once they land.
/// </summary>
[ApiController]
[Route("api/v1/rate-limit-test")]
[AllowAnonymous]
public class RateLimitTestController : ControllerBase
{
    [HttpPost("submit")]
    [EnableRateLimiting(RateLimitingExtensions.PublicSubmitPolicy)]
    public IActionResult Submit() => Ok(new { message = "ok" });

    [HttpGet("lookups")]
    [EnableRateLimiting(RateLimitingExtensions.PublicLookupsPolicy)]
    public IActionResult Lookups() => Ok(new { message = "ok" });
}
