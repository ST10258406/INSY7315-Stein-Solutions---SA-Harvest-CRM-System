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
}
