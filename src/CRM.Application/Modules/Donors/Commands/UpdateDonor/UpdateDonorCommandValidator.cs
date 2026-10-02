namespace CRM.Application.Modules.Donors.Commands.UpdateDonor;

using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Donors.Validation;
using FluentValidation;

public class UpdateDonorCommandValidator : AbstractValidator<UpdateDonorCommand>
{
    public UpdateDonorCommandValidator(ILookupRepository lookups, IUserRepository users)
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Request).NotNull();

        When(x => x.Request is not null, () =>
        {
            When(x => x.Request.Company is not null, () =>
            {
                RuleFor(x => x.Request.Company!.CompanyName)
                    .MaxLen(DonorFieldRules.CompanyNameMax)
                    .NotEmpty()
                    .When(x => x.Request.Company!.CompanyName is not null);

                RuleFor(x => x.Request.Company!.Website)
                    .MaxLen(DonorFieldRules.WebsiteMax).HttpUrl()
                    .NotEmpty()
                    .When(x => x.Request.Company!.Website is not null);

                RuleFor(x => x.Request.Company!.RegisteredCompanyName)
                    .MaxLen(DonorFieldRules.CompanyNameMax)
                    .NotEmpty()
                    .When(x => x.Request.Company!.RegisteredCompanyName is not null);

                RuleFor(x => x.Request.Company!.TradingName)
                    .MaxLen(DonorFieldRules.CompanyNameMax)
                    .NotEmpty()
                    .When(x => x.Request.Company!.TradingName is not null);

                RuleFor(x => x.Request.Company!.CompanyRegistrationNumber)
                    .MaxLen(DonorFieldRules.RegistrationNumberMax)
                    .NotEmpty()
                    .When(x => x.Request.Company!.CompanyRegistrationNumber is not null);

                // South African income tax number rule: must not start with '4'.
                // Same rule as CreateDonor, mirrored at the DB layer via
                // chk_donors_tax_number (DonorConfiguration.cs).
                RuleFor(x => x.Request.Company!.IncomeTaxNumber)
                    .MaxLen(DonorFieldRules.TaxNumberMax)
                    .Must(tin => tin is null || !tin.StartsWith('4'))
                    .WithMessage("Income tax number cannot start with 4.");

                RuleFor(x => x.Request.Company!.CompanyTypeId)
                    .MustAsync(async (id, ct) => id is null || await lookups.CompanyTypeExistsActiveAsync(id!.Value, ct))
                    .WithMessage("Invalid company type.");

                RuleFor(x => x.Request.Company!.EntityTypeId)
                    .MustAsync(async (id, ct) => id is null || await lookups.EntityTypeExistsActiveAsync(id!.Value, ct))
                    .WithMessage("Invalid entity type.");
            });

            When(x => x.Request.PrimaryContact is not null, () =>
            {
                RuleFor(x => x.Request.PrimaryContact!.Name)
                    .MaxLen(DonorFieldRules.ContactNameMax)
                    .NotEmpty()
                    .When(x => x.Request.PrimaryContact!.Name is not null);

                RuleFor(x => x.Request.PrimaryContact!.JobTitle)
                    .MaxLen(DonorFieldRules.JobTitleMax);

                RuleFor(x => x.Request.PrimaryContact!.Email)
                    .MaxLen(DonorFieldRules.EmailMax)
                    .NotEmpty().EmailAddress()
                    .When(x => x.Request.PrimaryContact!.Email is not null);

                RuleFor(x => x.Request.PrimaryContact!.Phone)
                    .MaxLen(DonorFieldRules.PhoneMax).PhoneNumber()
                    .NotEmpty()
                    .When(x => x.Request.PrimaryContact!.Phone is not null);
            });

            When(x => x.Request.MarketingContact is not null, () =>
            {
                RuleFor(x => x.Request.MarketingContact!.Name)
                    .MaxLen(DonorFieldRules.ContactNameMax)
                    .NotEmpty()
                    .When(x => x.Request.MarketingContact!.Name is not null);

                RuleFor(x => x.Request.MarketingContact!.JobTitle)
                    .MaxLen(DonorFieldRules.JobTitleMax);

                RuleFor(x => x.Request.MarketingContact!.Email)
                    .MaxLen(DonorFieldRules.EmailMax)
                    .NotEmpty().EmailAddress()
                    .When(x => x.Request.MarketingContact!.Email is not null);

                RuleFor(x => x.Request.MarketingContact!.Phone)
                    .MaxLen(DonorFieldRules.PhoneMax).PhoneNumber()
                    .NotEmpty()
                    .When(x => x.Request.MarketingContact!.Phone is not null);
            });

            When(x => x.Request.AccountsContact is not null, () =>
            {
                RuleFor(x => x.Request.AccountsContact!.Name)
                    .MaxLen(DonorFieldRules.ContactNameMax)
                    .NotEmpty()
                    .When(x => x.Request.AccountsContact!.Name is not null);

                RuleFor(x => x.Request.AccountsContact!.JobTitle)
                    .MaxLen(DonorFieldRules.JobTitleMax);

                RuleFor(x => x.Request.AccountsContact!.Email)
                    .MaxLen(DonorFieldRules.EmailMax)
                    .NotEmpty().EmailAddress()
                    .When(x => x.Request.AccountsContact!.Email is not null);

                RuleFor(x => x.Request.AccountsContact!.Phone)
                    .MaxLen(DonorFieldRules.PhoneMax).PhoneNumber()
                    .NotEmpty()
                    .When(x => x.Request.AccountsContact!.Phone is not null);
            });

            When(x => x.Request.LegalAddress is not null, () =>
            {
                RuleFor(x => x.Request.LegalAddress!.StreetAddress)
                    .MaxLen(DonorFieldRules.StreetMax)
                    .NotEmpty()
                    .When(x => x.Request.LegalAddress!.StreetAddress is not null);

                RuleFor(x => x.Request.LegalAddress!.Suburb)
                    .MaxLen(DonorFieldRules.SuburbMax)
                    .NotEmpty()
                    .When(x => x.Request.LegalAddress!.Suburb is not null);

                RuleFor(x => x.Request.LegalAddress!.City)
                    .MaxLen(DonorFieldRules.CityMax)
                    .NotEmpty()
                    .When(x => x.Request.LegalAddress!.City is not null);

                RuleFor(x => x.Request.LegalAddress!.PostalCode)
                    .MaxLen(DonorFieldRules.PostalCodeMax).PostalCode()
                    .NotEmpty()
                    .When(x => x.Request.LegalAddress!.PostalCode is not null);

                RuleFor(x => x.Request.LegalAddress!.ProvinceId)
                    .MustAsync(async (id, ct) => id is null || await lookups.ProvinceExistsActiveAsync(id!.Value, ct))
                    .WithMessage("Invalid province.");
            });

            When(x => x.Request.Donations is not null, () =>
            {
                RuleFor(x => x.Request.Donations!.CollectionAddress)
                    .MaxLen(DonorFieldRules.MaxLongTextLength)
                    .NotEmpty()
                    .When(x => x.Request.Donations!.CollectionAddress is not null);

                RuleFor(x => x.Request.Donations!.OperationsLogisticsDetails)
                    .MaxLen(DonorFieldRules.MaxLongTextLength);

                RuleFor(x => x.Request.Donations!.FrequencyId)
                    .MustAsync(async (id, ct) => id is null || await lookups.DonationFrequencyExistsActiveAsync(id!.Value, ct))
                    .WithMessage("Invalid donation frequency.");

                When(x => x.Request.Donations!.TypeIds is not null, () =>
                {
                    RuleFor(x => x.Request.Donations!.TypeIds!)
                        .Cascade(CascadeMode.Stop)
                        .NotEmpty()
                        .WithMessage("If donation types are supplied, at least one is required.")
                        .BoundedIdList()
                        .MustAsync(async (ids, ct) =>
                        {
                            var distinct = ids.Distinct().ToList();
                            var matchCount = await lookups.CountActiveDonationTypesAsync(distinct, ct);
                            return matchCount == distinct.Count;
                        })
                        .WithMessage("One or more donation types are invalid.");
                });

                When(x => x.Request.Donations!.RegionIds is not null, () =>
                {
                    RuleFor(x => x.Request.Donations!.RegionIds!)
                        .Cascade(CascadeMode.Stop)
                        .NotEmpty()
                        .WithMessage("If operational regions are supplied, at least one is required.")
                        .BoundedIdList()
                        .MustAsync(async (ids, ct) =>
                        {
                            var distinct = ids.Distinct().ToList();
                            var matchCount = await lookups.CountActiveOperationalRegionsAsync(distinct, ct);
                            return matchCount == distinct.Count;
                        })
                        .WithMessage("One or more operational regions are invalid.");
                });
            });

            When(x => x.Request.Compliance is not null, () =>
            {
                RuleFor(x => x.Request.Compliance!.BbbeeStatusId)
                    .MustAsync(async (id, ct) => id is null || await lookups.BbbeeStatusExistsActiveAsync(id!.Value, ct))
                    .WithMessage("Invalid BBBEE status.");
            });

            When(x => x.Request.Crm is not null, () =>
            {
                RuleFor(x => x.Request.Crm!.ImpactReportingPreferences).MaxLen(DonorFieldRules.MaxLongTextLength);
                RuleFor(x => x.Request.Crm!.AdditionalInformation).MaxLen(DonorFieldRules.MaxLongTextLength);
            });

            When(x => x.Request.Crm is not null, () =>
            {
                RuleFor(x => x.Request.Crm!.RelationshipManagerId)
                    .MustAsync(async (id, ct) => id is null || await users.ExistsAndActiveAsync(id!.Value, ct))
                    .WithMessage("Invalid relationship manager.");
            });
        });
    }
}
