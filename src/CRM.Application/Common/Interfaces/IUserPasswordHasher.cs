namespace CRM.Application.Common.Interfaces;

/// <summary>
/// Application-owned abstraction over password hashing — keeps CRM.Application free of a
/// direct Microsoft.AspNetCore.Identity dependency (Clean Architecture layer rule). The
/// Infrastructure implementation wraps the same standalone PasswordHasher&lt;User&gt; used
/// by Login/ChangePassword/ResetPassword, producing hashes compatible with theirs.
/// </summary>
public interface IUserPasswordHasher
{
    string HashPassword(string password);
}
