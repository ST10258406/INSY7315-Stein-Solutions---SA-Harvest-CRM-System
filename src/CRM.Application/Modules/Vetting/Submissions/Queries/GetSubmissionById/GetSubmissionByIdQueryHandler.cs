namespace CRM.Application.Modules.Vetting.Submissions.Queries.GetSubmissionById;

using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Vetting.Submissions.Dtos;
using CRM.Domain.Entities.Vetting;
using MediatR;

public class GetSubmissionByIdQueryHandler : IRequestHandler<GetSubmissionByIdQuery, VettingSubmissionDto>
{
    private readonly IVettingSubmissionRepository _submissions;

    public GetSubmissionByIdQueryHandler(IVettingSubmissionRepository submissions) => _submissions = submissions;

    public async Task<VettingSubmissionDto> Handle(GetSubmissionByIdQuery request, CancellationToken cancellationToken)
        => await _submissions.GetDtoByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(VettingSubmission), request.Id);
}
