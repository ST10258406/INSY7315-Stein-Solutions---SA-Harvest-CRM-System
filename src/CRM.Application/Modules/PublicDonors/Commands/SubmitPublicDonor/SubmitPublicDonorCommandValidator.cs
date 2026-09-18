namespace CRM.Application.Modules.PublicDonors.Commands.SubmitPublicDonor;

using CRM.Application.Common.Files;
using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Donors.Commands.CreateDonor;
using CRM.Application.Modules.Donors.Dtos;
using FluentValidation;

public class SubmitPublicDonorCommandValidator : AbstractValidator<SubmitPublicDonorCommand>
{
    public SubmitPublicDonorCommandValidator(ILookupRepository lookups, IUserRepository users)
    {
        RuleFor(x => x.Request).NotNull();

        When(x => x.Request is not null, () =>
        {
            // SubmitPublicDonorRequest IS-A CreateDonorRequest (it just adds
            // Signature) — the cast lets every donor field-level rule run through
            // the exact same validator POST /donors uses, instead of duplicating
            // them here.
            RuleFor(x => (CreateDonorRequest)x.Request).SetValidator(new CreateDonorRequestValidator(lookups, users));

            RuleFor(x => x.Request.Signature).NotNull();
            When(x => x.Request.Signature is not null, () =>
            {
                RuleFor(x => x.Request.Signature.ImageBase64)
                    .NotEmpty()
                    .Must(dataUri => Base64PngDecoder.TryDecode(dataUri, out _))
                    .WithMessage("signature.imageBase64 must be a valid PNG data URI (data:image/png;base64,...) no larger than 5MB.");
            });
        });
    }
}
