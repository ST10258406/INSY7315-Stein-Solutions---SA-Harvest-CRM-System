using CRM.Application.Modules.Auth.Dtos;
using MediatR;

namespace CRM.Application.Modules.Auth.Commands.Refresh;

public record RefreshTokenCommand(string RefreshToken) : IRequest<RefreshTokenResponseDto>;
