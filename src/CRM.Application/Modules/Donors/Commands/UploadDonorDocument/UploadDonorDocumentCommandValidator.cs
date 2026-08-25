namespace CRM.Application.Modules.Donors.Commands.UploadDonorDocument;

using CRM.Domain.Enums;
using FluentValidation;

public class UploadDonorDocumentCommandValidator : AbstractValidator<UploadDonorDocumentCommand>
{
    private const long MaxFileSizeBytes = 5 * 1024 * 1024;

    // Mirrors the Section "Constraints" table on Issue 34 — Signature is
    // deliberately narrower (png only) than BBBEECertificate.
    private static readonly Dictionary<DocumentType, string[]> AllowedMimeTypes = new()
    {
        [DocumentType.BBBEECertificate] = ["application/pdf", "image/jpeg", "image/png"],
        [DocumentType.Signature] = ["image/png"]
    };

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
                && AllowedMimeTypes.TryGetValue(type, out var mimes)
                && mimes.Contains(x.ContentType))
            .WithMessage("Unsupported file type for this document type.")
            .When(x => Enum.TryParse<DocumentType>(x.DocumentType, out _));

        RuleFor(x => x.FileSizeBytes)
            .GreaterThan(0)
            .LessThanOrEqualTo(MaxFileSizeBytes)
            .WithMessage("File exceeds 5MB limit.");

        RuleFor(x => x.OriginalFileName).NotEmpty();
    }
}
