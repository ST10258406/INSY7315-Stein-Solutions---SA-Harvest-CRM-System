namespace CRM.Application.Modules.SavedFilters.Dtos;

public record SavedFilterDto(Guid Id, string Name, string Page, string QueryString);
