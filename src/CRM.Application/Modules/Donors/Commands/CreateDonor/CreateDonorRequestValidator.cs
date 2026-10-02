namespace CRM.Application.Modules.Donors.Commands.CreateDonor;

using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Donors.Dtos;
using CRM.Application.Modules.Donors.Validation;
using FluentValidation;

/// <summary>
/// Field-level rules for the donor structure shared by POST /donors
/// (CreateDonorCommand) and POST /public/donors/submit (SubmitPublicDonorCommand
/// — SubmitPublicDonorRequest extends CreateDonorRequest with just a signature).
/// Extracted out of CreateDonorCommandValidator so both callers validate through
/// the exact same rules instead of duplicating them.
/// </summary>
public class CreateDonorRequestValidator : AbstractValidator<CreateDonorRequest>
{
    public CreateDonorRequestValidator(ILookupRepository lookups, IUserRepository users)
    {
        RuleFor(x => x.Company).NotNull();
        When(x => x.Company is not null, () =>
        {
            RuleFor(x => x.Company.CompanyName).NotEmpty().MaxLen(DonorFieldRules.CompanyNameMax);
            RuleFor(x => x.Company.Website).NotEmpty().MaxLen(DonorFieldRules.WebsiteMax).HttpUrl();
            RuleFor(x => x.Company.RegisteredCompanyName).NotEmpty().MaxLen(DonorFieldRules.CompanyNameMax);
            RuleFor(x => x.Company.TradingName).NotEmpty().MaxLen(DonorFieldRules.CompanyNameMax);
            RuleFor(x => x.Company.CompanyRegistrationNumber).NotEmpty().MaxLen(DonorFieldRules.RegistrationNumberMax);

            RuleFor(x => x.Company.CompanyTypeId)
                .GreaterThan((short)0)
                .MustAsync(async (id, ct) => await lookups.CompanyTypeExistsActiveAsync(id, ct))
                .WithMessage("Invalid company type.");

            RuleFor(x => x.Company.EntityTypeId)
                .GreaterThan((short)0)
                .MustAsync(async (id, ct) => await lookups.EntityTypeExistsActiveAsync(id, ct))
                .WithMessage("Invalid entity type.");

            // South African income tax number rule: must not start with '4'.
            // Mirrored at the DB layer via chk_donors_tax_number (DonorConfiguration.cs) —
            // confirmed present, so this is belt-and-braces for a clean 400 instead of a
            // raw CHECK-constraint violation surfacing as a 500.
            RuleFor(x => x.Company.IncomeTaxNumber)
                .NotEmpty()
                .MaxLen(DonorFieldRules.TaxNumberMax)
                .Must(tin => tin is null || !tin.StartsWith('4'))
                .WithMessage("Income tax number cannot start with 4.");
        });

        RuleFor(x => x.PrimaryContact).NotNull();
        When(x => x.PrimaryContact is not null, () =>
        {
            RuleFor(x => x.PrimaryContact.Name).NotEmpty().MaxLen(DonorFieldRules.ContactNameMax);
            RuleFor(x => x.PrimaryContact.JobTitle).MaxLen(DonorFieldRules.JobTitleMax);
            RuleFor(x => x.PrimaryContact.Email).NotEmpty().MaximumLength(DonorFieldRules.EmailMax).EmailAddress();
            RuleFor(x => x.PrimaryContact.Phone).NotEmpty().MaxLen(DonorFieldRules.PhoneMax).PhoneNumber();
        });

        When(x => x.MarketingContact is not null, () =>
        {
            RuleFor(x => x.MarketingContact!.Name).NotEmpty().MaxLen(DonorFieldRules.ContactNameMax);
            RuleFor(x => x.MarketingContact!.JobTitle).MaxLen(DonorFieldRules.JobTitleMax);
            RuleFor(x => x.MarketingContact!.Email).NotEmpty().MaximumLength(DonorFieldRules.EmailMax).EmailAddress();
            RuleFor(x => x.MarketingContact!.Phone).NotEmpty().MaxLen(DonorFieldRules.PhoneMax).PhoneNumber();
        });

        When(x => x.AccountsContact is not null, () =>
        {
            RuleFor(x => x.AccountsContact!.Name).NotEmpty().MaxLen(DonorFieldRules.ContactNameMax);
            RuleFor(x => x.AccountsContact!.JobTitle).MaxLen(DonorFieldRules.JobTitleMax);
            RuleFor(x => x.AccountsContact!.Email).NotEmpty().MaximumLength(DonorFieldRules.EmailMax).EmailAddress();
            RuleFor(x => x.AccountsContact!.Phone).NotEmpty().MaxLen(DonorFieldRules.PhoneMax).PhoneNumber();
        });

        RuleFor(x => x.LegalAddress).NotNull();
        When(x => x.LegalAddress is not null, () =>
        {
            RuleFor(x => x.LegalAddress.StreetAddress).NotEmpty().MaxLen(DonorFieldRules.StreetMax);
            RuleFor(x => x.LegalAddress.Suburb).NotEmpty().MaxLen(DonorFieldRules.SuburbMax);
            RuleFor(x => x.LegalAddress.City).NotEmpty().MaxLen(DonorFieldRules.CityMax);
            RuleFor(x => x.LegalAddress.PostalCode).NotEmpty().MaxLen(DonorFieldRules.PostalCodeMax).PostalCode();

            RuleFor(x => x.LegalAddress.ProvinceId)
                .GreaterThan((short)0)
                .MustAsync(async (id, ct) => await lookups.ProvinceExistsActiveAsync(id, ct))
                .WithMessage("Invalid province.");
        });

        RuleFor(x => x.Donations).NotNull();
        When(x => x.Donations is not null, () =>
        {
            RuleFor(x => x.Donations.CollectionAddress).NotEmpty().MaxLen(DonorFieldRules.MaxLongTextLength);
            RuleFor(x => x.Donations.OperationsLogisticsDetails).MaxLen(DonorFieldRules.MaxLongTextLength);

            RuleFor(x => x.Donations.FrequencyId)
                .GreaterThan((short)0)
                .MustAsync(async (id, ct) => await lookups.DonationFrequencyExistsActiveAsync(id, ct))
                .WithMessage("Invalid donation frequency.");

            RuleFor(x => x.Donations.TypeIds)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .BoundedIdList()
                .MustAsync(async (ids, ct) =>
                {
                    var distinct = ids.Distinct().ToList();
                    var matchCount = await lookups.CountActiveDonationTypesAsync(distinct, ct);
                    return matchCount == distinct.Count;
                })
                .WithMessage("One or more donation types are invalid.");

            RuleFor(x => x.Donations.RegionIds)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .BoundedIdList()
                .MustAsync(async (ids, ct) =>
                {
                    var distinct = ids.Distinct().ToList();
                    var matchCount = await lookups.CountActiveOperationalRegionsAsync(distinct, ct);
                    return matchCount == distinct.Count;
                })
                .WithMessage("One or more operational regions are invalid.");
        });

        When(x => x.Compliance?.BbbeeStatusId is not null, () =>
        {
            RuleFor(x => x.Compliance!.BbbeeStatusId!.Value)
                .MustAsync(async (id, ct) => await lookups.BbbeeStatusExistsActiveAsync(id, ct))
                .WithMessage("Invalid BBBEE status.");
        });

        When(x => x.Crm is not null, () =>
        {
            RuleFor(x => x.Crm!.ImpactReportingPreferences).MaxLen(DonorFieldRules.MaxLongTextLength);
            RuleFor(x => x.Crm!.AdditionalInformation).MaxLen(DonorFieldRules.MaxLongTextLength);
        });

        When(x => x.Crm?.RelationshipManagerId is not null, () =>
        {
            RuleFor(x => x.Crm!.RelationshipManagerId!.Value)
                .MustAsync(async (id, ct) => await users.ExistsAndActiveAsync(id, ct))
                .WithMessage("Invalid relationship manager.");
        });
    }
}
