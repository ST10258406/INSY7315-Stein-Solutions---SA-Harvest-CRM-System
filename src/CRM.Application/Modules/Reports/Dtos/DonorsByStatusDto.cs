namespace CRM.Application.Modules.Reports.Dtos;

/// <summary>
/// One row of GET /api/v1/reports/donors-by-status. donor_status is a single-value enum
/// column on donors (not a many-to-many join table like region/type), so DonorCount values
/// always sum to the total donor count.
/// </summary>
public class DonorsByStatusDto
{
    public string Status { get; set; } = string.Empty;
    public int DonorCount { get; set; }
}
