namespace CRM.Application.Modules.Vetting.Officers.Queries.GetOfficers;

using CRM.Application.Modules.Vetting.Officers.Dtos;
using MediatR;

public record GetOfficersQuery : IRequest<List<VettingOfficerDto>>;
