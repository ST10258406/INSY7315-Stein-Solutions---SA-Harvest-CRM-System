namespace CRM.Application.Modules.Vetting.Submissions.Queries.GetSubmissionById;

using CRM.Application.Modules.Vetting.Submissions.Dtos;
using MediatR;

public record GetSubmissionByIdQuery(Guid Id) : IRequest<VettingSubmissionDto>;
