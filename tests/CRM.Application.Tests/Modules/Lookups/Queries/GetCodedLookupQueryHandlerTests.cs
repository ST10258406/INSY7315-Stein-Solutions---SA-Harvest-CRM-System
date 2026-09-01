using CRM.Application.Common.Interfaces;
using CRM.Application.Common.Models;
using CRM.Application.Modules.Lookups.Queries.GetCodedLookup;
using NSubstitute;

namespace CRM.Application.Tests.Modules.Lookups.Queries;

public class GetCodedLookupQueryHandlerTests
{
    private readonly ILookupRepository _lookupsMock = Substitute.For<ILookupRepository>();
    private readonly GetCodedLookupQueryHandler _handler;

    public GetCodedLookupQueryHandlerTests()
    {
        _handler = new GetCodedLookupQueryHandler(_lookupsMock);
    }

    [Theory]
    [InlineData(CodedLookupType.OperationalRegions)]
    [InlineData(CodedLookupType.Provinces)]
    public async Task Handle_DelegatesRequestedTypeToRepository(CodedLookupType type)
    {
        var expected = new List<CodedLookupDto> { new() { Id = 1, Code = "GP", Name = "Gauteng" } };
        _lookupsMock.GetActiveCodedAsync(type, Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _handler.Handle(new GetCodedLookupQuery(type), CancellationToken.None);

        Assert.Same(expected, result);
        await _lookupsMock.Received(1).GetActiveCodedAsync(type, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PassesCancellationTokenThrough()
    {
        using var cts = new CancellationTokenSource();
        _lookupsMock.GetActiveCodedAsync(CodedLookupType.Provinces, cts.Token).Returns([]);

        await _handler.Handle(new GetCodedLookupQuery(CodedLookupType.Provinces), cts.Token);

        await _lookupsMock.Received(1).GetActiveCodedAsync(CodedLookupType.Provinces, cts.Token);
    }

    [Fact]
    public async Task Handle_EmptyRepositoryResult_ReturnsEmptyList()
    {
        _lookupsMock.GetActiveCodedAsync(CodedLookupType.Provinces, Arg.Any<CancellationToken>()).Returns([]);

        var result = await _handler.Handle(new GetCodedLookupQuery(CodedLookupType.Provinces), CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task Handle_PropagatesRepositoryException()
    {
        _lookupsMock.GetActiveCodedAsync(Arg.Any<CodedLookupType>(), Arg.Any<CancellationToken>())
            .Returns<List<CodedLookupDto>>(_ => throw new ArgumentOutOfRangeException("type"));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _handler.Handle(new GetCodedLookupQuery((CodedLookupType)999), CancellationToken.None));
    }
}
