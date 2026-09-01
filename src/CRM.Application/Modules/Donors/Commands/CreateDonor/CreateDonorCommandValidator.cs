namespace CRM.Application.Modules.Donors.Commands.CreateDonor;

using CRM.Application.Common.Interfaces;
using FluentValidation;

public class CreateDonorCommandValidator : AbstractValidator<CreateDonorCommand>
{
    public CreateDonorCommandValidator(ILookupRepository lookups, IUserRepository users)
    {
        RuleFor(x => x.Request).NotNull();

        When(x => x.Request is not null, () =>
        {
            RuleFor(x => x.Request.Company).NotNull();
            When(x => x.Request.Company is not null, () =>
            {
                RuleFor(x => x.Request.Company.CompanyName).NotEmpty();
                RuleFor(x => x.Request.Company.Website).NotEmpty();
                RuleFor(x => x.Request.Company.RegisteredCompanyName).NotEmpty();
                RuleFor(x => x.Request.Company.TradingName).NotEmpty();
                RuleFor(x => x.Request.Company.CompanyRegistrationNumber).NotEmpty();

                RuleFor(x => x.Request.Company.CompanyTypeId)
                    .GreaterThan((short)0)
                    .MustAsync(async (id, ct) => await lookups.CompanyTypeExistsActiveAsync(id, ct))
                    .WithMessage("Invalid company type.");

                RuleFor(x => x.Request.Company.EntityTypeId)
                    .GreaterThan((short)0)
                    .MustAsync(async (id, ct) => await lookups.EntityTypeExistsActiveAsync(id, ct))
                    .WithMessage("Invalid entity type.");

                // South African income tax number rule: must not start with '4'.
                // Mirrored at the DB layer via chk_donors_tax_number (DonorConfiguration.cs) —
                // confirmed present, so this is belt-and-braces for a clean 400 instead of a
                // raw CHECK-constraint violation surfacing as a 500.
                RuleFor(x => x.Request.Company.IncomeTaxNumber)
                    .NotEmpty()
                    .Must(tin => tin is null || !tin.StartsWith('4'))
                    .WithMessage("Income tax number cannot start with 4.");
            });

            RuleFor(x => x.Request.PrimaryContact).NotNull();
            When(x => x.Request.PrimaryContact is not null, () =>
            {
                RuleFor(x => x.Request.PrimaryContact.Name).NotEmpty();
                RuleFor(x => x.Request.PrimaryContact.Email).NotEmpty().EmailAddress();
                RuleFor(x => x.Request.PrimaryContact.Phone).NotEmpty();
            });

            When(x => x.Request.MarketingContact is not null, () =>
            {
                RuleFor(x => x.Request.MarketingContact!.Name).NotEmpty();
                RuleFor(x => x.Request.MarketingContact!.Email).NotEmpty().EmailAddress();
                RuleFor(x => x.Request.MarketingContact!.Phone).NotEmpty();
            });

            When(x => x.Request.AccountsContact is not null, () =>
            {
                RuleFor(x => x.Request.AccountsContact!.Name).NotEmpty();
                RuleFor(x => x.Request.AccountsContact!.Email).NotEmpty().EmailAddress();
                RuleFor(x => x.Request.AccountsContact!.Phone).NotEmpty();
            });

            RuleFor(x => x.Request.LegalAddress).NotNull();
            When(x => x.Request.LegalAddress is not null, () =>
            {
                RuleFor(x => x.Request.LegalAddress.StreetAddress).NotEmpty();
                RuleFor(x => x.Request.LegalAddress.Suburb).NotEmpty();
                RuleFor(x => x.Request.LegalAddress.City).NotEmpty();
                RuleFor(x => x.Request.LegalAddress.PostalCode).NotEmpty();

                RuleFor(x => x.Request.LegalAddress.ProvinceId)
                    .GreaterThan((short)0)
                    .MustAsync(async (id, ct) => await lookups.ProvinceExistsActiveAsync(id, ct))
                    .WithMessage("Invalid province.");
            });

            RuleFor(x => x.Request.Donations).NotNull();
            When(x => x.Request.Donations is not null, () =>
            {
                RuleFor(x => x.Request.Donations.CollectionAddress).NotEmpty();

                RuleFor(x => x.Request.Donations.FrequencyId)
                    .GreaterThan((short)0)
                    .MustAsync(async (id, ct) => await lookups.DonationFrequencyExistsActiveAsync(id, ct))
                    .WithMessage("Invalid donation frequency.");

                RuleFor(x => x.Request.Donations.TypeIds)
                    .NotEmpty()
                    .MustAsync(async (ids, ct) =>
                    {
                        var distinct = ids.Distinct().ToList();
                        var matchCount = await lookups.CountActiveDonationTypesAsync(distinct, ct);
                        return matchCount == distinct.Count;
                    })
                    .WithMessage("One or more donation types are invalid.");

                RuleFor(x => x.Request.Donations.RegionIds)
                    .NotEmpty()
                    .MustAsync(async (ids, ct) =>
                    {
                        var distinct = ids.Distinct().ToList();
                        var matchCount = await lookups.CountActiveOperationalRegionsAsync(distinct, ct);
                        return matchCount == distinct.Count;
                    })
                    .WithMessage("One or more operational regions are invalid.");
            });

            When(x => x.Request.Compliance?.BbbeeStatusId is not null, () =>
            {
                RuleFor(x => x.Request.Compliance!.BbbeeStatusId!.Value)
                    .MustAsync(async (id, ct) => await lookups.BbbeeStatusExistsActiveAsync(id, ct))
                    .WithMessage("Invalid BBBEE status.");
            });

            When(x => x.Request.Crm?.RelationshipManagerId is not null, () =>
            {
                RuleFor(x => x.Request.Crm!.RelationshipManagerId!.Value)
                    .MustAsync(async (id, ct) => await users.ExistsAndActiveAsync(id, ct))
                    .WithMessage("Invalid relationship manager.");
            });
        });
    }
}
