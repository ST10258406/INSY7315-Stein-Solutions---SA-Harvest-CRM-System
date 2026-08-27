namespace CRM.Application.Common.Interfaces;

using CRM.Application.Common.Models;
using CRM.Application.Modules.Lookups.Queries.GetCodedLookup;
using CRM.Application.Modules.Lookups.Queries.GetLookup;

/// <summary>
/// Read-only access to the reference/lookup tables. All query logic (EF Core, AutoMapper
/// projections, active-filtering and sort order) lives in the Infrastructure implementation.
/// </summary>
public interface ILookupRepository
{
    /// <summary>Active lookup rows for the given simple lookup type, ordered by sort order.</summary>
    Task<List<LookupDto>> GetActiveAsync(LookupType type, CancellationToken cancellationToken = default);

    /// <summary>Active lookup rows for the given coded lookup type, ordered by sort order.</summary>
    Task<List<CodedLookupDto>> GetActiveCodedAsync(CodedLookupType type, CancellationToken cancellationToken = default);

    Task<bool> CompanyTypeExistsActiveAsync(short id, CancellationToken cancellationToken = default);

    Task<bool> EntityTypeExistsActiveAsync(short id, CancellationToken cancellationToken = default);

    Task<bool> ProvinceExistsActiveAsync(short id, CancellationToken cancellationToken = default);

    Task<bool> DonationFrequencyExistsActiveAsync(short id, CancellationToken cancellationToken = default);

    Task<bool> BbbeeStatusExistsActiveAsync(short id, CancellationToken cancellationToken = default);

    /// <summary>Number of active donation types whose id is contained in <paramref name="ids"/>.</summary>
    Task<int> CountActiveDonationTypesAsync(IEnumerable<short> ids, CancellationToken cancellationToken = default);

    /// <summary>Number of active operational regions whose id is contained in <paramref name="ids"/>.</summary>
    Task<int> CountActiveOperationalRegionsAsync(IEnumerable<short> ids, CancellationToken cancellationToken = default);
}
