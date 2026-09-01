namespace CRM.Application.Modules.Donors.Dtos;

public class DonorContactDto
{
    public string Name { get; set; } = string.Empty;
    public string? JobTitle { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
}
