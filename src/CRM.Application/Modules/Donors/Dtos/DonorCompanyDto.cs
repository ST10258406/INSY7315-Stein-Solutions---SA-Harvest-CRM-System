using CRM.Application.Common.Models;

namespace CRM.Application.Modules.Donors.Dtos;

public class DonorCompanyDto
{
    public string CompanyName { get; set; } = string.Empty;
    public LookupDto CompanyType { get; set; } = null!;
    public string? Website { get; set; }
    public string RegisteredCompanyName { get; set; } = string.Empty;
    public string? TradingName { get; set; }
    public LookupDto EntityType { get; set; } = null!;
    public string? CompanyRegistrationNumber { get; set; }
    public string? IncomeTaxNumber { get; set; }
}
