using CRM.Application.Modules.Auth.Commands.ChangePassword;
using CRM.Application.Modules.Auth.Commands.ForgotPassword;
using CRM.Application.Modules.Auth.Commands.Login;
using CRM.Application.Modules.Auth.Commands.Logout;
using CRM.Application.Modules.Auth.Commands.Refresh;
using CRM.Application.Modules.Auth.Commands.ResetPassword;
using CRM.Application.Modules.Auth.Dtos;
using CRM.API.Authentication;
using CRM.API.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IConfiguration _configuration;

    public AuthController(IMediator mediator, IConfiguration configuration)
    {
        _mediator = mediator;
        _configuration = configuration;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingExtensions.AuthLoginPolicy)]
    [IssuesRefreshCookie] // refresh token → HttpOnly cookie only; it's [JsonIgnore]d in the body
    public async Task<ActionResult<LoginResponseDto>> Login(LoginCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    // Also what the SPA calls on page load to restore a session from the cookie alone.
    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingExtensions.AuthRefreshPolicy)]
    [RequireCsrfHeader]
    [IssuesRefreshCookie] // rotated cookie on success; cookie deleted on 401
    public async Task<ActionResult<RefreshTokenResponseDto>> Refresh([FromRefreshCookie] string? refreshToken)
    {
        var result = await _mediator.Send(new RefreshTokenCommand(refreshToken));
        return Ok(result);
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingExtensions.PublicFormPolicy)]
    public async Task<ActionResult<ForgotPasswordResponseDto>> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var baseUrl = _configuration["Frontend:BaseUrl"]
            ?? throw new InvalidOperationException("Frontend:BaseUrl is not configured.");

        var result = await _mediator.Send(new ForgotPasswordCommand
        {
            Email = request.Email,
            ResetPasswordUrl = $"{baseUrl.TrimEnd('/')}/reset-password"
        });
        return Ok(result);
    }

    [HttpPost("logout")]
    [Authorize]
    [RequireCsrfHeader]
    [ClearsRefreshCookie]
    public async Task<IActionResult> Logout([FromRefreshCookie] string? refreshToken)
    {
        await _mediator.Send(new LogoutCommand(refreshToken));
        return NoContent();
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingExtensions.AuthResetPasswordPolicy)]
    public async Task<ActionResult<ResetPasswordResponseDto>> ResetPassword(ResetPasswordCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpPatch("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordCommand command, [FromRefreshCookie] string? refreshToken)
    {
        // The cookie identifies the caller's own session, which survives; all others are revoked.
        await _mediator.Send(command with { CurrentRefreshToken = refreshToken });
        return NoContent();
    }
}
