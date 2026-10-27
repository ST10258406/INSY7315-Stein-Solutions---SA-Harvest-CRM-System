namespace CRM.Application.Modules.Vetting.Officers.Dtos;

public class VettingOfficerDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Initials { get; set; } = string.Empty;
    public string AssignedRegion { get; set; } = string.Empty;
    public int WorkloadCount { get; set; }
}
