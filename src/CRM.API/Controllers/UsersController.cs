using CRM.Application.Common.Models;
using CRM.Application.Modules.Users.Commands.ChangeUserRole;
using CRM.Application.Modules.Users.Commands.CreateUser;
using CRM.Application.Modules.Users.Commands.SetUserActiveStatus;
using CRM.Application.Modules.Users.Commands.UpdateUser;
using CRM.Application.Modules.Users.Dtos;
using CRM.Application.Modules.Users.Queries.GetUsers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/v1/users")]
[Authorize(Policy = "AdminOrAbove")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResult<UserListItemDto>>> GetUsers([FromQuery] GetUsersQuery query)
    {
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        var result = await _mediator.Send(new CreateUserCommand { Request = request });
        return StatusCode(201, new { data = result });
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserRequest request)
    {
        var result = await _mediator.Send(new UpdateUserCommand { Id = id, Request = request });
        return Ok(new { data = result });
    }

    // Role changes are a step up in privilege from the rest of user management, matching
    // the "requires SuperAdmin permission" warning shown on the Change Role dialog.
    [HttpPatch("{id:guid}/role")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> ChangeUserRole(Guid id, [FromBody] ChangeUserRoleRequest request)
    {
        var result = await _mediator.Send(new ChangeUserRoleCommand { Id = id, Request = request });
        return Ok(new { data = result });
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> SetUserActiveStatus(Guid id, [FromBody] SetUserActiveStatusRequest request)
    {
        var result = await _mediator.Send(new SetUserActiveStatusCommand { Id = id, IsActive = request.IsActive });
        return Ok(new { data = result });
    }
}
