namespace CRM.Application.Modules.SavedFilters;

/// <summary>
/// The donor-list query string a saved filter may hold. It is replayed in the browser as
/// /donors?{queryString}, so only the keys useDonorListFilters reads are allowed. Paging keys are
/// dropped so a saved filter always opens on page 1.
/// </summary>
public static class SavedFilterQueryString
{
    public const int MaxLength = 500;

    public static readonly IReadOnlySet<string> AllowedKeys = new HashSet<string>(StringComparer.Ordinal)
    {
        "search", "status", "companyTypeId", "regionCode", "donationTypeId",
        "donationFrequencyId", "followUpBefore", "sortBy", "sortDir",
    };

    private static readonly HashSet<string> DroppedKeys = new(StringComparer.Ordinal) { "page", "pageSize" };

    private static IEnumerable<string[]> Pairs(string? raw) =>
        (raw ?? string.Empty).TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Split('=', 2))
            .Where(kv => !DroppedKeys.Contains(kv[0]));

    /// <summary>Query string without the leading '?' or paging keys.</summary>
    public static string Normalise(string? raw) =>
        string.Join('&', Pairs(raw).Select(kv => string.Join('=', kv)));

    /// <summary>Keys that are not allowed, after normalising (empty when valid).</summary>
    public static IReadOnlyList<string> UnknownKeys(string? raw) =>
        Pairs(raw).Select(kv => kv[0]).Where(k => !AllowedKeys.Contains(k)).Distinct().ToList();

    /// <summary>True when a key appears twice or a pair has no '=': both mean a hand-crafted string.</summary>
    public static bool IsMalformed(string? raw)
    {
        var pairs = Pairs(raw).ToList();
        return pairs.Any(kv => kv.Length != 2) || pairs.Select(kv => kv[0]).Distinct().Count() != pairs.Count;
    }
}
