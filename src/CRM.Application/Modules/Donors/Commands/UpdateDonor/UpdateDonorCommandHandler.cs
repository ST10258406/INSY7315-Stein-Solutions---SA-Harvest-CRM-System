namespace CRM.Application.Modules.Donors.Commands.UpdateDonor;

using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Donors.Dtos;
using CRM.Domain.Entities;
using CRM.Domain.Enums;
using MediatR;

public class UpdateDonorCommandHandler : IRequestHandler<UpdateDonorCommand, DonorDetailDto>
{
    private readonly IDonorRepository _donors;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateDonorCommandHandler(IDonorRepository donors, IUnitOfWork unitOfWork)
    {
        _donors = donors;
        _unitOfWork = unitOfWork;
    }

    public async Task<DonorDetailDto> Handle(UpdateDonorCommand command, CancellationToken cancellationToken)
    {
        // Tracked aggregate — this is a write, the persistence layer needs to track
        // changes so SaveChangesAsync knows what to update.
        var donor = await _donors.GetForUpdateAsync(command.Id, cancellationToken);

        if (donor is null)
            throw new NotFoundException(nameof(Donor), command.Id);

        var req = command.Request;

        if (req.Company is not null)
        {
            if (req.Company.CompanyName is not null) donor.CompanyName = req.Company.CompanyName;
            if (req.Company.CompanyTypeId.HasValue) donor.CompanyTypeId = req.Company.CompanyTypeId.Value;
            if (req.Company.Website is not null) donor.Website = req.Company.Website;
            if (req.Company.RegisteredCompanyName is not null) donor.RegisteredCompanyName = req.Company.RegisteredCompanyName;
            if (req.Company.TradingName is not null) donor.TradingName = req.Company.TradingName;
            if (req.Company.EntityTypeId.HasValue) donor.EntityTypeId = req.Company.EntityTypeId.Value;
            if (req.Company.CompanyRegistrationNumber is not null) donor.CompanyRegistrationNumber = req.Company.CompanyRegistrationNumber;
            if (req.Company.IncomeTaxNumber is not null) donor.IncomeTaxNumber = req.Company.IncomeTaxNumber;
        }

        if (req.PrimaryContact is not null)
        {
            var contact = donor.Contacts.FirstOrDefault(c => c.ContactType == ContactType.Primary);
            if (contact is null)
            {
                contact = new DonorContact { DonorId = donor.Id, ContactType = ContactType.Primary };
                donor.Contacts.Add(contact);
            }

            if (req.PrimaryContact.Name is not null) contact.Name = req.PrimaryContact.Name;
            if (req.PrimaryContact.JobTitle is not null) contact.JobTitle = req.PrimaryContact.JobTitle;
            if (req.PrimaryContact.Phone is not null) contact.Phone = req.PrimaryContact.Phone;
            if (req.PrimaryContact.Email is not null) contact.Email = req.PrimaryContact.Email;
        }

        if (req.MarketingContact is not null)
        {
            var contact = donor.Contacts.FirstOrDefault(c => c.ContactType == ContactType.Marketing);
            if (contact is null)
            {
                contact = new DonorContact { DonorId = donor.Id, ContactType = ContactType.Marketing };
                donor.Contacts.Add(contact);
            }

            if (req.MarketingContact.Name is not null) contact.Name = req.MarketingContact.Name;
            if (req.MarketingContact.JobTitle is not null) contact.JobTitle = req.MarketingContact.JobTitle;
            if (req.MarketingContact.Phone is not null) contact.Phone = req.MarketingContact.Phone;
            if (req.MarketingContact.Email is not null) contact.Email = req.MarketingContact.Email;
        }

        if (req.AccountsContact is not null)
        {
            var contact = donor.Contacts.FirstOrDefault(c => c.ContactType == ContactType.Accounts);
            if (contact is null)
            {
                contact = new DonorContact { DonorId = donor.Id, ContactType = ContactType.Accounts };
                donor.Contacts.Add(contact);
            }

            if (req.AccountsContact.Name is not null) contact.Name = req.AccountsContact.Name;
            if (req.AccountsContact.JobTitle is not null) contact.JobTitle = req.AccountsContact.JobTitle;
            if (req.AccountsContact.Phone is not null) contact.Phone = req.AccountsContact.Phone;
            if (req.AccountsContact.Email is not null) contact.Email = req.AccountsContact.Email;
        }

        if (req.LegalAddress is not null)
        {
            if (donor.LegalAddress is null)
            {
                donor.LegalAddress = new DonorLegalAddress { DonorId = donor.Id };
            }

            if (req.LegalAddress.StreetAddress is not null) donor.LegalAddress.StreetAddress = req.LegalAddress.StreetAddress;
            if (req.LegalAddress.Suburb is not null) donor.LegalAddress.Suburb = req.LegalAddress.Suburb;
            if (req.LegalAddress.City is not null) donor.LegalAddress.City = req.LegalAddress.City;
            if (req.LegalAddress.ProvinceId.HasValue) donor.LegalAddress.ProvinceId = req.LegalAddress.ProvinceId.Value;
            if (req.LegalAddress.PostalCode is not null) donor.LegalAddress.PostalCode = req.LegalAddress.PostalCode;
        }

        if (req.Donations is not null)
        {
            if (req.Donations.FrequencyId.HasValue) donor.DonationFrequencyId = req.Donations.FrequencyId.Value;
            if (req.Donations.CollectionAddress is not null) donor.CollectionAddress = req.Donations.CollectionAddress;
            if (req.Donations.OperationsLogisticsDetails is not null) donor.OperationsLogisticsDetails = req.Donations.OperationsLogisticsDetails;

            // Junction rows: if new ids are supplied, replace the set entirely rather
            // than diffing old vs new — simpler and correct at this scale. Removing an
            // item from these navigation collections is enough for EF to delete the
            // orphaned row, since DonorId is a required part of the composite key.
            if (req.Donations.RegionIds is not null)
            {
                donor.OperationalRegions.Clear();
                foreach (var regionId in req.Donations.RegionIds.Distinct())
                    donor.OperationalRegions.Add(new DonorOperationalRegion { DonorId = donor.Id, OperationalRegionId = regionId });
            }

            if (req.Donations.TypeIds is not null)
            {
                donor.DonationTypes.Clear();
                foreach (var typeId in req.Donations.TypeIds.Distinct())
                    donor.DonationTypes.Add(new DonorDonationType { DonorId = donor.Id, DonationTypeId = typeId });
            }
        }

        if (req.Compliance is not null && req.Compliance.BbbeeStatusId.HasValue)
        {
            donor.BbbeeStatusId = req.Compliance.BbbeeStatusId;
        }

        if (req.Crm is not null)
        {
            if (req.Crm.RelationshipManagerId.HasValue) donor.RelationshipManagerId = req.Crm.RelationshipManagerId;
            if (req.Crm.MarketingConsent.HasValue)
            {
                donor.MarketingConsent = req.Crm.MarketingConsent.Value;
                if (req.Crm.MarketingConsent.Value && donor.MarketingConsentDate is null)
                    donor.MarketingConsentDate = DateTime.UtcNow;
            }
            if (req.Crm.ImpactReportingPreferences is not null) donor.ImpactReportingPreferences = req.Crm.ImpactReportingPreferences;
            if (req.Crm.AdditionalInformation is not null) donor.AdditionalInformation = req.Crm.AdditionalInformation;
        }

        // Do NOT set donor.UpdatedAt manually here — UpdatedAtInterceptor handles it
        // automatically on SaveChangesAsync. Do NOT write to audit_logs here either —
        // AuditBehaviour does that after this handler returns.

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        command.EntityId = donor.Id;
        command.NewValues = new { donor.Id, donor.CompanyName, donor.Status };

        return await _donors.GetDetailByIdAsync(donor.Id, cancellationToken)
            ?? throw new InvalidOperationException(
                $"Donor {donor.Id} could not be re-read immediately after being updated.");
    }
}
