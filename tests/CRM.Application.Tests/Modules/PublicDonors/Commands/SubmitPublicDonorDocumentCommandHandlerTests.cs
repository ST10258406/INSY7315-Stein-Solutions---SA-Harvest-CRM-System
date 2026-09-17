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

    private static Donor MakePendingDonor(string token, DateTimeOffset? expiresAt = null) => new()
    {
        Id = Guid.NewGuid(),
        CompanyName = "Doc Test Pty Ltd",
        ReferenceNumber = "DON-2026-00001",
        SubmissionToken = token,
        SubmissionTokenExpiresAt = expiresAt ?? DateTimeOffset.UtcNow.AddMinutes(30)
    };

    private static SubmitPublicDonorDocumentCommand MakeCommand(string token) => new()
    {
        SessionToken = token,
        DocumentType = "BBBEECertificate",
        FileStream = new MemoryStream([1, 2, 3]),
        OriginalFileName = "cert.pdf",
        ContentType = "application/pdf",
        FileSizeBytes = 3,
        IpAddress = "1.2.3.4",
        UserAgent = "xunit"
    };

    [Fact]
    public async Task Handle_ValidToken_UploadsAndCreatesDocumentLinkedToTheResolvedDonor()
    {
        var donor = MakePendingDonor("valid-token");
        _donorsMock.GetBySubmissionTokenAsync("valid-token", Arg.Any<CancellationToken>()).Returns(donor);

        var result = await _handler.Handle(MakeCommand("valid-token"), CancellationToken.None);

        var document = Assert.Single(_documents);
        Assert.Equal(donor.Id, document.DonorId);
        Assert.Equal(DocumentType.BBBEECertificate, document.DocumentType);
        Assert.Null(document.UploadedByUserId); // anonymous uploader
        Assert.True(document.IsActive);

        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        Assert.Equal("Document uploaded successfully.", result.Message);
    }

    [Fact]
    public async Task Handle_ValidToken_BurnsTheTokenSoItCannotBeReplayed()
    {
        var donor = MakePendingDonor("valid-token");
        _donorsMock.GetBySubmissionTokenAsync("valid-token", Arg.Any<CancellationToken>()).Returns(donor);

        await _handler.Handle(MakeCommand("valid-token"), CancellationToken.None);

        Assert.Null(donor.SubmissionToken);
        Assert.Null(donor.SubmissionTokenExpiresAt);
    }

    [Fact]
    public async Task Handle_UnknownToken_ThrowsValidationExceptionAndUploadsNothing()
    {
        _donorsMock.GetBySubmissionTokenAsync("bogus-token", Arg.Any<CancellationToken>()).Returns((Donor?)null);

        await Assert.ThrowsAsync<ValidationException>(
            () => _handler.Handle(MakeCommand("bogus-token"), CancellationToken.None));

        Assert.Empty(_documents);
        await _blobStorageMock.DidNotReceive().UploadAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>());
        await _unitOfWorkMock.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExpiredToken_ThrowsValidationException()
    {
        var donor = MakePendingDonor("expired-token", DateTimeOffset.UtcNow.AddMinutes(-1));
        _donorsMock.GetBySubmissionTokenAsync("expired-token", Arg.Any<CancellationToken>()).Returns(donor);

        await Assert.ThrowsAsync<ValidationException>(
            () => _handler.Handle(MakeCommand("expired-token"), CancellationToken.None));

        Assert.Empty(_documents);
    }

    [Fact]
    public async Task Handle_AlreadyConsumedToken_ThrowsValidationException()
    {
        // Simulates replay: the token was already burned by a prior successful
        // call, so SubmissionTokenExpiresAt is now null — same as a token that
        // never existed, from this handler's point of view.
        var donor = MakePendingDonor("used-token");
        donor.SubmissionToken = null;
        donor.SubmissionTokenExpiresAt = null;

        // A burned token no longer matches any donor via GetBySubmissionTokenAsync
        // in the real repository (the column is now null), so the lookup itself
        // returns null for a replayed token.
        _donorsMock.GetBySubmissionTokenAsync("used-token", Arg.Any<CancellationToken>()).Returns((Donor?)null);

        await Assert.ThrowsAsync<ValidationException>(
            () => _handler.Handle(MakeCommand("used-token"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ValidToken_WritesManualAuditLogWithDonorIdButResponseNeverContainsIt()
    {
        var donor = MakePendingDonor("valid-token");
        _donorsMock.GetBySubmissionTokenAsync("valid-token", Arg.Any<CancellationToken>()).Returns(donor);

        var result = await _handler.Handle(MakeCommand("valid-token"), CancellationToken.None);

        // Donor id is recorded server-side (audit log) only.
        var auditLog = Assert.Single(_auditLogs);
        Assert.Null(auditLog.UserId);
        Assert.Equal(AuditAction.Created, auditLog.Action);
        Assert.Contains(donor.Id.ToString(), auditLog.NewValues);

        // The response DTO structurally has no Id-shaped property at all.
        var properties = result.GetType().GetProperties();
        Assert.DoesNotContain(properties, p => p.PropertyType == typeof(Guid) || p.PropertyType == typeof(Guid?));
    }

    [Fact]
    public async Task Handle_BlobUploadFailure_DoesNotInsertDocumentOrBurnToken()
    {
        var donor = MakePendingDonor("valid-token");
        _donorsMock.GetBySubmissionTokenAsync("valid-token", Arg.Any<CancellationToken>()).Returns(donor);
        _blobStorageMock.UploadAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>())
            .ThrowsAsync(new InvalidOperationException("blob storage unavailable"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(MakeCommand("valid-token"), CancellationToken.None));

        Assert.Empty(_documents);
        Assert.NotNull(donor.SubmissionToken); // not burned — nothing actually succeeded
        await _unitOfWorkMock.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
