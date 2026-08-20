using AutoMapper;
using CRM.Application.Common.Models;
using CRM.Application.Modules.Donors.Dtos;
using CRM.Domain.Entities;
using CRM.Domain.Entities.Lookups;
using CRM.Domain.Enums;

namespace CRM.Application.Modules.Donors.Mappings;

public class DonorMappingProfile : Profile
{
    public DonorMappingProfile()
    {
        CreateMap<Donor, DonorListItemDto>()
            .ForMember(d => d.CompanyType, o => o.MapFrom(s => s.CompanyType.Name))
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.SubmissionSource, o => o.MapFrom(s => s.SubmissionSource.ToString()))
            .ForMember(d => d.RelationshipManager, o => o.MapFrom(s => s.RelationshipManager))
            .ForMember(d => d.LastInteractionDate, o => o.MapFrom(s =>
                s.InteractionLogs.OrderByDescending(i => i.CreatedAt).Select(i => (DateTime?)i.CreatedAt).FirstOrDefault()))
            .ForMember(d => d.LastInteractionType, o => o.MapFrom(s =>
                s.InteractionLogs.OrderByDescending(i => i.CreatedAt).Select(i => (string?)i.InteractionType.ToString()).FirstOrDefault()))
            .ForMember(d => d.OperationalRegions, o => o.MapFrom(s => s.OperationalRegions.Select(r => r.OperationalRegion.Code)))
            .ForMember(d => d.DonationFrequency, o => o.MapFrom(s => s.DonationFrequency.Name))
            .ForMember(d => d.DonationTypes, o => o.MapFrom(s => s.DonationTypes.Select(t => t.DonationType.Name)));

        CreateMap<User, RelationshipManagerDto>()
            .ForMember(d => d.FullName, o => o.MapFrom(s => s.FirstName + " " + s.LastName));

        // GetDonorById — full nested detail projection (Issue 31).
        CreateMap<Donor, DonorDetailDto>()
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.SubmissionSource, o => o.MapFrom(s => s.SubmissionSource.ToString()))
            .ForMember(d => d.Company, o => o.MapFrom(s => s))
            .ForMember(d => d.PrimaryContact, o => o.MapFrom(s => s.Contacts.FirstOrDefault(c => c.ContactType == ContactType.Primary)))
            .ForMember(d => d.MarketingContact, o => o.MapFrom(s => s.Contacts.FirstOrDefault(c => c.ContactType == ContactType.Marketing)))
            .ForMember(d => d.AccountsContact, o => o.MapFrom(s => s.Contacts.FirstOrDefault(c => c.ContactType == ContactType.Accounts)))
            .ForMember(d => d.LegalAddress, o => o.MapFrom(s => s.LegalAddress))
            .ForMember(d => d.Donations, o => o.MapFrom(s => s))
            .ForMember(d => d.Compliance, o => o.MapFrom(s => s))
            .ForMember(d => d.Crm, o => o.MapFrom(s => s));

        CreateMap<Donor, DonorCompanyDto>()
            .ForMember(d => d.CompanyType, o => o.MapFrom(s => s.CompanyType))
            .ForMember(d => d.EntityType, o => o.MapFrom(s => s.EntityType));

        CreateMap<DonorContact, DonorContactDto>();

        CreateMap<DonorLegalAddress, DonorLegalAddressDto>()
            .ForMember(d => d.StreetNameNumber, o => o.MapFrom(s => s.StreetAddress))
            .ForMember(d => d.Province, o => o.MapFrom(s => s.Province));

        CreateMap<Donor, DonorDonationsDto>()
            .ForMember(d => d.Frequency, o => o.MapFrom(s => s.DonationFrequency))
            .ForMember(d => d.Types, o => o.MapFrom(s => s.DonationTypes.Select(t => t.DonationType)))
            .ForMember(d => d.OperationalRegions, o => o.MapFrom(s => s.OperationalRegions.Select(r => r.OperationalRegion)));

        CreateMap<Donor, DonorComplianceDto>()
            .ForMember(d => d.BbbeeStatus, o => o.MapFrom(s => s.BbbeeStatus))
            .ForMember(d => d.Documents, o => o.MapFrom(s => s.Documents));

        CreateMap<DonorDocument, DonorDocumentDto>()
            .ForMember(d => d.DocumentType, o => o.MapFrom(s => s.DocumentType.ToString()))
            .ForMember(d => d.OriginalFileName, o => o.MapFrom(s => s.FileName))
            .ForMember(d => d.UploadedAt, o => o.MapFrom(s => s.CreatedAt));

        CreateMap<Donor, DonorCrmDto>()
            .ForMember(d => d.RelationshipManager, o => o.MapFrom(s => s.RelationshipManager));

        CreateMap<LookupCompanyType, LookupDto>();
        CreateMap<LookupEntityType, LookupDto>();
        CreateMap<LookupDonationFrequency, LookupDto>();
        CreateMap<LookupDonationType, LookupDto>();
        CreateMap<LookupBbbeeStatus, LookupDto>();
        CreateMap<LookupProvince, ProvinceDto>();
        CreateMap<LookupOperationalRegion, RegionDto>();
    }
}
