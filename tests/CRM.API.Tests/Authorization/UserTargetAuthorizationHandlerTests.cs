using System.Security.Claims;
using CRM.API.Authorization;
using CRM.Application.Modules.Users.Dtos;
using CRM.Domain.Constants;
using Microsoft.AspNetCore.Authorization;

namespace CRM.API.Tests.Authorization;

public class UserTargetAuthorizationHandlerTests
{
    private readonly UserTargetAuthorizationHandler _handler = new();

    private static ClaimsPrincipal MakeUser(params string[] roles)
    {
        var claims = roles.Select(r => new Claim(ClaimTypes.Role, r));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
    }

    private static UserAuthorizationTarget MakeTarget(string email, params string[] roles) =>
        new() { Id = Guid.NewGuid(), Email = email, RoleNames = roles.ToList() };

    private async Task<bool> SucceedsAsync(ClaimsPrincipal caller, UserAuthorizationTarget target)
    {
        var context = new AuthorizationHandlerContext([new ManageTargetUserRequirement()], caller, target);
        await _handler.HandleAsync(context);
        return context.HasSucceeded;
    }

    [Fact]
    public async Task HandleAsync_AdminTargetingSuperAdmin_DoesNotSucceed()
        => Assert.False(await SucceedsAsync(MakeUser("Admin"), MakeTarget("owner@saharvest.org", "SuperAdmin")));

    [Fact]
    public async Task HandleAsync_AdminTargetingUserHoldingSuperAdminAmongOtherRoles_DoesNotSucceed()
        => Assert.False(await SucceedsAsync(MakeUser("Admin"), MakeTarget("owner@saharvest.org", "Admin", "SuperAdmin")));

    [Fact]
    public async Task HandleAsync_SuperAdminTargetingSuperAdmin_Succeeds()
        => Assert.True(await SucceedsAsync(MakeUser("SuperAdmin"), MakeTarget("owner@saharvest.org", "SuperAdmin")));

    [Fact]
    public async Task HandleAsync_AdminTargetingAdmin_Succeeds()
        => Assert.True(await SucceedsAsync(MakeUser("Admin"), MakeTarget("colleague@saharvest.org", "Admin")));

    [Fact]
    public async Task HandleAsync_AdminTargetingUserWithNoRole_Succeeds()
        => Assert.True(await SucceedsAsync(MakeUser("Admin"), MakeTarget("new-hire@saharvest.org")));

    [Theory]
    [InlineData("Admin")]
    [InlineData("SuperAdmin")]
    public async Task HandleAsync_AnyCallerTargetingSystemUser_DoesNotSucceed(string callerRole)
        => Assert.False(await SucceedsAsync(MakeUser(callerRole), MakeTarget(SystemUsers.PublicFormEmail)));

    [Fact]
    public async Task HandleAsync_SuperAdminTargetingAnyReservedDomainAddress_DoesNotSucceed()
        => Assert.False(await SucceedsAsync(MakeUser("SuperAdmin"), MakeTarget("Future-Job@SYSTEM.LOCAL")));
}
