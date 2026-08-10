namespace CRM.Infrastructure.Tests.Services;

using CRM.Application.Common.Exceptions;
using CRM.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Xunit;

public class CurrentUserServiceTests
{
    [Fact]
    public void GetCurrentUserId_ReturnsId_WhenUserIsAuthenticated()
    {
        var id = Guid.NewGuid();
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, id.ToString())
        }, "TestAuth"));

        var accessor = new HttpContextAccessor { HttpContext = context };
        var service = new CurrentUserService(accessor);

        var result = service.GetCurrentUserId();

        Assert.Equal(id, result);
    }

    [Fact]
    public void GetCurrentUserId_ThrowsUnauthorizedException_WhenUserIsNotAuthenticated()
    {
        var context = new DefaultHttpContext();
        var accessor = new HttpContextAccessor { HttpContext = context };
        var service = new CurrentUserService(accessor);

        Assert.Throws<UnauthorizedException>(() => service.GetCurrentUserId());
    }

    [Fact]
    public void GetCurrentUserRoles_ReturnsRoles_WhenRolesExist()
    {
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim(ClaimTypes.Role, "SuperAdmin")
        }, "TestAuth"));

        var accessor = new HttpContextAccessor { HttpContext = context };
        var service = new CurrentUserService(accessor);

        var result = service.GetCurrentUserRoles();

        Assert.Contains("Admin", result);
        Assert.Contains("SuperAdmin", result);
        Assert.Equal(2, result.Count);
    }
}
