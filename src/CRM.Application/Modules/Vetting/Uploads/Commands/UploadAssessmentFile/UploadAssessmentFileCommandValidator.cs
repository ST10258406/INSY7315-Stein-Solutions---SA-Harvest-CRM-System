namespace CRM.Application.Modules.Vetting.Uploads.Commands.UploadAssessmentFile;

using CRM.Application.Common.Files;
using FluentValidation;

public class UploadAssessmentFileCommandValidator : AbstractValidator<UploadAssessmentFileCommand>
{
    public UploadAssessmentFileCommandValidator()
    {
        RuleFor(x => x.SubmissionId).NotEmpty();
        RuleFor(x => x.FileStream).NotNull().WithMessage("A file is required.");
        RuleFor(x => x.OriginalFileName).NotEmpty();

        // An unknown slot is a 404 from the handler, so only known slots get the file checks here.
        RuleFor(x => x)
            .Custom((x, context) =>
            {
                var error = UploadedFileInspector.Validate(
                    x.FileStream, x.FileSizeBytes, x.ContentType, x.OriginalFileName, VettingUploadSlots.Files[x.Slot].MimeTypes);
                if (error is not null) context.AddFailure(nameof(x.FileStream), error);
            })
            .When(x => x.FileStream is not null && VettingUploadSlots.Files.ContainsKey(x.Slot));
    }
}
