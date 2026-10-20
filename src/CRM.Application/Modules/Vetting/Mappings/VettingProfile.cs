namespace CRM.Application.Modules.Vetting.Mappings;

using AutoMapper;
using CRM.Application.Modules.Vetting.Officers.Dtos;
using CRM.Application.Modules.Vetting.SiteVisits.Dtos;
using CRM.Application.Modules.Vetting.Submissions.Dtos;
using CRM.Domain.Entities.Vetting;

public class VettingProfile : Profile
{
    public VettingProfile()
    {
        CreateMap<VettingOfficer, VettingOfficerDto>();

        // DonorCompanyName / OfficerInitials flatten from the navigations.
        CreateMap<VettingSiteVisit, VettingSiteVisitDto>()
            .ForMember(d => d.VisitStatus, o => o.MapFrom(s => s.VisitStatus.ToString()));

        // Request → tracked row. Keys, timestamps and the upload-only blob paths are never written from a body.
        CreateMap<VettingAssessmentDetailsDto, VettingAssessmentDetails>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.CreatedAt, o => o.Ignore())
            .ForMember(d => d.UpdatedAt, o => o.Ignore())
            .ForMember(d => d.SubmissionId, o => o.Ignore())
            .ForMember(d => d.Submission, o => o.Ignore())
            .ForMember(d => d.NpoCertificatePath, o => o.Ignore())
            .ForMember(d => d.PboCertificatePath, o => o.Ignore())
            .ForMember(d => d.KitchenStoragePhotosPath, o => o.Ignore())
            .ForMember(d => d.FacilityPhotosPath, o => o.Ignore())
            .ForMember(d => d.CertificatesPath, o => o.Ignore());
        CreateMap<VettingAssessmentDetails, VettingAssessmentDetailsDto>();

        CreateMap<VettingAssessmentDetails, VettingFilesDto>()
            .ForMember(d => d.NpoCertificate, o => o.MapFrom(s => s.NpoCertificatePath != null))
            .ForMember(d => d.PboCertificate, o => o.MapFrom(s => s.PboCertificatePath != null))
            .ForMember(d => d.KitchenStoragePhotos, o => o.MapFrom(s => s.KitchenStoragePhotosPath != null))
            .ForMember(d => d.FacilityPhotos, o => o.MapFrom(s => s.FacilityPhotosPath != null))
            .ForMember(d => d.Certificates, o => o.MapFrom(s => s.CertificatesPath != null));

        CreateMap<VettingFlag, VettingFlagDto>()
            .ForMember(d => d.Source, o => o.MapFrom(s => s.Source.ToString()));
        CreateMap<VettingEvidencePhoto, VettingPhotoDto>()
            .ForMember(d => d.Category, o => o.MapFrom(s => s.Category.ToString()));

        // DonorCompanyName / OfficerFullName flatten from the navigations.
        CreateMap<VettingSubmission, VettingSubmissionListItemDto>()
            .ForMember(d => d.ApprovalState, o => o.MapFrom(s => s.ApprovalState.ToString()))
            .ForMember(d => d.ActiveFlagCount, o => o.MapFrom(s => s.Flags.Count(f => f.IsActive)));
        CreateMap<VettingSubmission, VettingSubmissionDto>()
            .ForMember(d => d.ApprovalState, o => o.MapFrom(s => s.ApprovalState.ToString()))
            .ForMember(d => d.AdminDecision, o => o.MapFrom(s => s.AdminDecision.HasValue ? s.AdminDecision.Value.ToString() : null))
            .ForMember(d => d.Flags, o => o.MapFrom(s => s.Flags.Where(f => f.IsActive).OrderBy(f => f.CreatedAt)))
            .ForMember(d => d.Photos, o => o.MapFrom(s => s.Photos.OrderBy(p => p.ShotIndex)))
            .ForMember(d => d.Files, o => o.MapFrom(s => s.Details));
    }
}
