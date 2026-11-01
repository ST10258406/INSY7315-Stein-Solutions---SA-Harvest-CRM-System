namespace CRM.Application.Modules.Vetting.Uploads.Commands.UploadEvidencePhoto;

using CRM.Application.Common.Files;
using FluentValidation;

public class UploadEvidencePhotoCommandValidator : AbstractValidator<UploadEvidencePhotoCommand>
{
    public UploadEvidencePhotoCommandValidator()
    {
        RuleFor(x => x.SubmissionId).NotEmpty();
        RuleFor(x => x.FileStream).NotNull().WithMessage("A file is required.");
        RuleFor(x => x.OriginalFileName).NotEmpty();

        // Same magic-byte / Content-Type / extension / 5MB checks as donor documents, before any blob call.
        RuleFor(x => x)
            .Custom((x, context) =>
            {
                var error = UploadedFileInspector.Validate(
                    x.FileStream, x.FileSizeBytes, x.ContentType, x.OriginalFileName, VettingUploadSlots.PhotoMimeTypes);
                if (error is not null) context.AddFailure(nameof(x.FileStream), error);
            })
            .When(x => x.FileStream is not null);
    }
}
