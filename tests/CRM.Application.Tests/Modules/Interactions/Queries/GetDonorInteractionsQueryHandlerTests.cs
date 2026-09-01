using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Interactions.Dtos;
using CRM.Application.Modules.Interactions.Queries.GetDonorInteractions;
using CRM.Domain.Enums;
using NSubstitute;

namespace CRM.Application.Tests.Modules.Interactions.Queries;

public class GetDonorInteractionsQueryHandlerTests
{
    private readonly IDonorRepository _donorsMock = Substitute.For<IDonorRepository>();
    private readonly IInteractionLogRepository _interactionsMock = Substitute.For<IInteractionLogRepository>();
    private readonly IBlobStorageService _blobMock = Substitute.For<IBlobStorageService>();
    private readonly GetDonorInteractionsQueryHandler _handler;

    private (Guid donorId, InteractionType? type, int page, int pageSize)? _captured;

    public GetDonorInteractionsQueryHandlerTests()
    {
        _interactionsMock
            .GetByDonorAsync(Arg.Any<Guid>(), Arg.Any<InteractionType?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                _captured = (ci.ArgAt<Guid>(0), ci.ArgAt<InteractionType?>(1), ci.ArgAt<int>(2), ci.ArgAt<int>(3));
                return (new List<InteractionLogDto>(), 0);
            });

        _blobMock.GenerateSasUrlAsync(Arg.Any<string>(), Arg.Any<TimeSpan>())
            .Returns(ci => Task.FromResult($"https://blob.test/{ci.ArgAt<string>(0)}?sig=abc"));

        _handler = new GetDonorInteractionsQueryHandler(_donorsMock, _interactionsMock, _blobMock);
    }

    private Guid SetupExistingDonor()
    {
        var donorId = Guid.NewGuid();
        _donorsMock.ExistsAsync(donorId, Arg.Any<CancellationToken>()).Returns(true);
        return donorId;
    }

    [Fact]
    public async Task Handle_UnknownDonor_ThrowsNotFound()
    {
        _donorsMock.ExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(new GetDonorInteractionsQuery { DonorId = Guid.NewGuid() }, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_PassesDonorFilterPageAndParsedTypeToRepository()
    {
        var donorId = SetupExistingDonor();

        await _handler.Handle(new GetDonorInteractionsQuery
        {
            DonorId = donorId,
            InteractionType = "email",
            Page = 3,
            PageSize = 15
        }, CancellationToken.None);

        Assert.Equal((donorId, InteractionType.Email, 3, 15), _captured);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_NoInteractionTypeFilter_PassesNullToRepository(string? raw)
    {
        var donorId = SetupExistingDonor();

        await _handler.Handle(new GetDonorInteractionsQuery { DonorId = donorId, InteractionType = raw }, CancellationToken.None);

        Assert.Null(_captured!.Value.type);
    }

    [Fact]
    public async Task Handle_WrapsItemsInPaginationEnvelope()
    {
        var donorId = SetupExistingDonor();
        var items = new List<InteractionLogDto> { new() { Id = Guid.NewGuid(), Body = "hi" } };
        _interactionsMock
            .GetByDonorAsync(donorId, Arg.Any<InteractionType?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((items, 42));

        var result = await _handler.Handle(
            new GetDonorInteractionsQuery { DonorId = donorId, Page = 2, PageSize = 20 }, CancellationToken.None);

        Assert.Same(items[0], result.Data.Single());
        Assert.Equal(2, result.Pagination.Page);
        Assert.Equal(42, result.Pagination.TotalCount);
        Assert.Equal(3, result.Pagination.TotalPages);
    }

    [Fact]
    public async Task Handle_SwapsPresentEmailAttachmentPathForShortLivedSasUrl()
    {
        var donorId = SetupExistingDonor();
        var withAttachment = new InteractionLogDto { Id = Guid.NewGuid(), EmailAttachmentUrl = "donors/x/email/att.pdf" };
        var withoutAttachment = new InteractionLogDto { Id = Guid.NewGuid(), EmailAttachmentUrl = null };
        _interactionsMock
            .GetByDonorAsync(donorId, Arg.Any<InteractionType?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((new List<InteractionLogDto> { withAttachment, withoutAttachment }, 2));

        var result = await _handler.Handle(new GetDonorInteractionsQuery { DonorId = donorId }, CancellationToken.None);

        Assert.StartsWith("https://blob.test/", result.Data[0].EmailAttachmentUrl);
        Assert.Null(result.Data[1].EmailAttachmentUrl);
        await _blobMock.Received(1).GenerateSasUrlAsync("donors/x/email/att.pdf", TimeSpan.FromMinutes(15));
    }
}
