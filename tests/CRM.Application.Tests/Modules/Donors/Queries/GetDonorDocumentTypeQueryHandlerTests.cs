using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Donors.Queries.GetDonorDocumentType;
using CRM.Domain.Entities;
using CRM.Domain.Enums;
using MockQueryable.NSubstitute;
using NSubstitute;

namespace CRM.Application.Tests.Modules.Donors.Queries;

public class GetDonorDocumentTypeQueryHandlerTests
{
    private readonly IApplicationDbContext _contextMock;
    private readonly GetDonorDocumentTypeQueryHandler _handler;

    private readonly List<DonorDocument> _documents = [];

    public GetDonorDocumentTypeQueryHandlerTests()
    {
        _contextMock = Substitute.For<IApplicationDbContext>();

        var documentsDbSet = _documents.BuildMockDbSet();
        _contextMock.DonorDocuments.Returns(documentsDbSet);

        _handler = new GetDonorDocumentTypeQueryHandler(_contextMock);
    }

    private DonorDocument AddDocument(DocumentType type, Guid donorId)
    {
        var document = new DonorDocument
        {
            Id = Guid.NewGuid(),
            DonorId = donorId,
            DocumentType = type,
            FileName = "file.pdf",
            BlobStoragePath = "donors/x/y/blob.pdf",
            IsActive = true
        };
        _documents.Add(document);
        return document;
    }

    [Fact]
    public async Task Handle_ExistingDocument_ReturnsItsDocumentType()
    {
        var donorId = Guid.NewGuid();
        var document = AddDocument(DocumentType.BBBEECertificate, donorId);

        var result = await _handler.Handle(
            new GetDonorDocumentTypeQuery { DonorId = donorId, DocumentId = document.Id },
            CancellationToken.None);

        Assert.Equal(DocumentType.BBBEECertificate, result);
    }

    [Fact]
    public async Task Handle_NonExistentDocument_ThrowsNotFoundException()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(
            new GetDonorDocumentTypeQuery { DonorId = Guid.NewGuid(), DocumentId = Guid.NewGuid() },
            CancellationToken.None));
    }

    [Fact]
    public async Task Handle_DocumentBelongsToDifferentDonor_ThrowsNotFoundException()
    {
        var document = AddDocument(DocumentType.Signature, Guid.NewGuid());

        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(
            new GetDonorDocumentTypeQuery { DonorId = Guid.NewGuid(), DocumentId = document.Id },
            CancellationToken.None));
    }
}
