namespace CRM.API.Authentication;

/// <summary>
/// Marks an endpoint that a user with <c>MustChangePassword</c> set may still call
/// (change-password and logout). Every other authenticated endpoint is rejected by
/// <see cref="CRM.API.Middleware.PasswordChangeRequiredMiddleware"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class AllowWhenPasswordChangeRequiredAttribute : Attribute
{
}
