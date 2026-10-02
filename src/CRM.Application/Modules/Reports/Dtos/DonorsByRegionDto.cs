namespace CRM.Application.Modules.Reports.Dtos;

/// <summary>
/// One row of GET /api/v1/reports/donors-by-region. A donor operating in more than one
/// region is counted once per region — this report measures regional coverage, not
/// deduplicated donor headcount — so DonorCount values are not guaranteed to sum to the
/// total donor count.
/// </summary>
public class DonorsByRegionDto
{
    public string Region { get; set; } = string.Empty;
    public string RegionName { get; set; } = string.Empty;
    public int DonorCount { get; set; }
}
