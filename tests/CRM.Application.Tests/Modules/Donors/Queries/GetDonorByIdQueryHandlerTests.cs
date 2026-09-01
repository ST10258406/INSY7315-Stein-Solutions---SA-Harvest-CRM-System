using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Donors.Dtos;
using CRM.Application.Modules.Donors.Queries.GetDonorById;
using NSubstitute;

namespace CRM.Application.Tests.Modules.Donors.Queries;

// The DonorDetailDto projection itself is exercised against a real database in
// CRM.Infrastructure.Tests/Persistence/Repositories/DonorRepositoryTests — it is now
// a repository concern. What's left for the handler is delegation and the not-found path.
public class GetDonorByIdQueryHandlerTests
{
    private readonly IDonorRepository _donorsMock = Substitute.For<IDonorRepository>();
    private readonly GetDonorByIdQueryHandler _handler;

    public GetDonorByIdQueryHandlerTests()
    {
        _handler = new GetDonorByIdQueryHandler(_donorsMock);
    }

    [Fact]
    public async Task Handle_ExistingDonor_ReturnsRepositoryResult()
    {
        var id = Guid.NewGuid();
        var expected = new DonorDetailDto { Id = id };
        _donorsMock.GetDetailByIdAsync(id, Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _handler.Handle(new GetDonorByIdQuery(id), CancellationToken.None);

        Assert.Same(expected, result);
        await _donorsMock.Received(1).GetDetailByIdAsync(id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NonExistentId_ThrowsNotFoundException()
    {
        _donorsMock.GetDetailByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((DonorDetailDto?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(new GetDonorByIdQuery(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_PassesCancellationTokenThrough()
    {
        using var cts = new CancellationTokenSource();
        var id = Guid.NewGuid();
        _donorsMock.GetDetailByIdAsync(id, cts.Token).Returns(new DonorDetailDto { Id = id });

        await _handler.Handle(new GetDonorByIdQuery(id), cts.Token);

        await _donorsMock.Received(1).GetDetailByIdAsync(id, cts.Token);
    }
}
