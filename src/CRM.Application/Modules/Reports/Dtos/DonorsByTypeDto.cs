namespace CRM.Application.Modules.Reports.Dtos;

/// <summary>
/// One row of GET /api/v1/reports/donors-by-type. A donor offering more than one donation
/// type is counted once per type — this report measures donation-type coverage, not
/// deduplicated donor headcount — so DonorCount values are not guaranteed to sum to the
/// total donor count.
/// </summary>
public class DonorsByTypeDto
{
    public string DonationType { get; set; } = string.Empty;
    public int DonorCount { get; set; }
}
