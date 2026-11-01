namespace CRM.Application.Common.Validation;

using FluentValidation;

/// <summary>
/// Inclusive from/to date filter shared by the list queries the calendar (Issue 104) reads a month
/// at a time from. The window is capped so a range can't be used to pull every row ever.
/// </summary>
internal static class DateWindowRules
{
    public const int MaxRangeDays = 62;

    public static IRuleBuilderOptions<T, T> DateWindow<T>(
        this IRuleBuilder<T, T> rule, Func<T, DateOnly?> after, Func<T, DateOnly?> before, string afterName, string beforeName)
        => rule
            .Must(x => after(x)!.Value <= before(x)!.Value)
            .WithMessage($"{afterName} must be on or before {beforeName}.")
            .Must(x => before(x)!.Value.DayNumber - after(x)!.Value.DayNumber <= MaxRangeDays)
            .WithMessage($"The {afterName}–{beforeName} range can be at most {MaxRangeDays} days.")
            .When(x => after(x).HasValue && before(x).HasValue)
            .WithName(afterName);
}
