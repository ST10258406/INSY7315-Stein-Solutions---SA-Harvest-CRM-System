using CRM.Domain.Entities;
using CRM.Domain.Entities.Lookups;
using CRM.Domain.Enums;
using CRM.Infrastructure.Persistence;
using CRM.Infrastructure.Persistence.Repositories;
using CRM.Infrastructure.Tests.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Tests.Persistence.Repositories;

public class DonorDocumentRepositoryTests
{

    private readonly DbContextOptions<CrmDbContext> _options;

    private Guid _donorId;
    private Guid _otherDonorId;

    public DonorDocumentRepositoryTests()
    {
        _options = new DbContextOptionsBuilder<CrmDbContext>()
            .UseNpgsql(TestPostgres.ConnectionString("crm_test_donordocumentrepository"))
            .Options;
    }

    private async Task<CrmDbContext> CreateSeededContextAsync()
    {
        var context = new CrmDbContext(_options);
        await context.Database.EnsureCreatedAsync();

        context.DonorDocuments.RemoveRange(context.DonorDocuments.IgnoreQueryFilters());
        context.Donors.RemoveRange(context.Donors);
        await context.SaveChangesAsync();

        if (!await context.LookupCompanyTypes.AnyAsync(l => l.Id == 1))
            context.LookupCompanyTypes.Add(new LookupCompanyType { Id = 1, Name = "Manufacturer", IsActive = true, SortOrder = 1 });
        if (!await context.LookupEntityTypes.AnyAsync(l => l.Id == 1))
            context.LookupEntityTypes.Add(new LookupEntityType { Id = 1, Name = "Private Company", IsActive = true, SortOrder = 1 });
        if (!await context.LookupDonationFrequencies.AnyAsync(l => l.Id == 1))
            context.LookupDonationFrequencies.Add(new LookupDonationFrequency { Id = 1, Name = "Monthly", IsActive = true, SortOrder = 1 });

        var creator = await context.Users.FirstOrDefaultAsync(u => u.Email == "docrepo-fixture@example.com");
        if (creator is null)
        {
            creator = new User
            {
                Id = Guid.NewGuid(),
                Email = "docrepo-fixture@example.com",
                FirstName = "Fixture",
                LastName = "User",
                PasswordHash = "hash",
                IsActive = true
            };
            context.Users.Add(creator);
        }
        await context.SaveChangesAsync();

        var donor = MakeDonor("Doc Co", creator.Id);
        var other = MakeDonor("Other Co", creator.Id);
        context.Donors.AddRange(donor, other);
        await context.SaveChangesAsync();

        _donorId = donor.Id;
        _otherDonorId = other.Id;
        return context;
    }

    private static Donor MakeDonor(string name, Guid creatorId) => new()
    {
        Id = Guid.NewGuid(),
        ReferenceNumber = Guid.NewGuid().ToString("N")[..20],
        CompanyName = name,
        CompanyTypeId = 1,
        EntityTypeId = 1,
        DonationFrequencyId = 1,
        RegisteredCompanyName = name + " (Pty) Ltd",
        IncomeTaxNumber = "9012345678",
        Status = DonorStatus.Active,
        SubmissionSource = SubmissionSource.ManualCapture,
        CreatedByUserId = creatorId
    };

    private static DonorDocument MakeDocument(Guid donorId, DocumentType type, bool isActive = true) => new()
    {
        Id = Guid.NewGuid(),
        DonorId = donorId,
        DocumentType = type,
        FileName = "file.pdf",
        BlobStoragePath = $"donors/{donorId}/{type}/{Guid.NewGuid()}_file.pdf",
        MimeType = "application/pdf",
        FileSizeBytes = 1024,
        IsActive = isActive
    };

    [Fact]
    public async Task GetActiveByDonorAndTypeAsync_ReturnsOnlyActiveRowsOfThatTypeForThatDonor()
    {
        using var context = await CreateSeededContextAsync();

        var wanted = MakeDocument(_donorId, DocumentType.BBBEECertificate);
        context.DonorDocuments.AddRange(
            wanted,
            MakeDocument(_donorId, DocumentType.BBBEECertificate, isActive: false),
            MakeDocument(_donorId, DocumentType.Signature),
            MakeDocument(_otherDonorId, DocumentType.BBBEECertificate));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repository = new DonorDocumentRepository(context);

        var result = await repository.GetActiveByDonorAndTypeAsync(_donorId, DocumentType.BBBEECertificate);

        Assert.Equal(wanted.Id, Assert.Single(result).Id);
    }

    [Fact]
    public async Task GetActiveByDonorAndTypeAsync_ReturnsTrackedEntitiesThatCommitOnSave()
    {
        using var context = await CreateSeededContextAsync();

        var document = MakeDocument(_donorId, DocumentType.BBBEECertificate);
        context.DonorDocuments.Add(document);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repository = new DonorDocumentRepository(context);

        var loaded = Assert.Single(await repository.GetActiveByDonorAndTypeAsync(_donorId, DocumentType.BBBEECertificate));
        loaded.IsActive = false;
        await new UnitOfWork(context).SaveChangesAsync();
        context.ChangeTracker.Clear();

        Assert.False((await context.DonorDocuments.IgnoreQueryFilters().SingleAsync(d => d.Id == document.Id)).IsActive);
    }

