namespace CRM.Application.Modules.Donors.Commands.UploadDonorDocument;

using CRM.Application.Common.Files;
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

        // Validated before any blob upload call — a bad file shouldn't cost a network
        // round trip to Azure just to get rejected. Content is sniffed from its magic
        // bytes and cross-checked against the declared type and extension; the size
        // limit uses the real stream length.
        RuleFor(x => x.FileStream)
            .NotNull()
            .WithMessage("A file is required.");

        RuleFor(x => x)
            .Custom((x, context) =>
            {
                var type = Enum.Parse<DocumentType>(x.DocumentType);
                var error = UploadedFileInspector.Validate(
                    x.FileStream, x.FileSizeBytes, x.ContentType, x.OriginalFileName, type);
                if (error is not null) context.AddFailure(nameof(x.FileStream), error);
            })
            .When(x => Enum.TryParse<DocumentType>(x.DocumentType, out _) && x.FileStream is not null);

        RuleFor(x => x.OriginalFileName).NotEmpty();
    }
}
