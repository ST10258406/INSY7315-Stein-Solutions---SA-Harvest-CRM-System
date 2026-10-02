using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Reports.Dtos;
using CRM.Application.Modules.Reports.Queries.GetDonorsByRegionReport;
using NSubstitute;

namespace CRM.Application.Tests.Modules.Reports.Queries;

public class GetDonorsByRegionReportQueryHandlerTests
{
    private readonly IReportsRepository _reportsMock = Substitute.For<IReportsRepository>();
    private readonly GetDonorsByRegionReportQueryHandler _handler;

    public GetDonorsByRegionReportQueryHandlerTests()
    {
        _handler = new GetDonorsByRegionReportQueryHandler(_reportsMock);
    }

    [Fact]
    public async Task Handle_ReturnsRepositoryResultUnchanged()
    {
        var rows = new List<DonorsByRegionDto>
        {
            new() { Region = "JHB", RegionName = "Johannesburg", DonorCount = 42 },
            new() { Region = "CPT", RegionName = "Cape Town", DonorCount = 0 }
        };
        _reportsMock.GetDonorsByRegionAsync(Arg.Any<CancellationToken>()).Returns(rows);

        var result = await _handler.Handle(new GetDonorsByRegionReportQuery(), CancellationToken.None);

        Assert.Same(rows, result);
    }

    [Fact]
    public async Task Handle_PassesCancellationTokenThrough()
    {
        using var cts = new CancellationTokenSource();
        CancellationToken captured = default;
        _reportsMock
            .GetDonorsByRegionAsync(Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                captured = ci.ArgAt<CancellationToken>(0);
                return new List<DonorsByRegionDto>();
            });

        await _handler.Handle(new GetDonorsByRegionReportQuery(), cts.Token);

        Assert.Equal(cts.Token, captured);
    }
}
