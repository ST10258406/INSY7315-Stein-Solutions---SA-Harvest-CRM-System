using AutoMapper;
using CRM.Application.Modules.Lookups.Mappings;
using CRM.Application.Modules.Lookups.Queries.GetCodedLookup;
using CRM.Application.Modules.Lookups.Queries.GetLookup;
using CRM.Domain.Entities.Lookups;
using CRM.Infrastructure.Persistence;
using CRM.Infrastructure.Persistence.Repositories;
using CRM.Infrastructure.Tests.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace CRM.Infrastructure.Tests.Persistence.Repositories;

/// <summary>
/// Exercises <see cref="LookupRepository"/> against a real PostgreSQL database, since the
/// active-filtering and ordering semantics are now provider-side rather than in the handler.
/// </summary>
public class LookupRepositoryTests
{
    private readonly DbContextOptions<CrmDbContext> _options;
    private readonly IMapper _mapper;

    public LookupRepositoryTests()
    {
        _options = new DbContextOptionsBuilder<CrmDbContext>()
            .UseNpgsql(TestPostgres.ConnectionString("crm_test_lookuprepository"))
            .Options;

        var config = new MapperConfiguration(cfg => cfg.AddProfile<LookupsMappingProfile>(), NullLoggerFactory.Instance);
        _mapper = config.CreateMapper();
    }

