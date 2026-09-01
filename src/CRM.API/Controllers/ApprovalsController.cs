using CRM.Application.Modules.Approvals.Commands.ApproveDonor;
using CRM.Application.Modules.Approvals.Commands.RejectDonor;
using CRM.Application.Modules.Approvals.Dtos;
using CRM.Application.Modules.Approvals.Queries.GetApprovals;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/v1/approvals")]
[Authorize(Policy = "AdminOrAbove")] // the whole approval pipeline is Admin-only
public class ApprovalsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ApprovalsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetApprovals([FromQuery] GetApprovalsQuery query)
    {
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id)
    {
        await _mediator.Send(new ApproveDonorCommand { ApprovalId = id });
        return NoContent();
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectDonorRequest request)
    {
        await _mediator.Send(new RejectDonorCommand { ApprovalId = id, RejectionReason = request.RejectionReason });
        return NoContent();
    }
}
