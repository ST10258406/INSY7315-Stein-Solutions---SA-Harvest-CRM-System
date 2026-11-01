namespace CRM.Domain.Constants;

/// <summary>
/// Valid answers for the vetting assessment form, spelled exactly as in the design doc.
/// Stored as these display strings (data, not workflow), so validators read from here
/// and no code branches on them. Provinces must match the LookupSeeder province seed.
/// </summary>
public static class VettingOptions
{
    public static readonly IReadOnlyList<string> Provinces =
    [
        "Eastern Cape", "Free State", "Gauteng", "KwaZulu-Natal", "Limpopo",
        "Mpumalanga", "North West", "Northern Cape", "Western Cape"
    ];

    public static readonly IReadOnlyList<string> CoreFocusAreas =
        ["Feeding", "ECD", "Education", "Health", "Shelter", "Skills training", "Care & support"];

    public static readonly IReadOnlyList<string> TargetPopulations =
        ["Children", "Youth", "Women", "Elderly", "Disabled", "Homeless", "Families"];

    public static readonly IReadOnlyList<string> DistributionChannels =
        ["Direct to beneficiaries", "Through other organisations", "Both"];

    public static readonly IReadOnlyList<string> Races = ["African", "Coloured", "Indian/Asian", "White"];

    public static readonly IReadOnlyList<string> Genders = ["Female", "Male", "Other"];

    public static readonly IReadOnlyList<string> AgeGroups = ["0–5", "6–12", "13–18", "19–35", "36–59", "60+"];

    public static readonly IReadOnlyList<string> FeedingFrequencies =
        ["Daily", "Weekdays only", "Weekly", "Twice a week", "Monthly"];

    public static readonly IReadOnlyList<string> RelianceLevels = ["Low", "Moderate", "High", "Total"];

    public static readonly IReadOnlyList<string> TransportCapacities =
        ["Own vehicle", "Hired transport", "None — delivery required"];

    public static readonly IReadOnlyList<string> Meals = ["Breakfast", "Lunch", "Dinner"];

    public static readonly IReadOnlyList<string> Days = ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"];

    public static readonly IReadOnlyList<string> StorageTypes = ["Dry", "Chilled", "Frozen"];

    public static readonly IReadOnlyList<string> InfrastructureChecks =
        ["Roofing", "Fencing", "Lighting", "Gas safety", "Electrics"];
}
