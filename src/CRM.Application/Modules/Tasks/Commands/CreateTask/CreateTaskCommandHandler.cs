namespace CRM.Application.Modules.Tasks.Commands.CreateTask;

using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Tasks.Dtos;
using CRM.Domain.Entities;
using CRM.Domain.Enums;
using MediatR;

/// <summary>
/// Creates a task for a donor, assigns it to a staff member and fires a
/// <see cref="NotificationType.TaskAssigned"/> notification to that assignee.
/// </summary>
public class CreateTaskCommandHandler : IRequestHandler<CreateTaskCommand, TaskDto>
{
    private readonly IDonorRepository _donors;
    private readonly IUserRepository _users;
    private readonly ITaskRepository _tasks;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;
    private readonly ICurrentUserService _currentUserService;

    public CreateTaskCommandHandler(
        IDonorRepository donors,
        IUserRepository users,
        ITaskRepository tasks,
        IUnitOfWork unitOfWork,
        INotificationService notificationService,
        ICurrentUserService currentUserService)
    {
        _donors = donors;
        _users = users;
        _tasks = tasks;
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _currentUserService = currentUserService;
    }

    public async Task<TaskDto> Handle(CreateTaskCommand command, CancellationToken cancellationToken)
    {
        if (!await _donors.ExistsAsync(command.DonorId, cancellationToken))
            throw new NotFoundException(nameof(Donor), command.DonorId);

        if (!await _users.ExistsAndActiveAsync(command.AssignedToUserId, cancellationToken))
            throw new NotFoundException(nameof(User), command.AssignedToUserId);

        var task = new DonorTask
        {
            Id = Guid.NewGuid(),
            DonorId = command.DonorId,
            Title = command.Title,
            Description = command.Description,
            // Request carries a calendar date; store it as midnight UTC.
            DueDate = DateTime.SpecifyKind(command.DueDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc),
            IsCompleted = false,
            AssignedToUserId = command.AssignedToUserId,
            CreatedByUserId = _currentUserService.GetCurrentUserId()
        };
        await _tasks.AddAsync(task, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        command.EntityId = task.Id;
        command.NewValues = new { task.Id, task.DonorId, task.Title, task.AssignedToUserId, task.DueDate };

        await _notificationService.CreateAsync(
            task.AssignedToUserId,
            "New task assigned",
            $"You have been assigned a task due {command.DueDate:yyyy-MM-dd}: {task.Title}",
            NotificationType.TaskAssigned,
            task.Id,
            nameof(DonorTask),
            cancellationToken);

        // Re-read through the shared projection — one source of truth for TaskDto.
        return await _tasks.GetDtoByIdAsync(task.Id, cancellationToken)
            ?? throw new InvalidOperationException(
                $"DonorTask {task.Id} could not be re-read immediately after being created.");
    }
}
