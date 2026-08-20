using CRM.Application.Modules.Donors.Dtos;
using MediatR;

namespace CRM.Application.Modules.Donors.Queries.GetDonorById;

public record GetDonorByIdQuery(Guid Id) : IRequest<DonorDetailDto>;
