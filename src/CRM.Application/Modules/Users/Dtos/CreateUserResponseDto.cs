namespace CRM.Application.Modules.Users.Dtos;

/// <summary>
/// Returned once, immediately after creation, so the admin can hand the temporary
/// password to the new user. It is never persisted or retrievable again — only the
/// hash is stored (see CreateUserCommandHandler).
/// </summary>
public class CreateUserResponseDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public Guid RoleId { get; set; }
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public string TemporaryPassword { get; set; } = string.Empty;
}
