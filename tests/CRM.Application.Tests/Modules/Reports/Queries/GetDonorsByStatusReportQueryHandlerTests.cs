using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Reports.Dtos;
using CRM.Application.Modules.Reports.Queries.GetDonorsByStatusReport;
using NSubstitute;

namespace CRM.Application.Tests.Modules.Reports.Queries;

public class GetDonorsByStatusReportQueryHandlerTests
{
    private readonly IReportsRepository _reportsMock = Substitute.For<IReportsRepository>();
    private readonly GetDonorsByStatusReportQueryHandler _handler;

    public GetDonorsByStatusReportQueryHandlerTests()
    {
        _handler = new GetDonorsByStatusReportQueryHandler(_reportsMock);
    }

    [Fact]
    public async Task Handle_ReturnsRepositoryResultUnchanged()
    {
        var rows = new List<DonorsByStatusDto>
        {
            new() { Status = "Active", DonorCount = 87 },
            new() { Status = "PendingReview", DonorCount = 0 }
        };
        _reportsMock.GetDonorsByStatusAsync(Arg.Any<CancellationToken>()).Returns(rows);

        var result = await _handler.Handle(new GetDonorsByStatusReportQuery(), CancellationToken.None);

        Assert.Same(rows, result);
    }

    [Fact]
    public async Task Handle_PassesCancellationTokenThrough()
    {
        using var cts = new CancellationTokenSource();
        CancellationToken captured = default;
        _reportsMock
            .GetDonorsByStatusAsync(Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                captured = ci.ArgAt<CancellationToken>(0);
                return new List<DonorsByStatusDto>();
            });

        await _handler.Handle(new GetDonorsByStatusReportQuery(), cts.Token);

        Assert.Equal(cts.Token, captured);
    }
}
