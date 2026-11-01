namespace CRM.Application.Modules.Vetting.Officers.Dtos;

/// <summary>Request body for PATCH /api/v1/vetting/officers/{id}. A null field is left unchanged.</summary>
public class UpdateOfficerRequest
{
    public string? FullName { get; set; }
    public string? Initials { get; set; }
    public string? AssignedRegion { get; set; }
}
