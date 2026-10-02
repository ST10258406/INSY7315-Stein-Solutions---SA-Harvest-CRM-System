using CRM.Application.Common.Interfaces;
using CRM.Application.Common.Models;
using CRM.Application.Modules.Donors.Dtos;
using CRM.Application.Modules.PublicDonors.Commands.SubmitPublicDonor;
using CRM.Application.Modules.PublicDonors.Dtos;
using CRM.Domain.Constants;
using CRM.Domain.Entities;
using CRM.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace CRM.Application.Tests.Modules.PublicDonors.Commands;

public class SubmitPublicDonorCommandHandlerTests
{
    private const string ValidPngBase64 =
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUAAScy0NkAAAAASUVORK5CYII=";
    private const string ValidSignatureDataUri = $"data:image/png;base64,{ValidPngBase64}";

    private readonly IDonorRepository _donorsMock = Substitute.For<IDonorRepository>();
    private readonly IDonorDocumentRepository _documentsMock = Substitute.For<IDonorDocumentRepository>();
    private readonly IInteractionLogRepository _interactionLogsMock = Substitute.For<IInteractionLogRepository>();
    private readonly IAuditLogRepository _auditLogsMock = Substitute.For<IAuditLogRepository>();
    private readonly IUserRepository _usersMock = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _unitOfWorkMock = Substitute.For<IUnitOfWork>();
    private readonly IBlobStorageService _blobStorageMock = Substitute.For<IBlobStorageService>();
    private readonly INotificationService _notificationServiceMock = Substitute.For<INotificationService>();
    private readonly IEmailService _emailServiceMock = Substitute.For<IEmailService>();
    private readonly SubmitPublicDonorCommandHandler _handler;

    private readonly List<Donor> _donors = [];
    private readonly List<DonorDocument> _documents = [];
    private readonly List<InteractionLog> _interactionLogs = [];
    private readonly List<DonorApproval> _approvals = [];
    private readonly List<AuditLog> _auditLogs = [];
    private readonly User _systemUser = new() { Id = Guid.NewGuid(), Email = SystemUsers.PublicFormEmail, IsActive = false };

