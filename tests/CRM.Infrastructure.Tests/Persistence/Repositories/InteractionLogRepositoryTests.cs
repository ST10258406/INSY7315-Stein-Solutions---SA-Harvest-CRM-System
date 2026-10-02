using AutoMapper;
using CRM.Application.Modules.Donors.Mappings;
using CRM.Application.Modules.Interactions.Mappings;
using CRM.Domain.Entities;
using CRM.Domain.Entities.Lookups;
using CRM.Domain.Enums;
using CRM.Infrastructure.Persistence;
using CRM.Infrastructure.Persistence.Repositories;
using CRM.Infrastructure.Tests.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace CRM.Infrastructure.Tests.Persistence.Repositories;

public class InteractionLogRepositoryTests
{
    private readonly DbContextOptions<CrmDbContext> _options;
    private readonly IMapper _mapper;

    private Guid _donorId;
    private Guid _otherDonorId;
    private Guid _userId;

    public InteractionLogRepositoryTests()
    {
        _options = new DbContextOptionsBuilder<CrmDbContext>()
            .UseNpgsql(TestPostgres.ConnectionString("crm_test_interactionlogrepo"))
            .Options;

        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<InteractionMappingProfile>();
            cfg.AddProfile<DonorMappingProfile>();
        }, NullLoggerFactory.Instance);
        _mapper = config.CreateMapper();
    }

    private async Task<CrmDbContext> CreateSeededContextAsync()
    {
        var context = new CrmDbContext(_options);
        await context.Database.EnsureCreatedAsync();

        context.InteractionLogs.RemoveRange(context.InteractionLogs);
        context.Donors.RemoveRange(context.Donors);
        await context.SaveChangesAsync();

        if (!await context.LookupCompanyTypes.AnyAsync(l => l.Id == 1))
            context.LookupCompanyTypes.Add(new LookupCompanyType { Id = 1, Name = "Manufacturer", IsActive = true, SortOrder = 1 });
        if (!await context.LookupEntityTypes.AnyAsync(l => l.Id == 1))
            context.LookupEntityTypes.Add(new LookupEntityType { Id = 1, Name = "Private Company", IsActive = true, SortOrder = 1 });
        if (!await context.LookupDonationFrequencies.AnyAsync(l => l.Id == 1))
            context.LookupDonationFrequencies.Add(new LookupDonationFrequency { Id = 1, Name = "Monthly", IsActive = true, SortOrder = 1 });

        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == "interaction-fixture@example.com");
        if (user is null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                Email = "interaction-fixture@example.com",
                FirstName = "Case",
                LastName = "Worker",
                PasswordHash = "hash",
                IsActive = true
            };
            context.Users.Add(user);
        }
        await context.SaveChangesAsync();
        _userId = user.Id;

        var donor = MakeDonor("Interaction Co", _userId);
        var other = MakeDonor("Other Co", _userId);
        context.Donors.AddRange(donor, other);
        await context.SaveChangesAsync();

        _donorId = donor.Id;
        _otherDonorId = other.Id;
        context.ChangeTracker.Clear();
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

    private InteractionLog MakeLog(Guid donorId, InteractionType type, DateTime createdAt, string? attachment = null) => new()
    {
        Id = Guid.NewGuid(),
        DonorId = donorId,
        CreatedByUserId = _userId,
        InteractionType = type,
        Subject = $"{type} subject",
        Body = $"{type} body",
        EmailAttachmentUrl = attachment,
        CreatedAt = createdAt
    };

    [Fact]
    public async Task GetByDonorAsync_ReturnsOnlyThatDonorsRows_NewestFirst()
    {
        using var context = await CreateSeededContextAsync();
        var now = DateTime.UtcNow;
        context.InteractionLogs.AddRange(
            MakeLog(_donorId, InteractionType.Note, now.AddDays(-2)),
            MakeLog(_donorId, InteractionType.Call, now.AddDays(-1)),
            MakeLog(_donorId, InteractionType.Meeting, now),
            MakeLog(_otherDonorId, InteractionType.Note, now));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repo = new InteractionLogRepository(context, _mapper);
        var (items, total) = await repo.GetByDonorAsync(_donorId, null, page: 1, pageSize: 20);

        Assert.Equal(3, total);
        Assert.Equal(
            new[] { "Meeting", "Call", "Note" },
            items.Select(i => i.InteractionType).ToArray());
        Assert.All(items, i => Assert.Equal(_donorId, i.DonorId));
        Assert.All(items, i => Assert.Equal("Case Worker", i.CreatedBy.FullName));
    }

    [Fact]
    public async Task GetByDonorAsync_FiltersByInteractionType()
    {
        using var context = await CreateSeededContextAsync();
        var now = DateTime.UtcNow;
        context.InteractionLogs.AddRange(
            MakeLog(_donorId, InteractionType.Note, now.AddMinutes(-3)),
            MakeLog(_donorId, InteractionType.Email, now.AddMinutes(-2)),
            MakeLog(_donorId, InteractionType.Email, now.AddMinutes(-1)));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repo = new InteractionLogRepository(context, _mapper);
        var (items, total) = await repo.GetByDonorAsync(_donorId, InteractionType.Email, page: 1, pageSize: 20);

        Assert.Equal(2, total);
        Assert.All(items, i => Assert.Equal("Email", i.InteractionType));
    }

    [Fact]
    public async Task GetByDonorAsync_PaginatesButCountsAllMatches()
    {
        using var context = await CreateSeededContextAsync();
        var now = DateTime.UtcNow;
        for (var i = 0; i < 5; i++)
            context.InteractionLogs.Add(MakeLog(_donorId, InteractionType.Note, now.AddMinutes(-i)));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repo = new InteractionLogRepository(context, _mapper);
        var (items, total) = await repo.GetByDonorAsync(_donorId, null, page: 2, pageSize: 2);

        Assert.Equal(5, total);
        Assert.Equal(2, items.Count);
    }

    [Fact]
    public async Task GetDtoByIdAsync_ProjectsSingleRowWithAttachmentPathUnchanged()
    {
        using var context = await CreateSeededContextAsync();
        var log = MakeLog(_donorId, InteractionType.Email, DateTime.UtcNow, attachment: "donors/x/email/att.pdf");
        context.InteractionLogs.Add(log);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repo = new InteractionLogRepository(context, _mapper);
        var dto = await repo.GetDtoByIdAsync(log.Id);

        Assert.NotNull(dto);
        Assert.Equal(log.Id, dto!.Id);
        Assert.Equal("Email", dto.InteractionType);
        Assert.Equal("donors/x/email/att.pdf", dto.EmailAttachmentUrl);
        Assert.Equal("Case Worker", dto.CreatedBy.FullName);
    }
}
