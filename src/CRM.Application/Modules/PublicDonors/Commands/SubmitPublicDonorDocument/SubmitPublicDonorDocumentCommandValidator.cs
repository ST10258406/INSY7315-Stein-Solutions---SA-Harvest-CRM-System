namespace CRM.Application.Modules.PublicDonors.Commands.SubmitPublicDonorDocument;

using CRM.Domain.Constants;
using CRM.Domain.Enums;
using FluentValidation;

public class SubmitPublicDonorDocumentCommandValidator : AbstractValidator<SubmitPublicDonorDocumentCommand>
{
    public SubmitPublicDonorDocumentCommandValidator()
    {
        RuleFor(x => x.SessionToken).NotEmpty();

        // Unlike the internal upload endpoint, this one accepts exactly one
        // document type — no other value is ever valid here.
        RuleFor(x => x.DocumentType)
            .Equal(nameof(DocumentType.BBBEECertificate))
            .WithMessage("documentType must be 'BBBEECertificate'.");

        // Same MIME/size rules as the internal upload path (DocumentUploadLimits) —
        // validated before any blob upload call, so a bad file never costs a
        // network round trip to Azure just to get rejected.
        RuleFor(x => x.ContentType)
            .Must(mime => DocumentUploadLimits.AllowedMimeTypes[DocumentType.BBBEECertificate].Contains(mime))
            .WithMessage("Unsupported file type for BBBEE certificates.");

        RuleFor(x => x.FileSizeBytes)
            .GreaterThan(0)
            .LessThanOrEqualTo(DocumentUploadLimits.MaxFileSizeBytes)
            .WithMessage("File exceeds 5MB limit.");

        RuleFor(x => x.OriginalFileName).NotEmpty();
    }
}
