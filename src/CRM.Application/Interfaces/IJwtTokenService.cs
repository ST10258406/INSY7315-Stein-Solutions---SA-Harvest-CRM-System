namespace CRM.Application.Interfaces;

using CRM.Domain.Entities;

public interface IJwtTokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
}
