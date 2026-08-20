namespace CRM.Application.Modules.Donors.Commands.UpdateDonor;

using CRM.Application.Common.Interfaces;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

public class UpdateDonorCommandValidator : AbstractValidator<UpdateDonorCommand>
{
    public UpdateDonorCommandValidator(IApplicationDbContext context)
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Request).NotNull();

        When(x => x.Request is not null, () =>
        {
            When(x => x.Request.Company is not null, () =>
            {
                RuleFor(x => x.Request.Company!.CompanyName)
                    .NotEmpty()
                    .When(x => x.Request.Company!.CompanyName is not null);

                RuleFor(x => x.Request.Company!.Website)
                    .NotEmpty()
                    .When(x => x.Request.Company!.Website is not null);

                RuleFor(x => x.Request.Company!.RegisteredCompanyName)
                    .NotEmpty()
                    .When(x => x.Request.Company!.RegisteredCompanyName is not null);

                RuleFor(x => x.Request.Company!.TradingName)
                    .NotEmpty()
                    .When(x => x.Request.Company!.TradingName is not null);

                RuleFor(x => x.Request.Company!.CompanyRegistrationNumber)
                    .NotEmpty()
                    .When(x => x.Request.Company!.CompanyRegistrationNumber is not null);

                // South African income tax number rule: must not start with '4'.
                // Same rule as CreateDonor, mirrored at the DB layer via
                // chk_donors_tax_number (DonorConfiguration.cs).
                RuleFor(x => x.Request.Company!.IncomeTaxNumber)
                    .Must(tin => tin is null || !tin.StartsWith('4'))
                    .WithMessage("Income tax number cannot start with 4.");

                RuleFor(x => x.Request.Company!.CompanyTypeId)
                    .MustAsync(async (id, ct) => id is null || await context.LookupCompanyTypes.AnyAsync(l => l.Id == id && l.IsActive, ct))
                    .WithMessage("Invalid company type.");

                RuleFor(x => x.Request.Company!.EntityTypeId)
                    .MustAsync(async (id, ct) => id is null || await context.LookupEntityTypes.AnyAsync(l => l.Id == id && l.IsActive, ct))
                    .WithMessage("Invalid entity type.");
            });

            When(x => x.Request.PrimaryContact is not null, () =>
            {
                RuleFor(x => x.Request.PrimaryContact!.Name)
                    .NotEmpty()
                    .When(x => x.Request.PrimaryContact!.Name is not null);

                RuleFor(x => x.Request.PrimaryContact!.Email)
                    .NotEmpty().EmailAddress()
                    .When(x => x.Request.PrimaryContact!.Email is not null);

                RuleFor(x => x.Request.PrimaryContact!.Phone)
                    .NotEmpty()
                    .When(x => x.Request.PrimaryContact!.Phone is not null);
            });

            When(x => x.Request.MarketingContact is not null, () =>
            {
                RuleFor(x => x.Request.MarketingContact!.Name)
                    .NotEmpty()
                    .When(x => x.Request.MarketingContact!.Name is not null);

                RuleFor(x => x.Request.MarketingContact!.Email)
                    .NotEmpty().EmailAddress()
                    .When(x => x.Request.MarketingContact!.Email is not null);

                RuleFor(x => x.Request.MarketingContact!.Phone)
                    .NotEmpty()
                    .When(x => x.Request.MarketingContact!.Phone is not null);
            });

            When(x => x.Request.AccountsContact is not null, () =>
            {
                RuleFor(x => x.Request.AccountsContact!.Name)
                    .NotEmpty()
                    .When(x => x.Request.AccountsContact!.Name is not null);

                RuleFor(x => x.Request.AccountsContact!.Email)
                    .NotEmpty().EmailAddress()
                    .When(x => x.Request.AccountsContact!.Email is not null);

                RuleFor(x => x.Request.AccountsContact!.Phone)
                    .NotEmpty()
                    .When(x => x.Request.AccountsContact!.Phone is not null);
            });

            When(x => x.Request.LegalAddress is not null, () =>
            {
                RuleFor(x => x.Request.LegalAddress!.StreetAddress)
                    .NotEmpty()
                    .When(x => x.Request.LegalAddress!.StreetAddress is not null);

                RuleFor(x => x.Request.LegalAddress!.Suburb)
                    .NotEmpty()
                    .When(x => x.Request.LegalAddress!.Suburb is not null);

                RuleFor(x => x.Request.LegalAddress!.City)
                    .NotEmpty()
                    .When(x => x.Request.LegalAddress!.City is not null);

                RuleFor(x => x.Request.LegalAddress!.PostalCode)
                    .NotEmpty()
                    .When(x => x.Request.LegalAddress!.PostalCode is not null);

                RuleFor(x => x.Request.LegalAddress!.ProvinceId)
                    .MustAsync(async (id, ct) => id is null || await context.LookupProvinces.AnyAsync(l => l.Id == id && l.IsActive, ct))
                    .WithMessage("Invalid province.");
            });

            When(x => x.Request.Donations is not null, () =>
            {
                RuleFor(x => x.Request.Donations!.CollectionAddress)
                    .NotEmpty()
                    .When(x => x.Request.Donations!.CollectionAddress is not null);

                RuleFor(x => x.Request.Donations!.FrequencyId)
                    .MustAsync(async (id, ct) => id is null || await context.LookupDonationFrequencies.AnyAsync(l => l.Id == id && l.IsActive, ct))
                    .WithMessage("Invalid donation frequency.");

                When(x => x.Request.Donations!.TypeIds is not null, () =>
                {
                    RuleFor(x => x.Request.Donations!.TypeIds!)
                        .NotEmpty()
                        .WithMessage("If donation types are supplied, at least one is required.")
                        .MustAsync(async (ids, ct) =>
                        {
                            var distinct = ids.Distinct().ToList();
                            var matchCount = await context.LookupDonationTypes.CountAsync(l => distinct.Contains(l.Id) && l.IsActive, ct);
                            return matchCount == distinct.Count;
                        })
                        .WithMessage("One or more donation types are invalid.");
                });

                When(x => x.Request.Donations!.RegionIds is not null, () =>
                {
                    RuleFor(x => x.Request.Donations!.RegionIds!)
                        .NotEmpty()
                        .WithMessage("If operational regions are supplied, at least one is required.")
                        .MustAsync(async (ids, ct) =>
                        {
                            var distinct = ids.Distinct().ToList();
                            var matchCount = await context.LookupOperationalRegions.CountAsync(l => distinct.Contains(l.Id) && l.IsActive, ct);
                            return matchCount == distinct.Count;
                        })
                        .WithMessage("One or more operational regions are invalid.");
                });
            });

            When(x => x.Request.Compliance is not null, () =>
            {
                RuleFor(x => x.Request.Compliance!.BbbeeStatusId)
                    .MustAsync(async (id, ct) => id is null || await context.LookupBbbeeStatuses.AnyAsync(l => l.Id == id && l.IsActive, ct))
                    .WithMessage("Invalid BBBEE status.");
            });

            When(x => x.Request.Crm is not null, () =>
            {
                RuleFor(x => x.Request.Crm!.RelationshipManagerId)
                    .MustAsync(async (id, ct) => id is null || await context.Users.AnyAsync(u => u.Id == id && u.IsActive, ct))
                    .WithMessage("Invalid relationship manager.");
            });
        });
    }
}
