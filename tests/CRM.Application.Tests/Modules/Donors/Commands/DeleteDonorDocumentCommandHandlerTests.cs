using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Donors.Commands.DeleteDonorDocument;
using CRM.Domain.Entities;
using CRM.Domain.Enums;
using NSubstitute;

namespace CRM.Application.Tests.Modules.Donors.Commands;

public class DeleteDonorDocumentCommandHandlerTests
{
    private readonly IDonorDocumentRepository _documentsMock = Substitute.For<IDonorDocumentRepository>();
    private readonly IUnitOfWork _unitOfWorkMock = Substitute.For<IUnitOfWork>();
    private readonly DeleteDonorDocumentCommandHandler _handler;

    public DeleteDonorDocumentCommandHandlerTests()
    {
        _handler = new DeleteDonorDocumentCommandHandler(_documentsMock, _unitOfWorkMock);
    }

    private DonorDocument SetupDocument(Guid donorId, bool isActive = true)
    {
        var document = new DonorDocument
        {
            Id = Guid.NewGuid(),
            DonorId = donorId,
            DocumentType = DocumentType.BBBEECertificate,
            FileName = "cert.pdf",
            BlobStoragePath = $"donors/{donorId}/BBBEECertificate/cert.pdf",
            IsActive = isActive
        };

        _documentsMock.GetForMutationAsync(donorId, document.Id, Arg.Any<CancellationToken>())
            .Returns(document);

        return document;
    }

    [Fact]
    public async Task Handle_ExistingActiveDocument_FlipsIsActiveToFalse()
    {
        var donorId = Guid.NewGuid();
        var document = SetupDocument(donorId);
        var command = new DeleteDonorDocumentCommand { DonorId = donorId, DocumentId = document.Id };

        await _handler.Handle(command, CancellationToken.None);

        Assert.False(document.IsActive);
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // No "never calls blob deletion" test here: DeleteDonorDocumentCommandHandler
    // doesn't take an IBlobStorageService at all, which is a stronger guarantee
    // than a mock assertion — there's no dependency through which it could call one.
    // Likewise, the handler has no way to hard-delete: IDonorDocumentRepository
    // exposes no Remove/Delete method at all.

    [Fact]
    public async Task Handle_NonExistentDocument_ThrowsNotFoundException()
    {
        _documentsMock.GetForMutationAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((DonorDocument?)null);

        var command = new DeleteDonorDocumentCommand { DonorId = Guid.NewGuid(), DocumentId = Guid.NewGuid() };

        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(command, CancellationToken.None));
        await _unitOfWorkMock.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DocumentBelongsToDifferentDonor_ThrowsNotFoundException()
    {
        // The donor-scoped lookup returns null for a mismatched donor id — the repository
        // filters on both ids, so an unrelated donor id never resolves the document.
        var document = SetupDocument(Guid.NewGuid());
        var command = new DeleteDonorDocumentCommand { DonorId = Guid.NewGuid(), DocumentId = document.Id };

        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(command, CancellationToken.None));
        Assert.True(document.IsActive);
    }

    [Fact]
    public async Task Handle_ScopesLookupToTheRequestedDonor()
    {
        var donorId = Guid.NewGuid();
        var document = SetupDocument(donorId);

        await _handler.Handle(
            new DeleteDonorDocumentCommand { DonorId = donorId, DocumentId = document.Id },
            CancellationToken.None);

        await _documentsMock.Received(1).GetForMutationAsync(donorId, document.Id, Arg.Any<CancellationToken>());
    }
}
