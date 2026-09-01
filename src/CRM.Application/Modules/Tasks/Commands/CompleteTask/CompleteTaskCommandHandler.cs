namespace CRM.Application.Modules.Tasks.Commands.CompleteTask;

using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Tasks.Dtos;
using CRM.Domain.Entities;
using FluentValidation.Results;
using MediatR;

public class CompleteTaskCommandHandler : IRequestHandler<CompleteTaskCommand, TaskDto>
{
    private readonly ITaskRepository _tasks;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public CompleteTaskCommandHandler(
        ITaskRepository tasks, IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _tasks = tasks;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<TaskDto> Handle(CompleteTaskCommand command, CancellationToken cancellationToken)
    {
        var task = await _tasks.GetForUpdateAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(DonorTask), command.Id);

        if (task.IsCompleted)
            throw new ValidationException(new[] { new ValidationFailure("isCompleted", "Task is already complete.") });

        task.IsCompleted = true;
        task.CompletedAt = DateTime.UtcNow;
        task.CompletedByUserId = _currentUserService.GetCurrentUserId();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        command.EntityId = task.Id;
        command.NewValues = new { task.Id, task.IsCompleted, task.CompletedAt, task.CompletedByUserId };

        return await _tasks.GetDtoByIdAsync(task.Id, cancellationToken)
            ?? throw new InvalidOperationException(
                $"DonorTask {task.Id} could not be re-read immediately after being completed.");
    }
}
