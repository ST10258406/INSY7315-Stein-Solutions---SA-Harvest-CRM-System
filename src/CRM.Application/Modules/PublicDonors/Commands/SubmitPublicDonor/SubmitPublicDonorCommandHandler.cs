namespace CRM.Application.Modules.PublicDonors.Commands.SubmitPublicDonor;

using System.Security.Cryptography;
using System.Text.Json;
using CRM.Application.Common.Files;
using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.PublicDonors.Dtos;
using CRM.Domain.Constants;
using CRM.Domain.Entities;
using CRM.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

public class SubmitPublicDonorCommandHandler : IRequestHandler<SubmitPublicDonorCommand, SubmitPublicDonorResponseDto>
{
    private static readonly TimeSpan SubmissionTokenLifetime = TimeSpan.FromHours(1);
    private const string SuccessMessage = "Thank you. Your submission has been received and is currently under review.";

    private readonly IDonorRepository _donors;
    private readonly IDonorDocumentRepository _documents;
    private readonly IInteractionLogRepository _interactionLogs;
    private readonly IAuditLogRepository _auditLogs;
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBlobStorageService _blobStorage;
    private readonly INotificationService _notificationService;
    private readonly IEmailService _emailService;
    private readonly ILogger<SubmitPublicDonorCommandHandler> _logger;

