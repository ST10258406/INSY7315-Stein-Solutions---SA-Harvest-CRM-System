using CRM.Application.Tests.Common;
using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Common.Models;
using CRM.Application.Modules.PublicDonors.Commands.SubmitPublicDonorDocument;
using CRM.Domain.Entities;
using CRM.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace CRM.Application.Tests.Modules.PublicDonors.Commands;

public class SubmitPublicDonorDocumentCommandHandlerTests
{
    private readonly IDonorRepository _donorsMock = Substitute.For<IDonorRepository>();
    private readonly IDonorDocumentRepository _documentsMock = Substitute.For<IDonorDocumentRepository>();
    private readonly IAuditLogRepository _auditLogsMock = Substitute.For<IAuditLogRepository>();
    private readonly IUnitOfWork _unitOfWorkMock = Substitute.For<IUnitOfWork>();
    private readonly IBlobStorageService _blobStorageMock = Substitute.For<IBlobStorageService>();
    private readonly SubmitPublicDonorDocumentCommandHandler _handler;

    private readonly List<DonorDocument> _documents = [];
    private readonly List<AuditLog> _auditLogs = [];

    public SubmitPublicDonorDocumentCommandHandlerTests()
    {
        _documentsMock.AddAsync(Arg.Do<DonorDocument>(_documents.Add), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _auditLogsMock.AddAsync(Arg.Do<AuditLog>(_auditLogs.Add), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        _blobStorageMock.UploadAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(new BlobUploadResult("some/path", "https://blob.example/some/path"));

        _handler = new SubmitPublicDonorDocumentCommandHandler(
            _donorsMock, _documentsMock, _auditLogsMock, _unitOfWorkMock, _blobStorageMock,
            NullLogger<SubmitPublicDonorDocumentCommandHandler>.Instance);
    }

    private static SubmitPublicDonorDocumentCommand MakeCommand(string token) => new()
    {
        SessionToken = token,
        DocumentType = "BBBEECertificate",
        FileStream = TestFiles.PdfStream(),
        OriginalFileName = "cert.pdf",
        ContentType = "application/pdf",
        FileSizeBytes = 3,
        IpAddress = "1.2.3.4",
        UserAgent = "xunit"
    };

    [Fact]
    public async Task Handle_ValidToken_ClaimsItAndCreatesDocumentLinkedToTheResolvedDonor()
    {
        var donorId = Guid.NewGuid();
        _donorsMock.ClaimBySubmissionTokenAsync("valid-token", Arg.Any<CancellationToken>()).Returns(donorId);

        var result = await _handler.Handle(MakeCommand("valid-token"), CancellationToken.None);

        var document = Assert.Single(_documents);
        Assert.Equal(donorId, document.DonorId);
        Assert.Equal(DocumentType.BBBEECertificate, document.DocumentType);
        Assert.Null(document.UploadedByUserId); // anonymous uploader
        Assert.True(document.IsActive);

        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        Assert.Equal("Document uploaded successfully.", result.Message);
    }

    [Fact]
    public async Task Handle_ClaimReturnsNull_ThrowsValidationExceptionAndUploadsNothing()
    {
        // Covers unknown, expired, and already-consumed tokens alike — the
        // repository collapses all three into "no row claimed" (null), so the
        // handler has nothing further to distinguish here.
        _donorsMock.ClaimBySubmissionTokenAsync("bogus-token", Arg.Any<CancellationToken>()).Returns((Guid?)null);

        await Assert.ThrowsAsync<ValidationException>(
            () => _handler.Handle(MakeCommand("bogus-token"), CancellationToken.None));

        Assert.Empty(_documents);
        await _blobStorageMock.DidNotReceive().UploadAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>());
        await _unitOfWorkMock.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidToken_WritesManualAuditLogWithDonorIdButResponseNeverContainsIt()
    {
        var donorId = Guid.NewGuid();
        _donorsMock.ClaimBySubmissionTokenAsync("valid-token", Arg.Any<CancellationToken>()).Returns(donorId);

        var result = await _handler.Handle(MakeCommand("valid-token"), CancellationToken.None);

        // Donor id is recorded server-side (audit log) only.
        var auditLog = Assert.Single(_auditLogs);
        Assert.Null(auditLog.UserId);
        Assert.Equal(AuditAction.Created, auditLog.Action);
        Assert.Contains(donorId.ToString(), auditLog.NewValues);

        // The response DTO structurally has no Id-shaped property at all.
        var properties = result.GetType().GetProperties();
        Assert.DoesNotContain(properties, p => p.PropertyType == typeof(Guid) || p.PropertyType == typeof(Guid?));
    }

    [Fact]
    public async Task Handle_BlobUploadFailsAfterTokenAlreadyClaimed_PropagatesAndDoesNotInsertDocument()
    {
        // The claim is a separate, already-committed statement (see
        // IDonorRepository.ClaimBySubmissionTokenAsync remarks) — by the time
        // this runs, the token is unrecoverably burned even though the request
        // as a whole still fails. That's a deliberate trade-off documented on
        // the handler; this test locks in that a failure here still surfaces
        // as an exception rather than a false-success response, and that no
        // document/audit row is left half-written.
        var donorId = Guid.NewGuid();
        _donorsMock.ClaimBySubmissionTokenAsync("valid-token", Arg.Any<CancellationToken>()).Returns(donorId);
        _blobStorageMock.UploadAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>())
            .ThrowsAsync(new InvalidOperationException("blob storage unavailable"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(MakeCommand("valid-token"), CancellationToken.None));

        Assert.Empty(_documents);
        await _unitOfWorkMock.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
