using CRM.Application.Modules.Auth.Dtos;
using MediatR;

namespace CRM.Application.Modules.Auth.Commands.ForgotPassword;

public record ForgotPasswordCommand(string Email) : IRequest<ForgotPasswordResponseDto>;
