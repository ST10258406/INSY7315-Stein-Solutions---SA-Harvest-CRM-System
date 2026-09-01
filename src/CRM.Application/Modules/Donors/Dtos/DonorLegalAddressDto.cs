namespace CRM.Application.Modules.Donors.Dtos;

public class DonorLegalAddressDto
{
    public string StreetNameNumber { get; set; } = string.Empty;
    public string Suburb { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public ProvinceDto Province { get; set; } = null!;
    public string PostalCode { get; set; } = string.Empty;
}

public class ProvinceDto
{
    public short Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
