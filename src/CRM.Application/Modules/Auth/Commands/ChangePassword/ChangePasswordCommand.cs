using System.Text.Json.Serialization;
using MediatR;

namespace CRM.Application.Modules.Auth.Commands.ChangePassword;

public record ChangePasswordCommand(
    string CurrentPassword,
    string NewPassword,
    string ConfirmPassword) : IRequest
{
    /// <summary>
    /// The caller's own refresh token, from their HttpOnly cookie — set by the API layer,
    /// never bound from the request body. Identifies which session to keep alive while
    /// every other session is signed out.
    /// </summary>
    [JsonIgnore]
    public string? CurrentRefreshToken { get; init; }
}
