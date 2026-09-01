using AutoMapper;
using CRM.Application.Common.Models;
using CRM.Application.Modules.Tasks.Mappings;
using CRM.Domain.Entities;
using CRM.Domain.Entities.Lookups;
using CRM.Domain.Enums;
using CRM.Infrastructure.Persistence;
using CRM.Infrastructure.Persistence.Repositories;
using CRM.Infrastructure.Tests.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace CRM.Infrastructure.Tests.Persistence.Repositories;

public class TaskRepositoryTests
{
    private readonly DbContextOptions<CrmDbContext> _options;
    private readonly IMapper _mapper;

    private Guid _donorId;
    private Guid _otherDonorId;
    private Guid _assigneeId;
    private Guid _otherUserId;

    public TaskRepositoryTests()
    {
        _options = new DbContextOptionsBuilder<CrmDbContext>()
            .UseNpgsql(TestPostgres.ConnectionString("crm_test_taskrepo"))
            .Options;

        var config = new MapperConfiguration(cfg => cfg.AddProfile<TaskMappingProfile>(), NullLoggerFactory.Instance);
        _mapper = config.CreateMapper();
    }

    private async Task<CrmDbContext> CreateSeededContextAsync()
    {
        var context = new CrmDbContext(_options);
        await context.Database.EnsureCreatedAsync();

        context.DonorTasks.RemoveRange(context.DonorTasks);
        context.Donors.RemoveRange(context.Donors);
        await context.SaveChangesAsync();

        if (!await context.LookupCompanyTypes.AnyAsync(l => l.Id == 1))
            context.LookupCompanyTypes.Add(new LookupCompanyType { Id = 1, Name = "Manufacturer", IsActive = true, SortOrder = 1 });
        if (!await context.LookupEntityTypes.AnyAsync(l => l.Id == 1))
            context.LookupEntityTypes.Add(new LookupEntityType { Id = 1, Name = "Private Company", IsActive = true, SortOrder = 1 });
        if (!await context.LookupDonationFrequencies.AnyAsync(l => l.Id == 1))
            context.LookupDonationFrequencies.Add(new LookupDonationFrequency { Id = 1, Name = "Monthly", IsActive = true, SortOrder = 1 });

        var assignee = await GetOrAddUser(context, "task-assignee@example.com", "Ann", "Assignee");
        var other = await GetOrAddUser(context, "task-other@example.com", "Otto", "Other");
        await context.SaveChangesAsync();
        _assigneeId = assignee.Id;
        _otherUserId = other.Id;

        var donor = MakeDonor("Task Co", _assigneeId);
        var otherDonor = MakeDonor("Other Co", _assigneeId);
        context.Donors.AddRange(donor, otherDonor);
        await context.SaveChangesAsync();
        _donorId = donor.Id;
        _otherDonorId = otherDonor.Id;

        context.ChangeTracker.Clear();
        return context;
    }

