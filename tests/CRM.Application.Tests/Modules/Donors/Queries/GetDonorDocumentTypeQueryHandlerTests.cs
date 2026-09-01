using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Donors.Queries.GetDonorDocumentType;
using CRM.Domain.Enums;
using NSubstitute;

namespace CRM.Application.Tests.Modules.Donors.Queries;

public class GetDonorDocumentTypeQueryHandlerTests
{
    private readonly IDonorDocumentRepository _documentsMock = Substitute.For<IDonorDocumentRepository>();
    private readonly GetDonorDocumentTypeQueryHandler _handler;

    public GetDonorDocumentTypeQueryHandlerTests()
    {
        _handler = new GetDonorDocumentTypeQueryHandler(_documentsMock);
    }

    [Fact]
    public async Task Handle_ExistingDocument_ReturnsItsDocumentType()
    {
        var donorId = Guid.NewGuid();
        var documentId = Guid.NewGuid();

        _documentsMock.GetDocumentTypeAsync(donorId, documentId, Arg.Any<CancellationToken>())
            .Returns(DocumentType.BBBEECertificate);

        var result = await _handler.Handle(
            new GetDonorDocumentTypeQuery { DonorId = donorId, DocumentId = documentId },
            CancellationToken.None);

        Assert.Equal(DocumentType.BBBEECertificate, result);
    }

    [Fact]
    public async Task Handle_NonExistentDocument_ThrowsNotFoundException()
    {
        _documentsMock.GetDocumentTypeAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((DocumentType?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(
            new GetDonorDocumentTypeQuery { DonorId = Guid.NewGuid(), DocumentId = Guid.NewGuid() },
            CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ScopesLookupToTheRequestedDonor()
    {
        // The donor-scoping of the lookup is what stops a caller from probing another
        // donor's document ids; assert the donor id is actually passed down.
        var donorId = Guid.NewGuid();
        var documentId = Guid.NewGuid();

        _documentsMock.GetDocumentTypeAsync(donorId, documentId, Arg.Any<CancellationToken>())
            .Returns(DocumentType.Signature);

        await _handler.Handle(
            new GetDonorDocumentTypeQuery { DonorId = donorId, DocumentId = documentId },
            CancellationToken.None);

        await _documentsMock.Received(1).GetDocumentTypeAsync(donorId, documentId, Arg.Any<CancellationToken>());
    }
}
