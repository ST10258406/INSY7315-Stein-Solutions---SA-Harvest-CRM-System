using CRM.Application.Common.Models;

namespace CRM.Application.Modules.Donors.Dtos;

public class DonorDonationsDto
{
    public LookupDto Frequency { get; set; } = null!;
    public List<LookupDto> Types { get; set; } = new();
    public string? CollectionAddress { get; set; }
    public string? OperationsLogisticsDetails { get; set; }
    public List<RegionDto> OperationalRegions { get; set; } = new();
}

public class RegionDto
{
    public short Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
