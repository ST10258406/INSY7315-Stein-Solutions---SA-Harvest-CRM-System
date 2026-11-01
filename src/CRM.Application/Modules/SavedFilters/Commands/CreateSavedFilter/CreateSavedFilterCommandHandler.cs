namespace CRM.Application.Modules.SavedFilters.Commands.CreateSavedFilter;

using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.SavedFilters.Dtos;
using CRM.Domain.Entities;
using CRM.Domain.Enums;
using FluentValidation.Results;
using MediatR;

public class CreateSavedFilterCommandHandler : IRequestHandler<CreateSavedFilterCommand, SavedFilterDto>
{
    public const int MaxActivePerUser = 50;

    private readonly ISavedFilterRepository _savedFilters;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public CreateSavedFilterCommandHandler(
        ISavedFilterRepository savedFilters, IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _savedFilters = savedFilters;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<SavedFilterDto> Handle(CreateSavedFilterCommand command, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetCurrentUserId();
        var name = command.Name.Trim();

        if (await _savedFilters.CountActiveAsync(userId, cancellationToken) >= MaxActivePerUser)
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(command.Name), $"You can keep at most {MaxActivePerUser} saved filters. Delete one first."),
            });

        if (await _savedFilters.ActiveNameExistsAsync(userId, name, cancellationToken))
            throw new ConflictException($"You already have a saved filter called '{name}'.");

        var filter = new SavedFilter
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name,
            Page = Enum.Parse<SavedFilterPage>(command.Page, true),
            QueryString = SavedFilterQueryString.Normalise(command.QueryString),
            IsActive = true,
        };

        await _savedFilters.AddAsync(filter, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        command.EntityId = filter.Id;
        command.NewValues = new { filter.Name, Page = filter.Page.ToString(), filter.QueryString };

        return new SavedFilterDto(filter.Id, filter.Name, filter.Page.ToString(), filter.QueryString);
    }
}
