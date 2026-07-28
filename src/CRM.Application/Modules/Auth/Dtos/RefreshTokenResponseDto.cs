namespace CRM.Application.Modules.Auth.Dtos;

public class RefreshTokenResponseDto
{
    public string AccessToken { get; set; } = default!;
    public int ExpiresIn { get; set; } // seconds
}