    private async Task<CrmDbContext> CreateContextAsync()
    {
        var context = new CrmDbContext(_options);
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    private static async Task ResetCompanyTypesAsync(CrmDbContext context)
    {
        context.LookupCompanyTypes.RemoveRange(context.LookupCompanyTypes);
        await context.SaveChangesAsync();
    }

    private static async Task ResetProvincesAsync(CrmDbContext context)
    {
        context.LookupProvinces.RemoveRange(context.LookupProvinces);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetActiveAsync_ExcludesInactiveRows_AndOrdersBySortOrder()
    {
        using var context = await CreateContextAsync();
        await ResetCompanyTypesAsync(context);

        context.LookupCompanyTypes.AddRange(
            new LookupCompanyType { Id = 1, Name = "Second", IsActive = true, SortOrder = 2 },
            new LookupCompanyType { Id = 2, Name = "First", IsActive = true, SortOrder = 1 },
            new LookupCompanyType { Id = 3, Name = "Retired", IsActive = false, SortOrder = 0 });
        await context.SaveChangesAsync();

        var repository = new LookupRepository(context, _mapper);

        var result = await repository.GetActiveAsync(LookupType.CompanyTypes);

        Assert.Equal(["First", "Second"], result.Select(r => r.Name));
        Assert.DoesNotContain(result, r => r.Name == "Retired");
    }

    [Fact]
    public async Task GetActiveAsync_EmptyTable_ReturnsEmptyList()
    {
        using var context = await CreateContextAsync();
        await ResetCompanyTypesAsync(context);

        var repository = new LookupRepository(context, _mapper);

        var result = await repository.GetActiveAsync(LookupType.CompanyTypes);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetActiveAsync_UnsupportedType_Throws()
    {
        using var context = await CreateContextAsync();
        var repository = new LookupRepository(context, _mapper);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => repository.GetActiveAsync((LookupType)999));
    }

    [Fact]
    public async Task GetActiveCodedAsync_ProjectsCode_ExcludesInactive_AndOrdersBySortOrder()
    {
        using var context = await CreateContextAsync();
        await ResetProvincesAsync(context);

        context.LookupProvinces.AddRange(
            new LookupProvince { Id = 1, Code = "WC", Name = "Second", IsActive = true, SortOrder = 2 },
            new LookupProvince { Id = 2, Code = "GP", Name = "First", IsActive = true, SortOrder = 1 },
            new LookupProvince { Id = 3, Code = "XX", Name = "Retired", IsActive = false, SortOrder = 0 });
        await context.SaveChangesAsync();

        var repository = new LookupRepository(context, _mapper);

        var result = await repository.GetActiveCodedAsync(CodedLookupType.Provinces);

        Assert.Equal(["First", "Second"], result.Select(r => r.Name));
        Assert.Equal(["GP", "WC"], result.Select(r => r.Code));
    }

    [Fact]
    public async Task GetActiveCodedAsync_UnsupportedType_Throws()
    {
        using var context = await CreateContextAsync();
        var repository = new LookupRepository(context, _mapper);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => repository.GetActiveCodedAsync((CodedLookupType)999));
    }

    [Fact]
    public async Task CompanyTypeExistsActiveAsync_TrueOnlyForActiveRow()
    {
        using var context = await CreateContextAsync();
        await ResetCompanyTypesAsync(context);

        context.LookupCompanyTypes.AddRange(
            new LookupCompanyType { Id = 10, Name = "Active", IsActive = true, SortOrder = 1 },
            new LookupCompanyType { Id = 11, Name = "Inactive", IsActive = false, SortOrder = 2 });
        await context.SaveChangesAsync();

        var repository = new LookupRepository(context, _mapper);

        Assert.True(await repository.CompanyTypeExistsActiveAsync(10));
        Assert.False(await repository.CompanyTypeExistsActiveAsync(11));
        Assert.False(await repository.CompanyTypeExistsActiveAsync(999));
    }

    [Fact]
    public async Task ProvinceExistsActiveAsync_TrueOnlyForActiveRow()
    {
        using var context = await CreateContextAsync();
        await ResetProvincesAsync(context);

        context.LookupProvinces.AddRange(
            new LookupProvince { Id = 20, Code = "AA", Name = "Active", IsActive = true, SortOrder = 1 },
            new LookupProvince { Id = 21, Code = "BB", Name = "Inactive", IsActive = false, SortOrder = 2 });
        await context.SaveChangesAsync();

        var repository = new LookupRepository(context, _mapper);

        Assert.True(await repository.ProvinceExistsActiveAsync(20));
        Assert.False(await repository.ProvinceExistsActiveAsync(21));
    }

    [Fact]
    public async Task CountActiveDonationTypesAsync_CountsOnlyActiveMatches()
    {
        using var context = await CreateContextAsync();
        context.LookupDonationTypes.RemoveRange(context.LookupDonationTypes);
        await context.SaveChangesAsync();

        context.LookupDonationTypes.AddRange(
            new LookupDonationType { Id = 1, Name = "Food", IsActive = true, SortOrder = 1 },
            new LookupDonationType { Id = 2, Name = "Non-Food", IsActive = true, SortOrder = 2 },
            new LookupDonationType { Id = 3, Name = "Retired", IsActive = false, SortOrder = 3 });
        await context.SaveChangesAsync();

        var repository = new LookupRepository(context, _mapper);

        Assert.Equal(2, await repository.CountActiveDonationTypesAsync([(short)1, (short)2]));
        Assert.Equal(1, await repository.CountActiveDonationTypesAsync([(short)1, (short)3]));
        Assert.Equal(0, await repository.CountActiveDonationTypesAsync([]));
    }

    [Fact]
    public async Task CountActiveOperationalRegionsAsync_CountsOnlyActiveMatches()
    {
        using var context = await CreateContextAsync();
        context.LookupOperationalRegions.RemoveRange(context.LookupOperationalRegions);
        await context.SaveChangesAsync();

        context.LookupOperationalRegions.AddRange(
            new LookupOperationalRegion { Id = 1, Code = "GP", Name = "Gauteng", IsActive = true, SortOrder = 1 },
            new LookupOperationalRegion { Id = 2, Code = "XX", Name = "Retired", IsActive = false, SortOrder = 2 });
        await context.SaveChangesAsync();

        var repository = new LookupRepository(context, _mapper);

        Assert.Equal(1, await repository.CountActiveOperationalRegionsAsync([(short)1, (short)2]));
    }
}
