namespace CRM.Application.Modules.Vetting.Officers.Dtos;

/// <summary>Request body for POST /api/v1/vetting/officers. workloadCount is deliberately absent: it is always recounted server-side.</summary>
public class CreateOfficerRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Initials { get; set; } = string.Empty;
    public string AssignedRegion { get; set; } = string.Empty;
}
