using CRM.Application.Modules.Auth.Dtos;
using MediatR;

namespace CRM.Application.Modules.Auth.Commands.Refresh;

/// <summary>The raw refresh token from the caller's HttpOnly cookie — null when there is no cookie (a 401, like any invalid token).</summary>
public record RefreshTokenCommand(string? RefreshToken) : IRequest<RefreshTokenResponseDto>;
