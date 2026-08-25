using CRM.Application.Common.Interfaces;
using CRM.Application.Common.Models;
using CRM.Application.Modules.Lookups.Queries.GetLookup;
using NSubstitute;

namespace CRM.Application.Tests.Modules.Lookups.Queries;

public class GetLookupQueryHandlerTests
{
    private readonly ILookupRepository _lookupsMock = Substitute.For<ILookupRepository>();
    private readonly GetLookupQueryHandler _handler;

    public GetLookupQueryHandlerTests()
    {
        _handler = new GetLookupQueryHandler(_lookupsMock);
    }

    [Theory]
    [InlineData(LookupType.CompanyTypes)]
    [InlineData(LookupType.EntityTypes)]
    [InlineData(LookupType.DonationTypes)]
    [InlineData(LookupType.DonationFrequencies)]
    [InlineData(LookupType.BbbeeStatuses)]
    public async Task Handle_DelegatesRequestedTypeToRepository(LookupType type)
    {
        var expected = new List<LookupDto> { new() { Id = 1, Name = "Manufacturer" } };
        _lookupsMock.GetActiveAsync(type, Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _handler.Handle(new GetLookupQuery(type), CancellationToken.None);

        Assert.Same(expected, result);
        await _lookupsMock.Received(1).GetActiveAsync(type, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PassesCancellationTokenThrough()
    {
        using var cts = new CancellationTokenSource();
        _lookupsMock.GetActiveAsync(LookupType.CompanyTypes, cts.Token).Returns([]);

        await _handler.Handle(new GetLookupQuery(LookupType.CompanyTypes), cts.Token);

        await _lookupsMock.Received(1).GetActiveAsync(LookupType.CompanyTypes, cts.Token);
    }

    [Fact]
    public async Task Handle_EmptyRepositoryResult_ReturnsEmptyList()
    {
        _lookupsMock.GetActiveAsync(LookupType.CompanyTypes, Arg.Any<CancellationToken>()).Returns([]);

        var result = await _handler.Handle(new GetLookupQuery(LookupType.CompanyTypes), CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task Handle_PropagatesRepositoryException()
    {
        _lookupsMock.GetActiveAsync(Arg.Any<LookupType>(), Arg.Any<CancellationToken>())
            .Returns<List<LookupDto>>(_ => throw new ArgumentOutOfRangeException("type"));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _handler.Handle(new GetLookupQuery((LookupType)999), CancellationToken.None));
    }
}
