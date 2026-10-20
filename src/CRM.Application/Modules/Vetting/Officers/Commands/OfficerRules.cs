namespace CRM.Application.Modules.Vetting.Officers.Commands;

using FluentValidation;

/// <summary>Field rules shared by CreateOfficer and UpdateOfficer.</summary>
internal static class OfficerRules
{
    public static IRuleBuilderOptions<T, string?> OfficerFullName<T>(this IRuleBuilder<T, string?> rule)
        => rule.NotEmpty().MaximumLength(100);

    public static IRuleBuilderOptions<T, string?> OfficerInitials<T>(this IRuleBuilder<T, string?> rule)
        => rule.NotEmpty().MaximumLength(5)
            .Matches("^[A-Z]+$").WithMessage("initials must be uppercase letters.");

    public static IRuleBuilderOptions<T, string?> OfficerAssignedRegion<T>(this IRuleBuilder<T, string?> rule)
        => rule.NotEmpty().MaximumLength(50);
}
