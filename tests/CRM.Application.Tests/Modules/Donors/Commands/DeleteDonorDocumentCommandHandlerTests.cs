using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Donors.Commands.DeleteDonorDocument;
using CRM.Domain.Entities;
using CRM.Domain.Enums;
using MockQueryable.NSubstitute;
using NSubstitute;

namespace CRM.Application.Tests.Modules.Donors.Commands;

public class DeleteDonorDocumentCommandHandlerTests
{
    private readonly IApplicationDbContext _contextMock;
    private readonly DeleteDonorDocumentCommandHandler _handler;

    private readonly List<DonorDocument> _documents = [];

    public DeleteDonorDocumentCommandHandlerTests()
    {
        _contextMock = Substitute.For<IApplicationDbContext>();

        var documentsDbSet = _documents.BuildMockDbSet();
        _contextMock.DonorDocuments.Returns(documentsDbSet);

        _handler = new DeleteDonorDocumentCommandHandler(_contextMock);
    }

    private DonorDocument AddDocument(Guid donorId, bool isActive = true)
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
        _documents.Add(document);
        return document;
    }

    [Fact]
    public async Task Handle_ExistingActiveDocument_FlipsIsActiveToFalse()
    {
        var donorId = Guid.NewGuid();
        var document = AddDocument(donorId);
        var command = new DeleteDonorDocumentCommand { DonorId = donorId, DocumentId = document.Id };

        await _handler.Handle(command, CancellationToken.None);

        Assert.False(document.IsActive);
        await _contextMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // No "never calls blob deletion" test here: DeleteDonorDocumentCommandHandler
    // doesn't take an IBlobStorageService at all, which is a stronger guarantee
    // than a mock assertion — there's no dependency through which it could call one.

    [Fact]
    public async Task Handle_NonExistentDocument_ThrowsNotFoundException()
    {
        var command = new DeleteDonorDocumentCommand { DonorId = Guid.NewGuid(), DocumentId = Guid.NewGuid() };

        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_DocumentBelongsToDifferentDonor_ThrowsNotFoundException()
    {
        var document = AddDocument(Guid.NewGuid());
        var command = new DeleteDonorDocumentCommand { DonorId = Guid.NewGuid(), DocumentId = document.Id };

        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(command, CancellationToken.None));
        Assert.True(document.IsActive);
    }

    [Fact]
    public async Task Handle_DocumentStaysInCollection_RowIsNeverRemoved()
    {
        var donorId = Guid.NewGuid();
        var document = AddDocument(donorId);
        var command = new DeleteDonorDocumentCommand { DonorId = donorId, DocumentId = document.Id };

        await _handler.Handle(command, CancellationToken.None);

        Assert.Single(_documents);
        Assert.Contains(document, _documents);
        _contextMock.DonorDocuments.DidNotReceive().Remove(Arg.Any<DonorDocument>());
    }
}