    public SubmitPublicDonorCommandHandler(
        IDonorRepository donors,
        IDonorDocumentRepository documents,
        IInteractionLogRepository interactionLogs,
        IAuditLogRepository auditLogs,
        IUserRepository users,
        IUnitOfWork unitOfWork,
        IBlobStorageService blobStorage,
        INotificationService notificationService,
        IEmailService emailService,
        ILogger<SubmitPublicDonorCommandHandler> logger)
    {
        _donors = donors;
        _documents = documents;
        _interactionLogs = interactionLogs;
        _auditLogs = auditLogs;
        _users = users;
        _unitOfWork = unitOfWork;
        _blobStorage = blobStorage;
        _notificationService = notificationService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<SubmitPublicDonorResponseDto> Handle(SubmitPublicDonorCommand command, CancellationToken cancellationToken)
    {
        var req = command.Request;

        // Owns every FK that would otherwise require an authenticated user —
        // see SystemUsers' remarks. Seeded by SystemUserSeeder in every
        // environment, so this should never be null in practice; failing loudly
        // here beats silently attributing the donor to nobody.
        var systemUser = await _users.GetByEmailAsync(SystemUsers.PublicFormEmail, cancellationToken)
            ?? throw new InvalidOperationException(
                $"System user '{SystemUsers.PublicFormEmail}' is not seeded — SystemUserSeeder must run before this endpoint can be used.");

        var donor = new Donor
        {
            Id = Guid.NewGuid(),
            CompanyName = req.Company.CompanyName,
            CompanyTypeId = req.Company.CompanyTypeId,
            Website = req.Company.Website,
            RegisteredCompanyName = req.Company.RegisteredCompanyName,
            TradingName = req.Company.TradingName,
            EntityTypeId = req.Company.EntityTypeId,
            CompanyRegistrationNumber = req.Company.CompanyRegistrationNumber,
            IncomeTaxNumber = req.Company.IncomeTaxNumber,
            DonationFrequencyId = req.Donations.FrequencyId,
            BbbeeStatusId = req.Compliance?.BbbeeStatusId,
            CollectionAddress = req.Donations.CollectionAddress,
            OperationsLogisticsDetails = req.Donations.OperationsLogisticsDetails,
            AdditionalInformation = req.Crm?.AdditionalInformation,
            RelationshipManagerId = req.Crm?.RelationshipManagerId,
            // Server-set, always — same discipline as CreateDonorCommandHandler.
            Status = DonorStatus.PendingReview,
            SubmissionSource = SubmissionSource.PublicForm,
            MarketingConsent = req.Crm?.MarketingConsent ?? false,
            MarketingConsentDate = req.Crm?.MarketingConsent == true ? DateTime.UtcNow : null,
            ImpactReportingPreferences = req.Crm?.ImpactReportingPreferences,
            CreatedByUserId = systemUser.Id,
        };

        donor.LegalAddress = new DonorLegalAddress
        {
            DonorId = donor.Id,
            StreetAddress = req.LegalAddress.StreetAddress,
            Suburb = req.LegalAddress.Suburb,
            City = req.LegalAddress.City,
            ProvinceId = req.LegalAddress.ProvinceId,
            PostalCode = req.LegalAddress.PostalCode
        };

        donor.Contacts.Add(new DonorContact
        {
            DonorId = donor.Id,
            ContactType = ContactType.Primary,
            Name = req.PrimaryContact.Name,
            JobTitle = req.PrimaryContact.JobTitle,
            Phone = req.PrimaryContact.Phone,
            Email = req.PrimaryContact.Email
        });

        if (req.MarketingContact is not null)
        {
            donor.Contacts.Add(new DonorContact
            {
                DonorId = donor.Id,
                ContactType = ContactType.Marketing,
                Name = req.MarketingContact.Name,
                Phone = req.MarketingContact.Phone,
                Email = req.MarketingContact.Email
            });
        }

        if (req.AccountsContact is not null)
        {
            donor.Contacts.Add(new DonorContact
            {
                DonorId = donor.Id,
                ContactType = ContactType.Accounts,
                Name = req.AccountsContact.Name,
                Phone = req.AccountsContact.Phone,
                Email = req.AccountsContact.Email
            });
        }

        foreach (var regionId in req.Donations.RegionIds.Distinct())
        {
            donor.OperationalRegions.Add(new DonorOperationalRegion { DonorId = donor.Id, OperationalRegionId = regionId });
        }

        foreach (var typeId in req.Donations.TypeIds.Distinct())
        {
            donor.DonationTypes.Add(new DonorDonationType { DonorId = donor.Id, DonationTypeId = typeId });
        }

        donor.ReferenceNumber = await _donors.GetNextReferenceNumberAsync(cancellationToken);

        var submissionToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        donor.SubmissionToken = submissionToken;
        donor.SubmissionTokenExpiresAt = DateTimeOffset.UtcNow.Add(SubmissionTokenLifetime);

        await _donors.AddAsync(donor, cancellationToken);

        // Already validated as a real PNG by SubmitPublicDonorCommandValidator —
        // ValidationBehaviour ran before this handler, so re-validating here would
        // just be duplicate work. TryDecode can't meaningfully fail at this point.
        Base64PngDecoder.TryDecode(req.Signature.ImageBase64, out var signatureBytes);

        // Upload before inserting the DonorDocument row — if the upload fails, we
        // don't want a DB record pointing at a blob that was never written. An
        // orphaned blob from a subsequent failed SaveChanges is an accepted, cheap
        // failure mode (mirrors UploadDonorDocumentCommandHandler).
        var blobPath = $"donors/{donor.Id}/{DocumentType.Signature}/{Guid.NewGuid()}_signature.png";
        using (var stream = new MemoryStream(signatureBytes))
        {
            await _blobStorage.UploadAsync(stream, blobPath, "image/png");
        }

        var signatureDocument = new DonorDocument
        {
            Id = Guid.NewGuid(),
            DonorId = donor.Id,
            DocumentType = DocumentType.Signature,
            FileName = "signature.png",
            BlobStoragePath = blobPath,
            FileSizeBytes = signatureBytes.Length,
            MimeType = "image/png",
            // Null, not the system user — DonorDocument.UploadedByUserId is
            // nullable specifically for an anonymous uploader like this one.
            UploadedByUserId = null,
            IsActive = true
        };
        await _documents.AddAsync(signatureDocument, cancellationToken);

        var interactionLog = new InteractionLog
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            DonorId = donor.Id,
            CreatedByUserId = systemUser.Id,
            InteractionType = InteractionType.FormSubmission,
            Subject = "Public form submission received",
            Body = $"{donor.CompanyName} submitted the public onboarding form. Reference {donor.ReferenceNumber}."
        };
        await _interactionLogs.AddAsync(interactionLog, cancellationToken);

        var approval = new DonorApproval
        {
            Id = Guid.NewGuid(),
            DonorId = donor.Id,
            RequestedByUserId = systemUser.Id,
            Status = ApprovalStatus.Pending
        };
        await _donors.AddApprovalAsync(approval, cancellationToken);

        // Manual audit_logs write — this command deliberately does not implement
        // IAuditableCommand (see SubmitPublicDonorCommand's remarks), so
        // AuditBehaviour never runs for it. UserId = null: there is genuinely no
        // authenticated actor to attribute this to, unlike CreatedByUserId above
        // (a required FK, so it gets the system user instead).
        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = null,
            EntityType = nameof(Donor),
            EntityId = donor.Id,
            Action = AuditAction.Created,
            NewValues = JsonSerializer.Serialize(new
            {
                donor.Id,
                donor.CompanyName,
                donor.Status,
                donor.SubmissionSource,
                donor.ReferenceNumber
            }),
            IpAddress = command.IpAddress,
            UserAgent = command.UserAgent,
            CreatedAt = DateTime.UtcNow
        };
        await _auditLogs.AddAsync(auditLog, cancellationToken);

