namespace CRM.API.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/v1/policy-test")]
public class PolicyTestController : ControllerBase
{
    [HttpGet("marketing")]
    [Authorize(Policy = "MarketingOrAbove")]
    public IActionResult MarketingCheck() => Ok(new { message = "You're Marketing or above." });

    [HttpGet("procurement")]
    [Authorize(Policy = "ProcurementOrAbove")]
    public IActionResult ProcurementCheck() => Ok(new { message = "You're Procurement or above." });

    [HttpGet("admin")]
    [Authorize(Policy = "AdminOrAbove")]
    public IActionResult AdminCheck() => Ok(new { message = "You're Admin or above." });

    [HttpGet("superadmin")]
    [Authorize(Policy = "SuperAdminOnly")]
    public IActionResult SuperAdminCheck() => Ok(new { message = "You're SuperAdmin." });
}