    public SubmitPublicDonorCommandHandlerTests()
    {
        _usersMock.GetByEmailAsync(SystemUsers.PublicFormEmail, Arg.Any<CancellationToken>()).Returns(_systemUser);
        _usersMock.GetActiveUserIdsByRoleAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns([]);

        _donorsMock.AddAsync(Arg.Do<Donor>(_donors.Add), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _donorsMock.AddApprovalAsync(Arg.Do<DonorApproval>(_approvals.Add), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _donorsMock.GetNextReferenceNumberAsync(Arg.Any<CancellationToken>()).Returns("DON-2026-00001");

        _documentsMock.AddAsync(Arg.Do<DonorDocument>(_documents.Add), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _interactionLogsMock.AddAsync(Arg.Do<InteractionLog>(_interactionLogs.Add), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _auditLogsMock.AddAsync(Arg.Do<AuditLog>(_auditLogs.Add), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        _blobStorageMock.UploadAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(new BlobUploadResult("some/path", "https://blob.example/some/path"));

        _handler = new SubmitPublicDonorCommandHandler(
            _donorsMock, _documentsMock, _interactionLogsMock, _auditLogsMock, _usersMock,
            _unitOfWorkMock, _blobStorageMock, _notificationServiceMock, _emailServiceMock,
            NullLogger<SubmitPublicDonorCommandHandler>.Instance);
    }

    private void SetupAdmins(int count) =>
        _usersMock.GetActiveUserIdsByRoleAsync("Admin", Arg.Any<CancellationToken>())
            .Returns(Enumerable.Range(0, count).Select(_ => Guid.NewGuid()).ToList());

    private static SubmitPublicDonorRequest MakeValidRequest() => new()
    {
        Company = new CreateDonorCompanyRequest
        {
            CompanyName = "Test Donor Pty Ltd",
            CompanyTypeId = 1,
            RegisteredCompanyName = "Test Donor (Pty) Ltd",
            EntityTypeId = 1,
            IncomeTaxNumber = "9012345678"
        },
        PrimaryContact = new CreateDonorContactRequest { Name = "Jane Tester", Phone = "+27820000000", Email = "jane@test.co.za" },
        LegalAddress = new CreateDonorLegalAddressRequest
        {
            StreetAddress = "1 Test Street", Suburb = "Testville", City = "Johannesburg", ProvinceId = 1, PostalCode = "2000"
        },
        Donations = new CreateDonorDonationsRequest { FrequencyId = 1, TypeIds = [1], CollectionAddress = "Gate 1", RegionIds = [1] },
        Signature = new PublicDonorSignatureRequest { ImageBase64 = ValidSignatureDataUri }
    };

    [Fact]
    public async Task Handle_ValidCommand_CreatesAllFiveSideEffects()
    {
        SetupAdmins(2);
        var command = new SubmitPublicDonorCommand { Request = MakeValidRequest(), IpAddress = "1.2.3.4", UserAgent = "xunit" };

        var result = await _handler.Handle(command, CancellationToken.None);

        // 1. Donor created, PendingReview + PublicForm
        var donor = Assert.Single(_donors);
        Assert.Equal(DonorStatus.PendingReview, donor.Status);
        Assert.Equal(SubmissionSource.PublicForm, donor.SubmissionSource);
        Assert.Equal(_systemUser.Id, donor.CreatedByUserId);
        Assert.Equal("DON-2026-00001", donor.ReferenceNumber);

        // 2. Signature document
        var document = Assert.Single(_documents);
        Assert.Equal(DocumentType.Signature, document.DocumentType);
        Assert.Equal(donor.Id, document.DonorId);
        Assert.Null(document.UploadedByUserId); // nullable, deliberately not the system user

        // 3. FormSubmission interaction log — and exactly one, i.e. the
        // confirmation email below does not add a second one.
        var log = Assert.Single(_interactionLogs);
        Assert.Equal(InteractionType.FormSubmission, log.InteractionType);
        Assert.Equal(_systemUser.Id, log.CreatedByUserId);

        // 4. Pending approval
        var approval = Assert.Single(_approvals);
        Assert.Equal(ApprovalStatus.Pending, approval.Status);
        Assert.Equal(_systemUser.Id, approval.RequestedByUserId);

        // 5. Admin notifications — one per admin, not just one total
        await _notificationServiceMock.Received(2).CreateAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), NotificationType.NewDonorPendingReview,
            donor.Id, nameof(Donor), Arg.Any<CancellationToken>());

        // Manual audit row (command is not IAuditableCommand, so this is the ONLY audit write)
        var auditLog = Assert.Single(_auditLogs);
        Assert.Null(auditLog.UserId);
        Assert.Equal(AuditAction.Created, auditLog.Action);
        Assert.Equal(donor.Id, auditLog.EntityId);
        Assert.Equal("1.2.3.4", auditLog.IpAddress);

        // Exactly one commit for donor + children + document + log + approval + audit
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());

        // 6. Onboarding confirmation email to the address on the submission
        await _emailServiceMock.Received(1).SendAsync(
            "jane@test.co.za",
            Arg.Any<string>(),
            Arg.Any<string>(),
            EmailType.OnboardingConfirmation,
            donor.Id,
            Arg.Any<Guid?>());

        Assert.Equal("DON-2026-00001", result.ReferenceNumber);
        Assert.False(string.IsNullOrWhiteSpace(result.SubmissionToken));
    }

    [Fact]
    public async Task Handle_ValidCommand_EmailBodyMentionsPendingReviewAndReferenceNumber()
    {
        var command = new SubmitPublicDonorCommand { Request = MakeValidRequest() };

        await _handler.Handle(command, CancellationToken.None);

        await _emailServiceMock.Received(1).SendAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Is<string>(body => body.Contains("Pending Review") && body.Contains("DON-2026-00001") && body.Contains("Jane Tester")),
            EmailType.OnboardingConfirmation,
            Arg.Any<Guid?>(),
            Arg.Any<Guid?>());
    }

    [Fact]
    public async Task Handle_EmailServiceThrows_DoesNotFailTheRequest()
    {
        // Same discipline as the admin-notification failure test above: a failed
        // confirmation email must not turn a successful donor submission into a
        // 500, and must not roll back what was already committed.
        _emailServiceMock
            .SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<EmailType>(), Arg.Any<Guid?>(), Arg.Any<Guid?>())
            .ThrowsAsync(new InvalidOperationException("email provider down"));

        var command = new SubmitPublicDonorCommand { Request = MakeValidRequest() };

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal("DON-2026-00001", result.ReferenceNumber);
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ResponseNeverContainsTheDonorId()
    {
        var command = new SubmitPublicDonorCommand { Request = MakeValidRequest() };

        var result = await _handler.Handle(command, CancellationToken.None);

        // The response DTO structurally has no Id-shaped property at all — not just
        // "we didn't set it", there is nowhere for a donor Guid to leak from.
        var properties = typeof(SubmitPublicDonorResponseDto).GetProperties();
        Assert.DoesNotContain(properties, p => p.PropertyType == typeof(Guid) || p.PropertyType == typeof(Guid?));

        Assert.NotEqual(Guid.Empty.ToString(), result.ReferenceNumber);
        Assert.NotEqual(_donors[0].Id.ToString(), result.SubmissionToken);
    }

    [Fact]
    public async Task Handle_SubmissionTokenIsRandomAndUnrelatedToTheDonorId_NotJustTheGuidItself()
    {
        var command1 = new SubmitPublicDonorCommand { Request = MakeValidRequest() };
        var result1 = await _handler.Handle(command1, CancellationToken.None);

        var command2 = new SubmitPublicDonorCommand { Request = MakeValidRequest() };
        var result2 = await _handler.Handle(command2, CancellationToken.None);

        Assert.NotEqual(result1.SubmissionToken, result2.SubmissionToken);
        Assert.NotEqual(_donors[0].Id.ToString(), result1.SubmissionToken);
        Assert.Equal(result1.SubmissionToken, _donors[0].SubmissionToken);
        Assert.NotNull(_donors[0].SubmissionTokenExpiresAt);
    }

    [Fact]
    public async Task Handle_NoAdmins_DoesNotCallNotificationService()
    {
        SetupAdmins(0);
        var command = new SubmitPublicDonorCommand { Request = MakeValidRequest() };

        await _handler.Handle(command, CancellationToken.None);

        await _notificationServiceMock.DidNotReceive().CreateAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<NotificationType>(),
            Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NotificationServiceThrows_DoesNotFailTheRequest()
    {
        // A notification failure after the donor is already committed must not turn
        // into a 500 for the caller — the donor was created successfully; that's
        // what the response should reflect. See handler remarks on why this is
        // logged, not rethrown.
        SetupAdmins(1);
        _notificationServiceMock
            .CreateAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<NotificationType>(),
                Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("notification backend down"));

        var command = new SubmitPublicDonorCommand { Request = MakeValidRequest() };

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal("DON-2026-00001", result.ReferenceNumber);
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SystemUserNotSeeded_ThrowsLoudlyRatherThanSilentlyAttributingToNobody()
    {
        _usersMock.GetByEmailAsync(SystemUsers.PublicFormEmail, Arg.Any<CancellationToken>()).Returns((User?)null);
        var command = new SubmitPublicDonorCommand { Request = MakeValidRequest() };

        await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(command, CancellationToken.None));
    }
}
