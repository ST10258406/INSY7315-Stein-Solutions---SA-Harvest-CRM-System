namespace CRM.Application.Modules.Vetting.Submissions.Validation;

using CRM.Application.Modules.Donors.Validation;
using CRM.Application.Modules.Vetting.Submissions.Dtos;
using CRM.Domain.Constants;
using FluentValidation;

/// <summary>
/// The assessment-form rules shared by CreateSubmission and UpdateSubmission, section by section.
/// Lengths mirror VettingAssessmentDetailsConfiguration; allowed values come from VettingOptions.
/// </summary>
public class VettingAssessmentRules : AbstractValidator<VettingAssessmentDetailsDto>
{
    public VettingAssessmentRules()
    {
        // 1 Organisation & Contact
        RuleFor(x => x.LegalName).NotEmpty().MaxLen(200);
        RuleFor(x => x.ContactName).NotEmpty().MaxLen(150);
        RuleFor(x => x.ContactEmail).NotEmpty().EmailAddress().MaxLen(100);
        RuleFor(x => x.ContactPhone).NotEmpty().MaxLen(30);
        RuleFor(x => x.Website).HttpUrl().MaxLen(150);

        // 2 Location
        RuleFor(x => x.AddressPhysical).MaxLen(DonorFieldRules.MaxLongTextLength);
        RuleFor(x => x.AddressDetail).MaxLen(DonorFieldRules.MaxLongTextLength);
        RuleFor(x => x.Province).OneOf(VettingOptions.Provinces);
        RuleFor(x => x.What3Words).NotEmpty().MaxLen(100)
            .Matches(@"^///[a-z]+\.[a-z]+\.[a-z]+$").WithMessage("{PropertyName} must look like ///word.word.word.");

        // 3 Programmes & Services
        RuleFor(x => x.CoreFocusAreas).NotEmpty().SubsetOf(VettingOptions.CoreFocusAreas);
        RuleFor(x => x.TargetPopulation).NotEmpty().SubsetOf(VettingOptions.TargetPopulations);
        RuleFor(x => x.ProgrammesSummary).NotEmpty().MaxLen(DonorFieldRules.MaxLongTextLength);
        RuleFor(x => x.DistributionChannel).OneOf(VettingOptions.DistributionChannels);

        // 4 Staffing
        RuleFor(x => x.FullTimeFemales).GreaterThanOrEqualTo(0);
        RuleFor(x => x.FullTimeMales).GreaterThanOrEqualTo(0);
        RuleFor(x => x.VolunteersCount).GreaterThanOrEqualTo(0);

        // 5 Registration
        RuleFor(x => x.IsRegisteredNpo).Answered();
        RuleFor(x => x.IsRegisteredDsd).Answered();

        // 6 Demographics
        RuleFor(x => x.BeneficiaryRaces).NotEmpty().SubsetOf(VettingOptions.Races);
        RuleFor(x => x.BeneficiaryGenders).NotEmpty().SubsetOf(VettingOptions.Genders);
        RuleFor(x => x.BeneficiaryAgeGroups).NotEmpty().SubsetOf(VettingOptions.AgeGroups);

        // 7 Feeding Operation
        RuleFor(x => x.FeedingFrequency).OneOf(VettingOptions.FeedingFrequencies);
        RuleFor(x => x.SaHarvestReliance).OneOf(VettingOptions.RelianceLevels);
        RuleFor(x => x.TransportCapacity).OneOf(VettingOptions.TransportCapacities);
        RuleFor(x => x.TotalPeopleServed).GreaterThanOrEqualTo(0);
        RuleFor(x => x.FemalesServed).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MalesServed).GreaterThanOrEqualTo(0);
        RuleFor(x => x.AfricanServed).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ColouredServed).GreaterThanOrEqualTo(0);
        RuleFor(x => x.IndianAsianServed).GreaterThanOrEqualTo(0);
        RuleFor(x => x.WhiteServed).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MealsProvided).NotEmpty().SubsetOf(VettingOptions.Meals);
        RuleFor(x => x.FeedingDays).NotEmpty().SubsetOf(VettingOptions.Days);
        RuleFor(x => x.LastFedDate)
            .Must(d => d <= DateOnly.FromDateTime(DateTime.UtcNow))
            .When(x => x.LastFedDate.HasValue)
            .WithMessage("{PropertyName} cannot be in the future.");

        // 8 Facilities & Hygiene — infrastructureChecks may be empty (none passed)
        RuleFor(x => x.FoodStorageTypes).NotEmpty().SubsetOf(VettingOptions.StorageTypes);
        RuleFor(x => x.KitchenClean).Answered();
        RuleFor(x => x.WaterAccess).Answered();
        RuleFor(x => x.ToiletFacilities).Answered();
        RuleFor(x => x.PestFree).Answered();
        RuleFor(x => x.InfrastructureChecks).NotNull().SubsetOf(VettingOptions.InfrastructureChecks);

        // 9 Access & Security
        RuleFor(x => x.DeliveryAccessEase).Answered();
        RuleFor(x => x.ParkingAndSecurity).Answered();
        RuleFor(x => x.PoliceStationProximity).MaxLen(100);

        // 10 Capacity Notes
        RuleFor(x => x.OfficerComments).MaxLen(DonorFieldRules.MaxLongTextLength);
        RuleFor(x => x.ProposalCapabilities).MaxLen(DonorFieldRules.MaxLongTextLength);
        RuleFor(x => x.DigitalCapabilities).MaxLen(DonorFieldRules.MaxLongTextLength);

        // 11 Documents
        RuleFor(x => x.SlaSigned).Answered();
        RuleFor(x => x.ConsentFormSigned).Answered();
        RuleFor(x => x.PolicyDocumentPresent).Answered();
    }
}

internal static class VettingRuleExtensions
{
    public static IRuleBuilderOptions<T, bool?> Answered<T>(this IRuleBuilder<T, bool?> rule) =>
        rule.NotNull().WithMessage("Please answer Yes or No.");

    public static IRuleBuilderOptions<T, string> OneOf<T>(this IRuleBuilder<T, string> rule, IReadOnlyList<string> options) =>
        rule.Must(options.Contains).WithMessage($"{{PropertyName}} must be one of: {string.Join(", ", options)}.");

    /// <summary>Every item from <paramref name="options"/>, each at most once (which also bounds the list size).</summary>
    public static IRuleBuilderOptions<T, List<string>> SubsetOf<T>(this IRuleBuilder<T, List<string>> rule, IReadOnlyList<string> options) =>
        rule.Must(items => items is null || (items.All(options.Contains) && items.Distinct().Count() == items.Count))
            .WithMessage($"{{PropertyName}} may only contain, once each: {string.Join(", ", options)}.");
}
