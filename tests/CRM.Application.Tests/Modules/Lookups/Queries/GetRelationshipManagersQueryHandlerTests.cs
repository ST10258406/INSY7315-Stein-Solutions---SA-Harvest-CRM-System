using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Donors.Dtos;
using CRM.Application.Modules.Lookups.Queries.GetRelationshipManagers;
using NSubstitute;

namespace CRM.Application.Tests.Modules.Lookups.Queries;

public class GetRelationshipManagersQueryHandlerTests
{
    private readonly IUserRepository _usersMock = Substitute.For<IUserRepository>();
    private readonly GetRelationshipManagersQueryHandler _handler;

    public GetRelationshipManagersQueryHandlerTests()
    {
        _handler = new GetRelationshipManagersQueryHandler(_usersMock);
    }

    [Fact]
    public async Task Handle_DelegatesToUserRepository_WithProcurementRole()
    {
        var expected = new List<RelationshipManagerDto>
        {
            new() { Id = Guid.NewGuid(), FullName = "Jane Doe" }
        };
        _usersMock.GetActiveByRoleAsync("Procurement", Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _handler.Handle(new GetRelationshipManagersQuery(), CancellationToken.None);

        Assert.Same(expected, result);
        await _usersMock.Received(1).GetActiveByRoleAsync("Procurement", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PassesCancellationTokenThrough()
    {
        using var cts = new CancellationTokenSource();
        _usersMock.GetActiveByRoleAsync("Procurement", cts.Token).Returns([]);

        await _handler.Handle(new GetRelationshipManagersQuery(), cts.Token);

        await _usersMock.Received(1).GetActiveByRoleAsync("Procurement", cts.Token);
    }

    [Fact]
    public async Task Handle_EmptyRepositoryResult_ReturnsEmptyList()
    {
        _usersMock.GetActiveByRoleAsync("Procurement", Arg.Any<CancellationToken>()).Returns([]);

        var result = await _handler.Handle(new GetRelationshipManagersQuery(), CancellationToken.None);

        Assert.Empty(result);
    }
}
