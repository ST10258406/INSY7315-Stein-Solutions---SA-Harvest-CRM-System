namespace CRM.Application.Modules.Auth.Dtos;

/// <summary>Request body for POST /api/auth/forgot-password.</summary>
public class ForgotPasswordRequest
{
    public string Email { get; set; } = string.Empty;
}
