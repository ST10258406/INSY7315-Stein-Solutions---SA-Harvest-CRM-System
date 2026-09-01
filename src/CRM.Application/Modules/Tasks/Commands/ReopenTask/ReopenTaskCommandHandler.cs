namespace CRM.Application.Modules.Tasks.Commands.ReopenTask;

using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Tasks.Dtos;
using CRM.Domain.Entities;
using FluentValidation.Results;
using MediatR;

public class ReopenTaskCommandHandler : IRequestHandler<ReopenTaskCommand, TaskDto>
{
    private readonly ITaskRepository _tasks;
    private readonly IUnitOfWork _unitOfWork;

    public ReopenTaskCommandHandler(ITaskRepository tasks, IUnitOfWork unitOfWork)
    {
        _tasks = tasks;
        _unitOfWork = unitOfWork;
    }

    public async Task<TaskDto> Handle(ReopenTaskCommand command, CancellationToken cancellationToken)
    {
        var task = await _tasks.GetForUpdateAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(DonorTask), command.Id);

        if (!task.IsCompleted)
            throw new ValidationException(new[] { new ValidationFailure("isCompleted", "Task is not completed.") });

        task.IsCompleted = false;
        task.CompletedAt = null;
        task.CompletedByUserId = null;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        command.EntityId = task.Id;
        command.NewValues = new { task.Id, task.IsCompleted };

        return await _tasks.GetDtoByIdAsync(task.Id, cancellationToken)
            ?? throw new InvalidOperationException(
                $"DonorTask {task.Id} could not be re-read immediately after being reopened.");
    }
}
