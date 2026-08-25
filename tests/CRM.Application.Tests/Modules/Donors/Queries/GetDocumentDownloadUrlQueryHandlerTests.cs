using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Donors.Queries.GetDocumentDownloadUrl;
using CRM.Domain.Entities;
using CRM.Domain.Enums;
using MockQueryable.NSubstitute;
using NSubstitute;

namespace CRM.Application.Tests.Modules.Donors.Queries;

public class GetDocumentDownloadUrlQueryHandlerTests
{
    private readonly IApplicationDbContext _contextMock;
    private readonly IBlobStorageService _blobStorageMock;
    private readonly GetDocumentDownloadUrlQueryHandler _handler;

    private readonly List<DonorDocument> _documents = [];

    public GetDocumentDownloadUrlQueryHandlerTests()
    {
        _contextMock = Substitute.For<IApplicationDbContext>();
        _blobStorageMock = Substitute.For<IBlobStorageService>();

        _blobStorageMock
            .GenerateSasUrlAsync(Arg.Any<string>(), Arg.Any<TimeSpan>())
            .Returns(ci => Task.FromResult("https://storage.blob.core.windows.net/" + ci.ArgAt<string>(0) + "?sv=sas"));

        var documentsDbSet = _documents.BuildMockDbSet();
        _contextMock.DonorDocuments.Returns(documentsDbSet);

        _handler = new GetDocumentDownloadUrlQueryHandler(_contextMock, _blobStorageMock);
    }

    private DonorDocument AddDocument(DocumentType type, Guid? donorId = null)
    {
        var document = new DonorDocument
        {
            Id = Guid.NewGuid(),
            DonorId = donorId ?? Guid.NewGuid(),
            DocumentType = type,
            FileName = "file.pdf",
            BlobStoragePath = $"donors/{Guid.NewGuid()}/{type}/blob.pdf",
            IsActive = true
        };
        _documents.Add(document);
        return document;
    }

    [Fact]
    public async Task Handle_ExistingDocument_ReturnsUrl()
    {
        var document = AddDocument(DocumentType.Signature);

        var result = await _handler.Handle(
            new GetDocumentDownloadUrlQuery { DonorId = document.DonorId, DocumentId = document.Id },
            CancellationToken.None);

        Assert.False(string.IsNullOrEmpty(result.DownloadUrl));
        Assert.Equal("file.pdf", result.OriginalFileName);
    }

    [Fact]
    public async Task Handle_NonExistentDocument_ThrowsNotFoundException()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(
            new GetDocumentDownloadUrlQuery { DonorId = Guid.NewGuid(), DocumentId = Guid.NewGuid() },
            CancellationToken.None));
    }

    [Fact]
    public async Task Handle_SuccessfulCall_SetsEntityIdForAuditBehaviour()
    {
        var document = AddDocument(DocumentType.BBBEECertificate);
        var command = new GetDocumentDownloadUrlQuery { DonorId = document.DonorId, DocumentId = document.Id };

        await _handler.Handle(command, CancellationToken.None);

        // AuditBehaviour reads EntityId (and EntityType/Action, which are fixed
        // constants on the query) after the handler completes — this is the only
        // audit mechanism now; the handler itself never touches AuditLogs.
        Assert.Equal(document.Id, command.EntityId);
    }

    [Fact]
    public void Query_ImplementsIAuditableCommand_WithViewedAction()
    {
        var command = new GetDocumentDownloadUrlQuery();

        Assert.Equal(nameof(DonorDocument), command.EntityType);
        Assert.Equal(AuditAction.Viewed, command.Action);
    }

    [Fact]
    public async Task Handle_AlwaysGeneratesSasUrlWithHardcoded15MinuteExpiry()
    {
        var document = AddDocument(DocumentType.Signature);

        await _handler.Handle(
            new GetDocumentDownloadUrlQuery { DonorId = document.DonorId, DocumentId = document.Id },
            CancellationToken.None);

        await _blobStorageMock.Received(1).GenerateSasUrlAsync(
            document.BlobStoragePath,
            TimeSpan.FromMinutes(15));
    }
}
