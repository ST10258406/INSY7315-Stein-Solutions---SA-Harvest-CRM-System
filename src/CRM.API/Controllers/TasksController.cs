using CRM.Application.Modules.Tasks.Commands.CompleteTask;
using CRM.Application.Modules.Tasks.Commands.ReopenTask;
using CRM.Application.Modules.Tasks.Commands.UpdateTask;
using CRM.Application.Modules.Tasks.Dtos;
using CRM.Application.Modules.Tasks.Queries.GetMyTasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/v1/tasks")]
public class TasksController : ControllerBase
{
    private readonly IMediator _mediator;

    public TasksController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [Authorize] // any authenticated user — "My Tasks" is scoped to the caller
    public async Task<IActionResult> GetMyTasks([FromQuery] GetMyTasksQuery query)
    {
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpPatch("{id:guid}")]
    [Authorize(Policy = "ProcurementOrAbove")]
    public async Task<IActionResult> UpdateTask(Guid id, [FromBody] UpdateTaskRequest request)
    {
        var result = await _mediator.Send(new UpdateTaskCommand
        {
            Id = id,
            Title = request.Title,
            Description = request.Description,
            DueDate = request.DueDate,
            AssignedToUserId = request.AssignedToUserId
        });
        return Ok(new { data = result });
    }

    [HttpPost("{id:guid}/complete")]
    [Authorize] // ANY authenticated user may complete a task — not restricted to the assignee
    public async Task<IActionResult> CompleteTask(Guid id)
    {
        var result = await _mediator.Send(new CompleteTaskCommand { Id = id });
        return Ok(new { data = result });
    }

    [HttpPost("{id:guid}/reopen")]
    [Authorize(Policy = "ProcurementOrAbove")] // asymmetric with complete — reopen is Procurement-only
    public async Task<IActionResult> ReopenTask(Guid id)
    {
        var result = await _mediator.Send(new ReopenTaskCommand { Id = id });
        return Ok(new { data = result });
    }
}
