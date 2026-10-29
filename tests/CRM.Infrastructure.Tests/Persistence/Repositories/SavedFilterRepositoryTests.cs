using CRM.Domain.Entities;
using CRM.Domain.Enums;
using CRM.Infrastructure.Persistence;
using CRM.Infrastructure.Persistence.Repositories;
using CRM.Infrastructure.Tests.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Tests.Persistence.Repositories;

/// <summary>Issue 103, on real Postgres: per-user scoping, soft delete, and the partial unique index.</summary>
public class SavedFilterRepositoryTests
{
    private readonly DbContextOptions<CrmDbContext> _options = new DbContextOptionsBuilder<CrmDbContext>()
        .UseNpgsql(TestPostgres.ConnectionString("crm_test_savedfilterrepo"))
        .Options;

    private Guid _userId;
    private Guid _otherUserId;

    private async Task<CrmDbContext> CreateSeededContextAsync()
    {
        var context = new CrmDbContext(_options);
        await context.Database.EnsureCreatedAsync();

        context.SavedFilters.RemoveRange(context.SavedFilters);
        await context.SaveChangesAsync();

        _userId = (await GetOrAddUser(context, "filters-owner@example.com")).Id;
        _otherUserId = (await GetOrAddUser(context, "filters-other@example.com")).Id;
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();
        return context;
    }

    private static async Task<User> GetOrAddUser(CrmDbContext context, string email)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user is null)
        {
            user = new User { Id = Guid.NewGuid(), Email = email, FirstName = "F", LastName = "U", PasswordHash = "hash", IsActive = true };
            context.Users.Add(user);
        }
        return user;
    }

    private static SavedFilter Make(Guid userId, string name, bool isActive = true) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        Name = name,
        Page = SavedFilterPage.Donors,
        QueryString = "status=Active",
        IsActive = isActive,
    };

    [Fact]
    public async Task ListActiveAsync_ReturnsOnlyTheUsersActiveFilters_ByName()
    {
        using var context = await CreateSeededContextAsync();
        context.SavedFilters.AddRange(
            Make(_userId, "Zulu"),
            Make(_userId, "Alpha"),
            Make(_userId, "Deleted", isActive: false),
            Make(_otherUserId, "Not mine"));
        await context.SaveChangesAsync();

        var items = await new SavedFilterRepository(context).ListActiveAsync(_userId, SavedFilterPage.Donors);

        Assert.Equal(new[] { "Alpha", "Zulu" }, items.Select(i => i.Name));
        Assert.All(items, i => Assert.Equal("Donors", i.Page));
    }

    [Fact]
    public async Task GetActiveForUpdateAsync_DoesNotReturnAnotherUsersFilter()
    {
        using var context = await CreateSeededContextAsync();
        var theirs = Make(_otherUserId, "Theirs");
        context.SavedFilters.Add(theirs);
        await context.SaveChangesAsync();

        Assert.Null(await new SavedFilterRepository(context).GetActiveForUpdateAsync(theirs.Id, _userId));
    }

    [Fact]
    public async Task ActiveNameExistsAsync_IsCaseInsensitive_AndIgnoresDeletedFilters()
    {
        using var context = await CreateSeededContextAsync();
        context.SavedFilters.AddRange(Make(_userId, "Gauteng"), Make(_userId, "Old", isActive: false));
        await context.SaveChangesAsync();
        var repo = new SavedFilterRepository(context);

        Assert.True(await repo.ActiveNameExistsAsync(_userId, "GAUTENG"));
        Assert.False(await repo.ActiveNameExistsAsync(_userId, "Old"));
        Assert.False(await repo.ActiveNameExistsAsync(_otherUserId, "Gauteng"));
        Assert.Equal(1, await repo.CountActiveAsync(_userId));
    }

    [Fact]
    public async Task UniqueIndex_AllowsReusingTheNameOfADeletedFilter_ButNotOfAnActiveOne()
    {
        using var context = await CreateSeededContextAsync();
        context.SavedFilters.AddRange(Make(_userId, "Reused", isActive: false), Make(_userId, "Reused"));
        await context.SaveChangesAsync(); // one deleted + one active: allowed

        context.SavedFilters.Add(Make(_userId, "Reused"));
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }
}
