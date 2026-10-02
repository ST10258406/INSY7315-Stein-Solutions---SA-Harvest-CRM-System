namespace CRM.Application.Modules.PublicDonors.Commands.SubmitPublicDonorDocument;

using CRM.Application.Common.Files;
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

        // Same content/size rules as the internal upload path (UploadedFileInspector) —
        // validated before any blob upload call, so a bad file never costs a network
        // round trip to Azure just to get rejected.
        RuleFor(x => x.FileStream)
            .NotNull()
            .WithMessage("A file is required.");

        RuleFor(x => x)
            .Custom((x, context) =>
            {
                var error = UploadedFileInspector.Validate(
                    x.FileStream, x.FileSizeBytes, x.ContentType, x.OriginalFileName, DocumentType.BBBEECertificate);
                if (error is not null) context.AddFailure(nameof(x.FileStream), error);
            })
            .When(x => x.FileStream is not null);

        RuleFor(x => x.OriginalFileName).NotEmpty();
    }
}
