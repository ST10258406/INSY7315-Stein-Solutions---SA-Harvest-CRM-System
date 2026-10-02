namespace CRM.Application.Modules.Donors.Validation;

using System.Text.RegularExpressions;
using FluentValidation;

/// <summary>
/// Field-level rules shared by the create, update and public-submission donor
/// validators so the limits cannot drift apart. Maximum lengths mirror the database
/// columns (DonorConfiguration, DonorContactConfiguration, DonorLegalAddressConfiguration);
/// the database is the source of truth.
/// </summary>
public static class DonorFieldRules
{
    public const int MaxIdListSize = 20;

    // Free-text columns have no database cap; these keep a hostile payload bounded.
    public const int MaxLongTextLength = 4000;

    public const int CompanyNameMax = 255;
    public const int WebsiteMax = 500;
    public const int RegistrationNumberMax = 100;
    public const int TaxNumberMax = 100;
    public const int ContactNameMax = 255;
    public const int JobTitleMax = 255;
    public const int PhoneMax = 20;
    public const int EmailMax = 255;
    public const int StreetMax = 255;
    public const int SuburbMax = 100;
    public const int CityMax = 100;
    public const int PostalCodeMax = 10;

    // South African numbers: national form (0 + 9 digits) or international (+27 / 0027 + 9 digits).
    // Spaces, hyphens and brackets are accepted as separators and stripped before matching.
    private static readonly Regex SeparatorPattern = new(@"[ ()\-]", RegexOptions.Compiled);
    private static readonly Regex PhonePattern = new(@"^(\+27|0027|0)[0-9]{9}$", RegexOptions.Compiled);

    // South African postal codes are exactly four digits.
    private static readonly Regex PostalCodePattern = new(@"^[0-9]{4}$", RegexOptions.Compiled);

    public static IRuleBuilderOptions<T, string?> MaxLen<T>(this IRuleBuilder<T, string?> rule, int max) =>
        rule.MaximumLength(max).WithMessage($"{{PropertyName}} must be {max} characters or fewer.");

    public static IRuleBuilderOptions<T, string?> HttpUrl<T>(this IRuleBuilder<T, string?> rule) =>
        rule.Must(v => string.IsNullOrEmpty(v) ||
                (Uri.TryCreate(v, UriKind.Absolute, out var uri) &&
                 (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)))
            .WithMessage("{PropertyName} must be a valid http or https URL.");

    public static IRuleBuilderOptions<T, string?> PhoneNumber<T>(this IRuleBuilder<T, string?> rule) =>
        rule.Must(v => string.IsNullOrEmpty(v) ||
                PhonePattern.IsMatch(SeparatorPattern.Replace(v, string.Empty)))
            .WithMessage("{PropertyName} must be a valid South African phone number (e.g. 082 000 0000 or +27 82 000 0000).");

    public static IRuleBuilderOptions<T, string?> PostalCode<T>(this IRuleBuilder<T, string?> rule) =>
        rule.Must(v => string.IsNullOrEmpty(v) || PostalCodePattern.IsMatch(v))
            .WithMessage("{PropertyName} must be a valid 4-digit South African postal code.");

    /// <summary>Caps the list size and requires every id to be positive.</summary>
    public static IRuleBuilderOptions<T, List<short>> BoundedIdList<T>(this IRuleBuilder<T, List<short>> rule) =>
        rule.Must(ids => ids.Count <= MaxIdListSize)
            .WithMessage($"{{PropertyName}} can contain at most {MaxIdListSize} items.")
            .Must(ids => ids.TrueForAll(i => i > 0))
            .WithMessage("{PropertyName} contains an invalid id.");
}
