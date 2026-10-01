using CRM.Application.Modules.Auth.Commands.ChangePassword;
using CRM.Application.Modules.Auth.Commands.ForgotPassword;
using CRM.Application.Modules.Auth.Commands.Login;
using CRM.Application.Modules.Auth.Commands.Logout;
using CRM.Application.Modules.Auth.Commands.Refresh;
using CRM.Application.Modules.Auth.Commands.ResetPassword;
using CRM.Application.Modules.Auth.Dtos;
using CRM.API.Authentication;
using CRM.API.Extensions;
using CRM.Application.Common.Exceptions;
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
    private readonly RefreshTokenCookie _refreshCookie;

    public AuthController(IMediator mediator, IConfiguration configuration, RefreshTokenCookie refreshCookie)
    {
        _mediator = mediator;
        _configuration = configuration;
        _refreshCookie = refreshCookie;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingExtensions.AuthLoginPolicy)]
    public async Task<ActionResult<LoginResponseDto>> Login(LoginCommand command)
    {
        var result = await _mediator.Send(command);
        // The refresh token goes only into the HttpOnly cookie; it's [JsonIgnore]d in the body.
        _refreshCookie.Write(Response, result.RefreshToken, result.RefreshTokenExpiresAt);
        return Ok(result);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingExtensions.AuthRefreshPolicy)]
    [RequireCsrfHeader]
    public async Task<ActionResult<RefreshTokenResponseDto>> Refresh()
    {
        // Also what the SPA calls on page load to restore a session from the cookie alone.
        var refreshToken = _refreshCookie.Read(Request)
            ?? throw new UnauthorizedException("Refresh token is invalid or expired.");

        try
        {
            var result = await _mediator.Send(new RefreshTokenCommand(refreshToken));
            // Rotation: the presented token is now revoked, so replace the cookie.
            _refreshCookie.Write(Response, result.RefreshToken, result.RefreshTokenExpiresAt);
            return Ok(result);
        }
        catch (UnauthorizedException)
        {
            // Dead session — drop the cookie so the browser stops presenting it.
            _refreshCookie.Delete(Response);
            throw;
        }
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
    public async Task<IActionResult> Logout()
    {
        await _mediator.Send(new LogoutCommand(_refreshCookie.Read(Request)));
        _refreshCookie.Delete(Response);
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
        // The cookie identifies the caller's own session, which survives; all others are revoked.
        await _mediator.Send(command with { CurrentRefreshToken = _refreshCookie.Read(Request) });
        return NoContent();
    }
}
