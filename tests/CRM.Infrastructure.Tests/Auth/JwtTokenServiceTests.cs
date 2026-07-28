using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CRM.Domain.Entities;
using CRM.Infrastructure.Auth;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CRM.Infrastructure.Tests.Auth;

public class JwtTokenServiceTests
{
    private readonly JwtSettings _settings;
    private readonly JwtTokenService _sut;

    public JwtTokenServiceTests()
    {
        _settings = new JwtSettings
        {
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            SigningKey = "this-is-a-super-secret-key-that-is-at-least-32-chars-long!",
            AccessTokenExpiryMinutes = 60
        };

        _sut = new JwtTokenService(Options.Create(_settings));
    }

    [Fact]
    public void GenerateAccessToken_ReturnsTokenWithCorrectClaims()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            UserRoles = new List<UserRole> { new UserRole { Role = new Role { Name = "Admin" } } }
        };

        // Act
        var token = _sut.GenerateAccessToken(user);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        Assert.Equal(_settings.Issuer, jwtToken.Issuer);
        Assert.Equal(_settings.Audience, jwtToken.Audiences.First());
        
        Assert.Equal(user.Id.ToString(), jwtToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal(user.Email, jwtToken.Claims.First(c => c.Type == ClaimTypes.Email).Value);
        Assert.Equal(user.FirstName, jwtToken.Claims.First(c => c.Type == ClaimTypes.GivenName).Value);
        Assert.Equal(user.LastName, jwtToken.Claims.First(c => c.Type == ClaimTypes.Surname).Value);
        Assert.Contains(jwtToken.Claims, c => c.Type == ClaimTypes.Role && c.Value == "Admin");
    }

    [Fact]
    public void GenerateAccessToken_SetsExpiryToConfiguredMinutes()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            UserRoles = new List<UserRole>()
        };

        // Act
        var token = _sut.GenerateAccessToken(user);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        var expectedExpiry = DateTime.UtcNow.AddMinutes(_settings.AccessTokenExpiryMinutes);
        
        // Allow a few seconds tolerance
        Assert.True(jwtToken.ValidTo >= expectedExpiry.AddSeconds(-5) && jwtToken.ValidTo <= expectedExpiry.AddSeconds(5));
    }

    [Fact]
    public void GenerateAccessToken_TokenValidatesAgainstSigningKey()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            UserRoles = new List<UserRole>()
        };

        var token = _sut.GenerateAccessToken(user);

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _settings.Issuer,
            ValidateAudience = true,
            ValidAudience = _settings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SigningKey)),
            ValidateLifetime = true
        };

        var handler = new JwtSecurityTokenHandler();

        // Act & Assert
        // This will throw an exception if validation fails
        handler.ValidateToken(token, validationParameters, out var validatedToken);
        Assert.NotNull(validatedToken);
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsUniqueValueEachCall()
    {
        // Act
        var token1 = _sut.GenerateRefreshToken();
        var token2 = _sut.GenerateRefreshToken();

        // Assert
        Assert.False(string.IsNullOrEmpty(token1));
        Assert.False(string.IsNullOrEmpty(token2));
        Assert.NotEqual(token1, token2);
    }
}
