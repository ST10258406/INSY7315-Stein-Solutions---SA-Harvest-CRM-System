using CRM.Application.Common.Models;
using CRM.Application.Modules.Donors.Dtos;
using MediatR;

namespace CRM.Application.Modules.Donors.Queries.GetDonors;

public record GetDonorsQuery : PaginationParams, IRequest<PaginatedResult<DonorListItemDto>>
{
    public string? Search { get; init; }
    public string? Status { get; init; }
    public short? CompanyTypeId { get; init; }
    public string? RegionCode { get; init; }
    public short? DonationTypeId { get; init; }
    public short? DonationFrequencyId { get; init; }
    public Guid? RelationshipManagerId { get; init; }
    public DateOnly? FollowUpBefore { get; init; }
}
