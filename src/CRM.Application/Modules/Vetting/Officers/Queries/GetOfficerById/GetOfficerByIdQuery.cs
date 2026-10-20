namespace CRM.Application.Modules.Vetting.Officers.Queries.GetOfficerById;

using CRM.Application.Modules.Vetting.Officers.Dtos;
using MediatR;

public record GetOfficerByIdQuery(Guid Id) : IRequest<VettingOfficerDto>;
