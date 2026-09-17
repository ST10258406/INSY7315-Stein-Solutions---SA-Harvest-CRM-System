namespace CRM.Application.Modules.Donors.Commands.CreateDonor;

using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Donors.Dtos;
using CRM.Domain.Entities;
using CRM.Domain.Enums;
using MediatR;

public class CreateDonorCommandHandler : IRequestHandler<CreateDonorCommand, DonorDetailDto>
{
    private readonly IDonorRepository _donors;
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;
    private readonly ICurrentUserService _currentUserService;

    public CreateDonorCommandHandler(
        IDonorRepository donors,
        IUserRepository users,
        IUnitOfWork unitOfWork,
        INotificationService notificationService,
        ICurrentUserService currentUserService)
    {
        _donors = donors;
        _users = users;
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _currentUserService = currentUserService;
    }

    public async Task<DonorDetailDto> Handle(CreateDonorCommand command, CancellationToken cancellationToken)
    {
        var req = command.Request;
        var currentUserId = _currentUserService.GetCurrentUserId();

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
            // Server-set, always — never sourced from the request. Nothing in
            // CreateDonorRequest can override these (see Issue 32 AC).
            Status = DonorStatus.PendingReview,
            SubmissionSource = SubmissionSource.ManualCapture,
            MarketingConsent = req.Crm?.MarketingConsent ?? false,
            MarketingConsentDate = req.Crm?.MarketingConsent == true ? DateTime.UtcNow : null,
            ImpactReportingPreferences = req.Crm?.ImpactReportingPreferences,
            CreatedByUserId = currentUserId
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

        // Junction rows use composite keys (DonorId, RegionId/DonationTypeId) — no
        // surrogate Id — so these are plain new instances, not looked up first.
        foreach (var regionId in req.Donations.RegionIds.Distinct())
        {
            donor.OperationalRegions.Add(new DonorOperationalRegion { DonorId = donor.Id, OperationalRegionId = regionId });
        }

        foreach (var typeId in req.Donations.TypeIds.Distinct())
        {
            donor.DonationTypes.Add(new DonorDonationType { DonorId = donor.Id, DonationTypeId = typeId });
        }

        // Every donor gets a reference number, regardless of how it was created —
        // not just public-form submissions (see SubmitPublicDonorCommandHandler).
        donor.ReferenceNumber = await _donors.GetNextReferenceNumberAsync(cancellationToken);

        await _donors.AddAsync(donor, cancellationToken);

        var approval = new DonorApproval
        {
            Id = Guid.NewGuid(),
            DonorId = donor.Id,
            RequestedByUserId = currentUserId,
            Status = ApprovalStatus.Pending
        };
        await _donors.AddApprovalAsync(approval, cancellationToken);

        // One SaveChangesAsync call — donor, its children, and the approval
        // commit together in a single transaction.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        command.EntityId = donor.Id;
        command.NewValues = new { donor.Id, donor.CompanyName, donor.Status, donor.SubmissionSource };

        var adminUserIds = await _users.GetActiveUserIdsByRoleAsync("Admin", cancellationToken);

        foreach (var adminUserId in adminUserIds)
        {
            await _notificationService.CreateAsync(
                adminUserId,
                "New donor pending review",
                $"{donor.CompanyName} was captured and is awaiting approval.",
                NotificationType.NewDonorPendingReview,
                donor.Id,
                nameof(Donor),
                cancellationToken);
        }

        // Re-read through the same projection GetDonorByIdQueryHandler uses, rather
        // than mapping the in-memory graph — keeps a single source of truth for the
        // DonorDetailDto projection.
        return await _donors.GetDetailByIdAsync(donor.Id, cancellationToken)
            ?? throw new InvalidOperationException(
                $"Donor {donor.Id} could not be re-read immediately after being created.");
    }
}
