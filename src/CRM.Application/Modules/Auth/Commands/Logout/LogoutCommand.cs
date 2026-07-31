using MediatR;

namespace CRM.Application.Modules.Auth.Commands.Logout;

public record LogoutCommand(string RefreshToken) : IRequest;
