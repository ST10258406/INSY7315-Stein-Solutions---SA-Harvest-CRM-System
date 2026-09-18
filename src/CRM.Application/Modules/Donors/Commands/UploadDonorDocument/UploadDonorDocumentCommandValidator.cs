namespace CRM.Application.Modules.Donors.Commands.UploadDonorDocument;

using CRM.Domain.Constants;
using CRM.Domain.Enums;
using FluentValidation;

public class UploadDonorDocumentCommandValidator : AbstractValidator<UploadDonorDocumentCommand>
{
    public UploadDonorDocumentCommandValidator()
    {
        RuleFor(x => x.DonorId).NotEmpty();

        RuleFor(x => x.DocumentType)
            .Must(t => Enum.TryParse<DocumentType>(t, out _))
            .WithMessage("documentType must be 'BBBEECertificate' or 'Signature'.");

        // Validated before any blob upload call — a bad mime type or an oversized
        // file shouldn't cost a network round trip to Azure just to get rejected.
        RuleFor(x => x)
            .Must(x => Enum.TryParse<DocumentType>(x.DocumentType, out var type)
                && DocumentUploadLimits.AllowedMimeTypes.TryGetValue(type, out var mimes)
                && mimes.Contains(x.ContentType))
            .WithMessage("Unsupported file type for this document type.")
            .When(x => Enum.TryParse<DocumentType>(x.DocumentType, out _));

        RuleFor(x => x.FileSizeBytes)
            .GreaterThan(0)
            .LessThanOrEqualTo(DocumentUploadLimits.MaxFileSizeBytes)
            .WithMessage("File exceeds 5MB limit.");

        RuleFor(x => x.OriginalFileName).NotEmpty();
    }
}