    private static async Task<User> GetOrAddUser(CrmDbContext context, string email, string first, string last)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user is null)
        {
            user = new User { Id = Guid.NewGuid(), Email = email, FirstName = first, LastName = last, PasswordHash = "hash", IsActive = true };
            context.Users.Add(user);
        }
        return user;
    }

    private static Donor MakeDonor(string name, Guid creatorId) => new()
    {
        Id = Guid.NewGuid(),
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

    private DonorTask MakeTask(
        Guid donorId, Guid assigneeId, DateTime dueDate, bool completed = false, Guid? completedBy = null) => new()
    {
        Id = Guid.NewGuid(),
        DonorId = donorId,
        Title = "Follow up",
        DueDate = dueDate,
        IsCompleted = completed,
        AssignedToUserId = assigneeId,
        CreatedByUserId = _assigneeId,
        CompletedByUserId = completed ? (completedBy ?? _assigneeId) : null
    };

    [Fact]
    public async Task GetTasksAsync_ByAssignee_ReturnsOnlyThatUsersTasks_SoonestDueFirst()
    {
        using var context = await CreateSeededContextAsync();
        var today = DateTime.UtcNow.Date;
        context.DonorTasks.AddRange(
            MakeTask(_donorId, _assigneeId, today.AddDays(5)),
            MakeTask(_donorId, _assigneeId, today.AddDays(1)),
            MakeTask(_donorId, _otherUserId, today.AddDays(2)));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repo = new TaskRepository(context, _mapper);
        var (items, total) = await repo.GetTasksAsync(new TaskSearchCriteria { AssignedToUserId = _assigneeId });

        Assert.Equal(2, total);
        Assert.All(items, i => Assert.Equal("Ann Assignee", i.AssignedTo.FullName));
        Assert.True(items[0].DueDate < items[1].DueDate);
    }

    [Fact]
    public async Task GetTasksAsync_ByDonor_ReturnsEveryAssigneesTasksForThatDonorOnly()
    {
        using var context = await CreateSeededContextAsync();
        var today = DateTime.UtcNow.Date;
        context.DonorTasks.AddRange(
            MakeTask(_donorId, _assigneeId, today.AddDays(1)),
            MakeTask(_donorId, _otherUserId, today.AddDays(2)),
            MakeTask(_otherDonorId, _assigneeId, today.AddDays(1)));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repo = new TaskRepository(context, _mapper);
        var (items, total) = await repo.GetTasksAsync(new TaskSearchCriteria { DonorId = _donorId });

        Assert.Equal(2, total);
        Assert.All(items, i => Assert.Equal(_donorId, i.Donor.Id));
        Assert.All(items, i => Assert.Equal("Task Co", i.Donor.CompanyName));
    }

    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 2)]
    [InlineData(null, 3)]
    public async Task GetTasksAsync_FiltersOnCompletion(bool? isCompleted, int expected)
    {
        using var context = await CreateSeededContextAsync();
        var today = DateTime.UtcNow.Date;
        context.DonorTasks.AddRange(
            MakeTask(_donorId, _assigneeId, today.AddDays(1)),
            MakeTask(_donorId, _assigneeId, today.AddDays(2)),
            MakeTask(_donorId, _assigneeId, today.AddDays(3), completed: true, completedBy: _otherUserId));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repo = new TaskRepository(context, _mapper);
        var (items, total) = await repo.GetTasksAsync(new TaskSearchCriteria { DonorId = _donorId, IsCompleted = isCompleted });

        Assert.Equal(expected, total);
        Assert.Equal(expected, items.Count);
        if (isCompleted == true)
            Assert.Equal("Otto Other", items[0].CompletedBy!.FullName);
    }

    [Fact]
    public async Task GetTasksAsync_DueBefore_IsInclusiveAndPaginatesWithFullCount()
    {
        using var context = await CreateSeededContextAsync();
        var today = DateTime.UtcNow.Date;
        for (var i = 1; i <= 5; i++)
            context.DonorTasks.Add(MakeTask(_donorId, _assigneeId, today.AddDays(i)));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repo = new TaskRepository(context, _mapper);
        var (items, total) = await repo.GetTasksAsync(new TaskSearchCriteria
        {
            DonorId = _donorId,
            DueBefore = today.AddDays(3),
            Page = 2,
            PageSize = 2
        });

        Assert.Equal(3, total);      // days 1..3
        Assert.Single(items);        // page 2 of size 2 over 3 rows
    }

    [Fact]
    public async Task AddAsync_StagesTask_CommittedByCallerSaveChanges()
    {
        using var context = await CreateSeededContextAsync();
        var repo = new TaskRepository(context, _mapper);

        var task = MakeTask(_donorId, _assigneeId, DateTime.UtcNow.Date.AddDays(4));
        await repo.AddAsync(task);
        await context.SaveChangesAsync();

        using var verify = new CrmDbContext(_options);
        Assert.True(await verify.DonorTasks.AsNoTracking().AnyAsync(t => t.Id == task.Id));
    }

    [Fact]
    public async Task GetDtoByIdAsync_ProjectsNestedDonorAssigneeAndCreator()
    {
        using var context = await CreateSeededContextAsync();
        var task = MakeTask(_donorId, _otherUserId, DateTime.UtcNow.Date.AddDays(2));
        context.DonorTasks.Add(task);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repo = new TaskRepository(context, _mapper);
        var dto = await repo.GetDtoByIdAsync(task.Id);

        Assert.NotNull(dto);
        Assert.Equal(task.Id, dto!.Id);
        Assert.Equal("Task Co", dto.Donor.CompanyName);
        Assert.Equal("Otto Other", dto.AssignedTo.FullName);
        Assert.Equal("Ann Assignee", dto.CreatedBy.FullName);
        Assert.Null(dto.CompletedBy);
    }

    [Fact]
    public async Task GetDtoByIdAsync_UnknownId_ReturnsNull()
    {
        using var context = await CreateSeededContextAsync();
        var repo = new TaskRepository(context, _mapper);

        Assert.Null(await repo.GetDtoByIdAsync(Guid.NewGuid()));
    }
}
