using MediatR;

namespace CRM.Application.Modules.Auth.Commands.Logout;

/// <summary>The raw refresh token from the caller's HttpOnly cookie — null when there is no cookie.</summary>
public record LogoutCommand(string? RefreshToken) : IRequest;
