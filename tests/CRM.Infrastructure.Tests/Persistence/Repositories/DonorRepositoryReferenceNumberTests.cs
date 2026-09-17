using AutoMapper;
using CRM.Infrastructure.Persistence;
using CRM.Infrastructure.Persistence.Repositories;
using CRM.Infrastructure.Tests.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace CRM.Infrastructure.Tests.Persistence.Repositories;

/// <summary>
/// GetNextReferenceNumberAsync's whole reason to exist is "no two concurrent
/// callers ever get the same reference number" — a single-threaded test can't
/// catch that class of bug (see Issue's "Manual Verification" concurrency check).
/// This fires many callers at once, each against its own DbContext instance
/// (DbContext isn't thread-safe, so one per simulated concurrent request — the
/// same shape as separate HTTP requests each getting their own scoped context),
/// against a real Postgres database.
/// </summary>
public class DonorRepositoryReferenceNumberTests
{
    private readonly DbContextOptions<CrmDbContext> _options;
    private readonly IMapper _mapper;
    private readonly int _year = DateTime.UtcNow.Year;
    private string SequenceName => $"donor_reference_seq_{_year}";

    public DonorRepositoryReferenceNumberTests()
    {
        _options = new DbContextOptionsBuilder<CrmDbContext>()
            .UseNpgsql(TestPostgres.ConnectionString("crm_test_donorreferencenumber"))
            .Options;

        var config = new MapperConfiguration(_ => { }, NullLoggerFactory.Instance);
        _mapper = config.CreateMapper();
    }

    private async Task ResetSequenceAsync()
    {
        using var context = new CrmDbContext(_options);
        await context.Database.EnsureCreatedAsync();
        // Force the "sequence doesn't exist yet" lazy-create path to actually be
        // exercised (including under concurrency) on every test run, instead of
        // reusing whatever a previous run left behind. SequenceName is derived
        // only from DateTime.UtcNow.Year — never user input — so raw interpolation
        // here is safe (an identifier can't be a bound SQL parameter anyway).
#pragma warning disable EF1002
        await context.Database.ExecuteSqlRawAsync($"DROP SEQUENCE IF EXISTS {SequenceName}");
#pragma warning restore EF1002
    }

    [Fact]
    public async Task GetNextReferenceNumberAsync_ManyConcurrentCallers_NeverProducesADuplicate()
    {
        await ResetSequenceAsync();

        const int concurrentCallers = 20;

        var results = await Task.WhenAll(Enumerable.Range(0, concurrentCallers).Select(async _ =>
        {
            // Each "caller" gets its own DbContext/connection, mirroring separate
            // concurrent HTTP requests each with their own DI-scoped context.
            using var context = new CrmDbContext(_options);
            var repository = new DonorRepository(context, _mapper);
            return await repository.GetNextReferenceNumberAsync();
        }));

        Assert.Equal(concurrentCallers, results.Distinct().Count());
        Assert.All(results, r => Assert.Matches($@"^DON-{_year}-\d{{5,}}$", r));
    }

    [Fact]
    public async Task GetNextReferenceNumberAsync_SequentialCalls_IncrementsAndFormatsCorrectly()
    {
        await ResetSequenceAsync();
        using var context = new CrmDbContext(_options);
        var repository = new DonorRepository(context, _mapper);

        var first = await repository.GetNextReferenceNumberAsync();
        var second = await repository.GetNextReferenceNumberAsync();

        Assert.Equal($"DON-{_year}-00001", first);
        Assert.Equal($"DON-{_year}-00002", second);
    }
}
