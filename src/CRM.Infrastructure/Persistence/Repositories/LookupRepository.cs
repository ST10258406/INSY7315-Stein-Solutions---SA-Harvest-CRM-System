namespace CRM.Infrastructure.Persistence.Repositories;

using AutoMapper;
using AutoMapper.QueryableExtensions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Common.Models;
using CRM.Application.Modules.Lookups.Queries.GetCodedLookup;
using CRM.Application.Modules.Lookups.Queries.GetLookup;
using Microsoft.EntityFrameworkCore;

public class LookupRepository : ILookupRepository
{
    private readonly CrmDbContext _context;
    private readonly IMapper _mapper;

    public LookupRepository(CrmDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public Task<List<LookupDto>> GetActiveAsync(LookupType type, CancellationToken cancellationToken = default)
    {
        return type switch
        {
            LookupType.CompanyTypes => _context.LookupCompanyTypes.AsNoTracking()
                .Where(l => l.IsActive).OrderBy(l => l.SortOrder)
                .ProjectTo<LookupDto>(_mapper.ConfigurationProvider).ToListAsync(cancellationToken),

            LookupType.EntityTypes => _context.LookupEntityTypes.AsNoTracking()
                .Where(l => l.IsActive).OrderBy(l => l.SortOrder)
                .ProjectTo<LookupDto>(_mapper.ConfigurationProvider).ToListAsync(cancellationToken),

            LookupType.DonationTypes => _context.LookupDonationTypes.AsNoTracking()
                .Where(l => l.IsActive).OrderBy(l => l.SortOrder)
                .ProjectTo<LookupDto>(_mapper.ConfigurationProvider).ToListAsync(cancellationToken),

            LookupType.DonationFrequencies => _context.LookupDonationFrequencies.AsNoTracking()
                .Where(l => l.IsActive).OrderBy(l => l.SortOrder)
                .ProjectTo<LookupDto>(_mapper.ConfigurationProvider).ToListAsync(cancellationToken),

            LookupType.BbbeeStatuses => _context.LookupBbbeeStatuses.AsNoTracking()
                .Where(l => l.IsActive).OrderBy(l => l.SortOrder)
                .ProjectTo<LookupDto>(_mapper.ConfigurationProvider).ToListAsync(cancellationToken),

            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported lookup type.")
        };
    }

    public Task<List<CodedLookupDto>> GetActiveCodedAsync(CodedLookupType type, CancellationToken cancellationToken = default)
    {
        return type switch
        {
            CodedLookupType.OperationalRegions => _context.LookupOperationalRegions.AsNoTracking()
                .Where(l => l.IsActive).OrderBy(l => l.SortOrder)
                .ProjectTo<CodedLookupDto>(_mapper.ConfigurationProvider).ToListAsync(cancellationToken),

            CodedLookupType.Provinces => _context.LookupProvinces.AsNoTracking()
                .Where(l => l.IsActive).OrderBy(l => l.SortOrder)
                .ProjectTo<CodedLookupDto>(_mapper.ConfigurationProvider).ToListAsync(cancellationToken),

            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported lookup type.")
        };
    }

    public Task<bool> CompanyTypeExistsActiveAsync(short id, CancellationToken cancellationToken = default)
        => _context.LookupCompanyTypes.AsNoTracking().AnyAsync(l => l.Id == id && l.IsActive, cancellationToken);

    public Task<bool> EntityTypeExistsActiveAsync(short id, CancellationToken cancellationToken = default)
        => _context.LookupEntityTypes.AsNoTracking().AnyAsync(l => l.Id == id && l.IsActive, cancellationToken);

    public Task<bool> ProvinceExistsActiveAsync(short id, CancellationToken cancellationToken = default)
        => _context.LookupProvinces.AsNoTracking().AnyAsync(l => l.Id == id && l.IsActive, cancellationToken);

    public Task<bool> DonationFrequencyExistsActiveAsync(short id, CancellationToken cancellationToken = default)
        => _context.LookupDonationFrequencies.AsNoTracking().AnyAsync(l => l.Id == id && l.IsActive, cancellationToken);

    public Task<bool> BbbeeStatusExistsActiveAsync(short id, CancellationToken cancellationToken = default)
        => _context.LookupBbbeeStatuses.AsNoTracking().AnyAsync(l => l.Id == id && l.IsActive, cancellationToken);

    public Task<int> CountActiveDonationTypesAsync(IEnumerable<short> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        return _context.LookupDonationTypes.AsNoTracking()
            .CountAsync(l => idList.Contains(l.Id) && l.IsActive, cancellationToken);
    }

    public Task<int> CountActiveOperationalRegionsAsync(IEnumerable<short> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        return _context.LookupOperationalRegions.AsNoTracking()
            .CountAsync(l => idList.Contains(l.Id) && l.IsActive, cancellationToken);
    }
}
