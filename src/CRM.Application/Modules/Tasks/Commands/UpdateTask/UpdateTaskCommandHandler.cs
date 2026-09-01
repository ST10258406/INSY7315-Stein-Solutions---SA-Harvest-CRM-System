namespace CRM.Application.Modules.Tasks.Commands.UpdateTask;

using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Tasks.Dtos;
using CRM.Domain.Entities;
using MediatR;

public class UpdateTaskCommandHandler : IRequestHandler<UpdateTaskCommand, TaskDto>
{
    private readonly ITaskRepository _tasks;
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateTaskCommandHandler(ITaskRepository tasks, IUserRepository users, IUnitOfWork unitOfWork)
    {
        _tasks = tasks;
        _users = users;
        _unitOfWork = unitOfWork;
    }

    public async Task<TaskDto> Handle(UpdateTaskCommand command, CancellationToken cancellationToken)
    {
        var task = await _tasks.GetForUpdateAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(DonorTask), command.Id);

        command.OldValues = new { task.Title, task.Description, task.DueDate, task.AssignedToUserId };

        if (command.AssignedToUserId is { } newAssignee
            && newAssignee != task.AssignedToUserId
            && !await _users.ExistsAndActiveAsync(newAssignee, cancellationToken))
        {
            throw new NotFoundException(nameof(User), newAssignee);
        }

        if (command.Title is not null) task.Title = command.Title;
        if (command.Description is not null) task.Description = command.Description;
        if (command.DueDate is { } due)
            task.DueDate = DateTime.SpecifyKind(due.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        if (command.AssignedToUserId is { } assignee) task.AssignedToUserId = assignee;

        // UpdatedAt is handled by UpdatedAtInterceptor; audit_logs by AuditBehaviour.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        command.EntityId = task.Id;
        command.NewValues = new { task.Title, task.Description, task.DueDate, task.AssignedToUserId };

        return await _tasks.GetDtoByIdAsync(task.Id, cancellationToken)
            ?? throw new InvalidOperationException(
                $"DonorTask {task.Id} could not be re-read immediately after being updated.");
    }
}
