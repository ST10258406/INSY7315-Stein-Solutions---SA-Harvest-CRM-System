using System.Security.Claims;
using CRM.API.Authorization;
using CRM.Domain.Enums;
using Microsoft.AspNetCore.Authorization;

namespace CRM.API.Tests.Authorization;

public class DocumentTypeAuthorizationHandlerTests
{
    private readonly DocumentTypeAuthorizationHandler _handler = new();

    private static ClaimsPrincipal MakeUser(params string[] roles)
    {
        var claims = roles.Select(r => new Claim(ClaimTypes.Role, r));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
    }

    private static AuthorizationHandlerContext MakeContext(ClaimsPrincipal user, DocumentType resource) =>
        new([new RestrictedDocumentTypeRequirement()], user, resource);

    [Fact]
    public async Task HandleAsync_ProcurementRequestingBBBEECertificate_DoesNotSucceed()
    {
        var context = MakeContext(MakeUser("Procurement"), DocumentType.BBBEECertificate);

        await _handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleAsync_AdminRequestingBBBEECertificate_Succeeds()
    {
        var context = MakeContext(MakeUser("Admin"), DocumentType.BBBEECertificate);

        await _handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleAsync_SuperAdminRequestingBBBEECertificate_Succeeds()
    {
        var context = MakeContext(MakeUser("SuperAdmin"), DocumentType.BBBEECertificate);

        await _handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleAsync_ProcurementRequestingSignature_Succeeds()
    {
        var context = MakeContext(MakeUser("Procurement"), DocumentType.Signature);

        await _handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }
}
