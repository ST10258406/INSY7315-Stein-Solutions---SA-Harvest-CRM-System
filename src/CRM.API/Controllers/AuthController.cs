using CRM.Application.Modules.Auth.Commands.ChangePassword;
using CRM.Application.Modules.Auth.Commands.ForgotPassword;
using CRM.Application.Modules.Auth.Commands.Login;
using CRM.Application.Modules.Auth.Commands.Logout;
using CRM.Application.Modules.Auth.Commands.Refresh;
using CRM.Application.Modules.Auth.Commands.ResetPassword;
using CRM.Application.Modules.Auth.Dtos;
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
    public async Task<ActionResult<LoginResponseDto>> Login(LoginCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingExtensions.AuthRefreshPolicy)]
    public async Task<ActionResult<RefreshTokenResponseDto>> Refresh(RefreshTokenCommand command)
    {
        var result = await _mediator.Send(command);
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
    public async Task<IActionResult> Logout(LogoutCommand command)
    {
        await _mediator.Send(command);
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
    public async Task<IActionResult> ChangePassword(ChangePasswordCommand command)
    {
        await _mediator.Send(command);
        return NoContent();
    }
}
