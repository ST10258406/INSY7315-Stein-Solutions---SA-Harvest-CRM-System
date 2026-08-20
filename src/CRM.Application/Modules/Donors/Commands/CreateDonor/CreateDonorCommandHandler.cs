namespace CRM.Application.Modules.Donors.Commands.CreateDonor;

using AutoMapper;
using AutoMapper.QueryableExtensions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Donors.Dtos;
using CRM.Domain.Entities;
using CRM.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

public class CreateDonorCommandHandler : IRequestHandler<CreateDonorCommand, DonorDetailDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly INotificationService _notificationService;
    private readonly ICurrentUserService _currentUserService;

    public CreateDonorCommandHandler(
        IApplicationDbContext context,
        IMapper mapper,
        INotificationService notificationService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _mapper = mapper;
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

        _context.Donors.Add(donor);

        var approval = new DonorApproval
        {
            Id = Guid.NewGuid(),
            DonorId = donor.Id,
            RequestedByUserId = currentUserId,
            Status = ApprovalStatus.Pending
        };
        _context.DonorApprovals.Add(approval);

        // One SaveChangesAsync call — donor, its children, and the approval
        // commit together in a single transaction.
        await _context.SaveChangesAsync(cancellationToken);

        command.EntityId = donor.Id;
        command.NewValues = new { donor.Id, donor.CompanyName, donor.Status, donor.SubmissionSource };

        var adminUserIds = await _context.Users
            .Where(u => u.IsActive && u.UserRoles.Any(ur => ur.Role.Name == "Admin"))
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        foreach (var adminUserId in adminUserIds)
        {
            await _notificationService.CreateAsync(
                adminUserId,
                NotificationType.NewDonorPendingReview,
                "New donor pending review",
                $"{donor.CompanyName} was captured and is awaiting approval.",
                donor.Id,
                nameof(Donor));
        }

        // Re-query through the same ProjectTo shape GetDonorByIdQueryHandler uses,
        // rather than mapping the in-memory graph — keeps a single source of truth
        // for the DonorDetailDto projection.
        return await _context.Donors
            .AsNoTracking()
            .Where(d => d.Id == donor.Id)
            .ProjectTo<DonorDetailDto>(_mapper.ConfigurationProvider)
            .FirstAsync(cancellationToken);
    }
}