    [Fact]
    public async Task GetForMutationAsync_IsScopedToTheDonor()
    {
        using var context = await CreateSeededContextAsync();

        var document = MakeDocument(_donorId, DocumentType.Signature);
        context.DonorDocuments.Add(document);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repository = new DonorDocumentRepository(context);

        Assert.NotNull(await repository.GetForMutationAsync(_donorId, document.Id));
        Assert.Null(await repository.GetForMutationAsync(_otherDonorId, document.Id));
    }

    [Fact]
    public async Task GetReadOnlyAsync_ReturnsAnUntrackedInstance()
    {
        using var context = await CreateSeededContextAsync();

        var document = MakeDocument(_donorId, DocumentType.Signature);
        context.DonorDocuments.Add(document);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repository = new DonorDocumentRepository(context);

        var loaded = await repository.GetReadOnlyAsync(_donorId, document.Id);

        Assert.NotNull(loaded);
        Assert.Equal(EntityState.Detached, context.Entry(loaded!).State);
    }

    [Fact]
    public async Task GetDocumentTypeAsync_ReturnsTypeOrNull_ScopedToTheDonor()
    {
        using var context = await CreateSeededContextAsync();

        var document = MakeDocument(_donorId, DocumentType.BBBEECertificate);
        context.DonorDocuments.Add(document);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repository = new DonorDocumentRepository(context);

        Assert.Equal(DocumentType.BBBEECertificate, await repository.GetDocumentTypeAsync(_donorId, document.Id));
        Assert.Null(await repository.GetDocumentTypeAsync(_otherDonorId, document.Id));
        Assert.Null(await repository.GetDocumentTypeAsync(_donorId, Guid.NewGuid()));
    }

    // Security review F-06: a soft-deleted document is invisible to every read, so the
    // authorization filter's type lookup, the download lookup and the delete lookup all
    // report it as not found (→ 404 for every role, never confirming it exists). Both
    // the download handler and the type lookup go through the same filter, so there is
    // no document the handler could reach that skipped the role check.

    [Fact]
    public async Task GetDocumentTypeAsync_IgnoresInactiveDocuments()
    {
        using var context = await CreateSeededContextAsync();

        var document = MakeDocument(_donorId, DocumentType.Signature, isActive: false);
        context.DonorDocuments.Add(document);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repository = new DonorDocumentRepository(context);

        Assert.Null(await repository.GetDocumentTypeAsync(_donorId, document.Id));
    }

    [Fact]
    public async Task GetReadOnlyAsync_IgnoresInactiveDocuments()
    {
        using var context = await CreateSeededContextAsync();

        var document = MakeDocument(_donorId, DocumentType.BBBEECertificate, isActive: false);
        context.DonorDocuments.Add(document);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repository = new DonorDocumentRepository(context);

        Assert.Null(await repository.GetReadOnlyAsync(_donorId, document.Id));
    }

    [Fact]
    public async Task GetForMutationAsync_IgnoresInactiveDocuments()
    {
        using var context = await CreateSeededContextAsync();

        var document = MakeDocument(_donorId, DocumentType.Signature, isActive: false);
        context.DonorDocuments.Add(document);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repository = new DonorDocumentRepository(context);

        Assert.Null(await repository.GetForMutationAsync(_donorId, document.Id));
    }

    [Fact]
    public async Task SoftDeletedDocument_IsHiddenButTheRowIsKept()
    {
        using var context = await CreateSeededContextAsync();

        var document = MakeDocument(_donorId, DocumentType.BBBEECertificate);
        context.DonorDocuments.Add(document);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repository = new DonorDocumentRepository(context);
        var loaded = await repository.GetForMutationAsync(_donorId, document.Id);
        loaded!.IsActive = false;
        await new UnitOfWork(context).SaveChangesAsync();
        context.ChangeTracker.Clear();

        Assert.False(await context.DonorDocuments.AnyAsync(d => d.Id == document.Id));
        var row = await context.DonorDocuments.IgnoreQueryFilters().SingleAsync(d => d.Id == document.Id);
        Assert.False(row.IsActive);
    }

    [Fact]
    public async Task AddAsync_PersistsOnUnitOfWorkSave()
    {
        using var context = await CreateSeededContextAsync();

        var repository = new DonorDocumentRepository(context);
        var document = MakeDocument(_donorId, DocumentType.BBBEECertificate);

        await repository.AddAsync(document);
        await new UnitOfWork(context).SaveChangesAsync();
        context.ChangeTracker.Clear();

        Assert.True(await context.DonorDocuments.AnyAsync(d => d.Id == document.Id));
    }
}
