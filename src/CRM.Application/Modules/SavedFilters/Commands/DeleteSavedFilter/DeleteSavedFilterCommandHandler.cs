namespace CRM.Application.Modules.SavedFilters.Commands.DeleteSavedFilter;

using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Domain.Entities;
using MediatR;

public class DeleteSavedFilterCommandHandler : IRequestHandler<DeleteSavedFilterCommand>
{
    private readonly ISavedFilterRepository _savedFilters;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public DeleteSavedFilterCommandHandler(
        ISavedFilterRepository savedFilters, IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _savedFilters = savedFilters;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task Handle(DeleteSavedFilterCommand command, CancellationToken cancellationToken)
    {
        // Another user's filter is reported as not found (never 403), so ids can't be probed.
        var filter = await _savedFilters.GetActiveForUpdateAsync(command.Id, _currentUserService.GetCurrentUserId(), cancellationToken)
            ?? throw new NotFoundException(nameof(SavedFilter), command.Id);

        // Soft delete only: no hard deletes (copilot rule).
        filter.IsActive = false;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        command.EntityId = filter.Id;
        command.OldValues = new { filter.Name, filter.QueryString, IsActive = true };
        command.NewValues = new { IsActive = false };
    }
}
