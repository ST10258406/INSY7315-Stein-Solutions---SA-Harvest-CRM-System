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
    private readonly ICurrentUserService _currentUserServiceMock;
    private readonly GetDocumentDownloadUrlQueryHandler _handler;

    private readonly List<DonorDocument> _documents = [];
    private readonly List<AuditLog> _auditLogs = [];
    private readonly Guid _currentUserId = Guid.NewGuid();

    public GetDocumentDownloadUrlQueryHandlerTests()
    {
        _contextMock = Substitute.For<IApplicationDbContext>();
        _blobStorageMock = Substitute.For<IBlobStorageService>();
        _currentUserServiceMock = Substitute.For<ICurrentUserService>();

        _currentUserServiceMock.GetCurrentUserId().Returns(_currentUserId);

        _blobStorageMock
            .GenerateSasUrlAsync(Arg.Any<string>(), Arg.Any<TimeSpan>())
            .Returns(ci => Task.FromResult("https://storage.blob.core.windows.net/" + ci.ArgAt<string>(0) + "?sv=sas"));

        var documentsDbSet = _documents.BuildMockDbSet();
        _contextMock.DonorDocuments.Returns(documentsDbSet);

        var auditLogsDbSet = _auditLogs.BuildMockDbSet();
        _contextMock.AuditLogs.Returns(auditLogsDbSet);
        _contextMock.AuditLogs.Add(Arg.Do<AuditLog>(a => _auditLogs.Add(a)));

        _handler = new GetDocumentDownloadUrlQueryHandler(_contextMock, _blobStorageMock, _currentUserServiceMock);
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

    private void SetRoles(params string[] roles) =>
        _currentUserServiceMock.GetCurrentUserRoles().Returns(roles.ToList());

    [Fact]
    public async Task Handle_ProcurementRequestingSignature_ReturnsUrl()
    {
        SetRoles("Procurement");
        var document = AddDocument(DocumentType.Signature);

        var result = await _handler.Handle(
            new GetDocumentDownloadUrlQuery { DonorId = document.DonorId, DocumentId = document.Id },
            CancellationToken.None);

        Assert.False(string.IsNullOrEmpty(result.DownloadUrl));
        Assert.Equal("file.pdf", result.OriginalFileName);
    }

    [Fact]
    public async Task Handle_ProcurementRequestingBBBEECertificate_ThrowsForbiddenException()
    {
        SetRoles("Procurement");
        var document = AddDocument(DocumentType.BBBEECertificate);

        await Assert.ThrowsAsync<ForbiddenException>(() => _handler.Handle(
            new GetDocumentDownloadUrlQuery { DonorId = document.DonorId, DocumentId = document.Id },
            CancellationToken.None));
    }

    [Fact]
    public async Task Handle_AdminRequestingBBBEECertificate_ReturnsUrl()
    {
        SetRoles("Admin");
        var document = AddDocument(DocumentType.BBBEECertificate);

        var result = await _handler.Handle(
            new GetDocumentDownloadUrlQuery { DonorId = document.DonorId, DocumentId = document.Id },
            CancellationToken.None);

        Assert.False(string.IsNullOrEmpty(result.DownloadUrl));
    }

    [Fact]
    public async Task Handle_AdminRequestingSignature_ReturnsUrl()
    {
        SetRoles("Admin");
        var document = AddDocument(DocumentType.Signature);

        var result = await _handler.Handle(
            new GetDocumentDownloadUrlQuery { DonorId = document.DonorId, DocumentId = document.Id },
            CancellationToken.None);

        Assert.False(string.IsNullOrEmpty(result.DownloadUrl));
    }

    [Fact]
    public async Task Handle_NonExistentDocument_ThrowsNotFoundException()
    {
        SetRoles("Admin");

        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(
            new GetDocumentDownloadUrlQuery { DonorId = Guid.NewGuid(), DocumentId = Guid.NewGuid() },
            CancellationToken.None));
    }

    [Fact]
    public async Task Handle_SuccessfulCall_WritesAuditLogWithViewedAction()
    {
        SetRoles("Admin");
        var document = AddDocument(DocumentType.BBBEECertificate);

        await _handler.Handle(
            new GetDocumentDownloadUrlQuery { DonorId = document.DonorId, DocumentId = document.Id },
            CancellationToken.None);

        var entry = Assert.Single(_auditLogs);
        Assert.Equal(AuditAction.Viewed, entry.Action);
        Assert.Equal(nameof(DonorDocument), entry.EntityType);
        Assert.Equal(document.Id, entry.EntityId);
        Assert.Equal(_currentUserId, entry.UserId);
    }

    [Fact]
    public async Task Handle_AlwaysGeneratesSasUrlWithHardcoded15MinuteExpiry()
    {
        SetRoles("Admin");
        var document = AddDocument(DocumentType.Signature);

        await _handler.Handle(
            new GetDocumentDownloadUrlQuery { DonorId = document.DonorId, DocumentId = document.Id },
            CancellationToken.None);

        await _blobStorageMock.Received(1).GenerateSasUrlAsync(
            document.BlobStoragePath,
            TimeSpan.FromMinutes(15));
    }
}
