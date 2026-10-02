using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Reports.Dtos;
using CRM.Application.Modules.Reports.Queries.GetDonorsByTypeReport;
using NSubstitute;

namespace CRM.Application.Tests.Modules.Reports.Queries;

public class GetDonorsByTypeReportQueryHandlerTests
{
    private readonly IReportsRepository _reportsMock = Substitute.For<IReportsRepository>();
    private readonly GetDonorsByTypeReportQueryHandler _handler;

    public GetDonorsByTypeReportQueryHandlerTests()
    {
        _handler = new GetDonorsByTypeReportQueryHandler(_reportsMock);
    }

    [Fact]
    public async Task Handle_ReturnsRepositoryResultUnchanged()
    {
        var rows = new List<DonorsByTypeDto>
        {
            new() { DonationType = "Meat", DonorCount = 35 },
            new() { DonationType = "Dairy", DonorCount = 0 }
        };
        _reportsMock.GetDonorsByTypeAsync(Arg.Any<CancellationToken>()).Returns(rows);

        var result = await _handler.Handle(new GetDonorsByTypeReportQuery(), CancellationToken.None);

        Assert.Same(rows, result);
    }

    [Fact]
    public async Task Handle_PassesCancellationTokenThrough()
    {
        using var cts = new CancellationTokenSource();
        CancellationToken captured = default;
        _reportsMock
            .GetDonorsByTypeAsync(Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                captured = ci.ArgAt<CancellationToken>(0);
                return new List<DonorsByTypeDto>();
            });

        await _handler.Handle(new GetDonorsByTypeReportQuery(), cts.Token);

        Assert.Equal(cts.Token, captured);
    }
}
