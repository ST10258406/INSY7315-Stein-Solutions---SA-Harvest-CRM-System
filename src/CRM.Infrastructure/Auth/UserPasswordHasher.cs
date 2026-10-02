namespace CRM.Infrastructure.Auth;

using CRM.Application.Common.Interfaces;
using CRM.Domain.Entities;
using Microsoft.AspNetCore.Identity;

/// <summary>
/// Wraps the standalone <see cref="PasswordHasher{TUser}"/> already used by Login /
/// ChangePassword / ResetPassword. ASP.NET Core Identity's hasher doesn't actually use the
/// TUser instance's data — the salt lives in the hash output — so hashing with a dummy user
/// here produces a value that verifies correctly against a real User elsewhere.
/// </summary>
public class UserPasswordHasher : IUserPasswordHasher
{
    private readonly PasswordHasher<User> _hasher = new();

    public string HashPassword(string password) => _hasher.HashPassword(null!, password);
}
