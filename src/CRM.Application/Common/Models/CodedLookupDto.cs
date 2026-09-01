namespace CRM.Application.Common.Models;

public class CodedLookupDto
{
    public short Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
