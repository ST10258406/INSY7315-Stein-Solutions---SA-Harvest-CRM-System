using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Approvals.Dtos;
using CRM.Application.Modules.Approvals.Queries.GetApprovals;
using CRM.Domain.Enums;
using NSubstitute;

namespace CRM.Application.Tests.Modules.Approvals.Queries;

public class GetApprovalsQueryHandlerTests
{
    private readonly IApprovalRepository _approvalsMock = Substitute.For<IApprovalRepository>();
    private readonly GetApprovalsQueryHandler _handler;

    private (ApprovalStatus status, int page, int pageSize)? _captured;

    public GetApprovalsQueryHandlerTests()
    {
        _approvalsMock
            .GetApprovalsAsync(Arg.Any<ApprovalStatus>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                _captured = (ci.ArgAt<ApprovalStatus>(0), ci.ArgAt<int>(1), ci.ArgAt<int>(2));
                return (new List<ApprovalDto>(), 0);
            });

        _handler = new GetApprovalsQueryHandler(_approvalsMock);
    }

    [Fact]
    public async Task Handle_DefaultsToPending()
    {
        await _handler.Handle(new GetApprovalsQuery(), CancellationToken.None);

        Assert.Equal(ApprovalStatus.Pending, _captured!.Value.status);
    }

    [Theory]
    [InlineData("approved", ApprovalStatus.Approved)]
    [InlineData("Rejected", ApprovalStatus.Rejected)]
    public async Task Handle_ParsesStatusCaseInsensitively(string raw, ApprovalStatus expected)
    {
        await _handler.Handle(new GetApprovalsQuery { Status = raw }, CancellationToken.None);

        Assert.Equal(expected, _captured!.Value.status);
    }

    [Fact]
    public async Task Handle_WrapsResultInEnvelope()
    {
        var items = new List<ApprovalDto> { new() { Id = Guid.NewGuid(), Status = "Pending" } };
        _approvalsMock
            .GetApprovalsAsync(Arg.Any<ApprovalStatus>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((items, 30));

        var result = await _handler.Handle(new GetApprovalsQuery { Page = 2, PageSize = 10 }, CancellationToken.None);

        Assert.Same(items[0], result.Data.Single());
        Assert.Equal(2, result.Pagination.Page);
        Assert.Equal(30, result.Pagination.TotalCount);
        Assert.Equal(3, result.Pagination.TotalPages);
    }
}