        // One SaveChangesAsync call — donor + its children, the signature
        // document, the interaction log, the approval, and the audit log all
        // commit together or not at all.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Notifications fire only after the main unit of work has committed —
        // same established pattern as CreateDonorCommandHandler. A failure here
        // must not roll back (or even fail) the request: the donor is already
        // correctly saved and pending review, so the worst case is an admin
        // finding out late rather than an orphaned/half-created donor. Logged
        // loudly per-admin so a failure here is never silent.
        var adminUserIds = await _users.GetActiveUserIdsByRoleAsync("Admin", cancellationToken);

        foreach (var adminUserId in adminUserIds)
        {
            try
            {
                await _notificationService.CreateAsync(
                    adminUserId,
                    "New donor pending review",
                    $"{donor.CompanyName} submitted the public onboarding form and is awaiting approval.",
                    NotificationType.NewDonorPendingReview,
                    donor.Id,
                    nameof(Donor),
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to notify admin {AdminUserId} of new public donor submission {DonorId} ({ReferenceNumber}).",
                    adminUserId, donor.Id, donor.ReferenceNumber);
            }
        }

        // Confirmation email to the submitter — same fire-and-forget discipline as
        // the admin notifications above: fired only after commit, wrapped so a
        // failure (network, provider outage, whatever) cannot fail or roll back a
        // submission that already succeeded. EmailService itself is documented to
        // never throw, but this handler doesn't rely on that guarantee holding.
        // Deliberately no InteractionLog entry for this — system-triggered
        // transactional email to a not-yet-approved donor isn't donor-interaction
        // history.
        try
        {
            await _emailService.SendAsync(
                to: req.PrimaryContact.Email!,
                subject: "Your SA Harvest CRM donor submission has been received",
                htmlBody: BuildConfirmationEmailBody(req.PrimaryContact.Name, donor.ReferenceNumber),
                emailType: EmailType.OnboardingConfirmation,
                donorId: donor.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send onboarding confirmation email for donor {DonorId} ({ReferenceNumber}).",
                donor.Id, donor.ReferenceNumber);
        }

        return new SubmitPublicDonorResponseDto
        {
            Message = SuccessMessage,
            ReferenceNumber = donor.ReferenceNumber,
            SubmissionToken = submissionToken
        };
    }

    private static string BuildConfirmationEmailBody(string contactName, string referenceNumber) =>
        $"<p>Hi {WebUtility.HtmlEncode(contactName)},</p>" +
        $"<p>Thank you for submitting your donor information to SA Harvest. We have received your submission (reference {WebUtility.HtmlEncode(referenceNumber)}) and it is currently <strong>Pending Review</strong>.</p>" +
        "<p>No action is needed from you at this time. We will be in touch once your submission has been reviewed.</p>" +
        "<p>Kind regards,<br/>SA Harvest</p>";
}
