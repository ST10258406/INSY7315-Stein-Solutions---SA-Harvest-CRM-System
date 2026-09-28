using System.Security.Claims;
using CRM.API.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace CRM.API.Tests.Authorization;

public class RoleAssignmentAuthorizationHandlerTests
{
    private readonly RoleAssignmentAuthorizationHandler _handler = new();

    private static ClaimsPrincipal MakeUser(params string[] roles)
    {
        var claims = roles.Select(r => new Claim(ClaimTypes.Role, r));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
    }

    private static AuthorizationHandlerContext MakeContext(ClaimsPrincipal user, string resource) =>
        new([new RestrictedRoleAssignmentRequirement()], user, resource);

    [Fact]
    public async Task HandleAsync_AdminAssigningSuperAdmin_DoesNotSucceed()
    {
        var context = MakeContext(MakeUser("Admin"), "SuperAdmin");

        await _handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleAsync_SuperAdminAssigningSuperAdmin_Succeeds()
    {
        var context = MakeContext(MakeUser("SuperAdmin"), "SuperAdmin");

        await _handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleAsync_AdminAssigningMarketing_Succeeds()
    {
        var context = MakeContext(MakeUser("Admin"), "Marketing");

        await _handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleAsync_AdminAssigningUnresolvedRole_Succeeds()
    {
        // "" means GetRoleNameAsync found nothing for the submitted RoleId — that's a 400
        // from CreateUserCommandValidator's RoleExistsAsync check, not this handler's
        // concern, so it must not block on a role it doesn't recognize as SuperAdmin.
        var context = MakeContext(MakeUser("Admin"), string.Empty);

        await _handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }
}
