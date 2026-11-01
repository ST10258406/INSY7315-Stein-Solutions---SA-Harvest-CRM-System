using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.SavedFilters;
using CRM.Application.Modules.SavedFilters.Commands.CreateSavedFilter;
using CRM.Application.Modules.SavedFilters.Commands.DeleteSavedFilter;
using CRM.Application.Modules.SavedFilters.Queries.GetSavedFilters;
using CRM.Domain.Entities;
using CRM.Domain.Enums;
using NSubstitute;
using ConflictException = CRM.Application.Common.Exceptions.ConflictException;
using NotFoundException = CRM.Application.Common.Exceptions.NotFoundException;
using ValidationException = CRM.Application.Common.Exceptions.ValidationException;

namespace CRM.Application.Tests.Modules.SavedFilters;

/// <summary>Issue 103: saved donor-list filters.</summary>
public class SavedFilterCommandTests
{
    private readonly ISavedFilterRepository _repo = Substitute.For<ISavedFilterRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly Guid _userId = Guid.NewGuid();

    public SavedFilterCommandTests() => _currentUser.GetCurrentUserId().Returns(_userId);

    private CreateSavedFilterCommandHandler CreateHandler() => new(_repo, _unitOfWork, _currentUser);

    // ---- query string ----

    [Fact]
    public void Normalise_DropsLeadingQuestionMarkAndPagingKeys()
        => Assert.Equal("status=Active&regionCode=GP",
            SavedFilterQueryString.Normalise("?status=Active&page=3&regionCode=GP&pageSize=50"));

    [Fact]
    public void UnknownKeys_ListsKeysOutsideTheAllowList()
        => Assert.Equal(new[] { "redirect" }, SavedFilterQueryString.UnknownKeys("status=Active&redirect=evil"));

    [Theory]
    [InlineData("status=Active&status=Lapsed")]
    [InlineData("status")]
    public void IsMalformed_DuplicateOrValuelessKeys(string raw)
        => Assert.True(SavedFilterQueryString.IsMalformed(raw));

    // ---- validator ----

    private static bool Valid(CreateSavedFilterCommand c) => new CreateSavedFilterCommandValidator().Validate(c).IsValid;

    [Fact]
    public void Validator_AcceptsAllowListedFilters()
        => Assert.True(Valid(new() { Name = "Gauteng", QueryString = "regionCode=GP&sortBy=companyName&sortDir=asc" }));

    [Theory]
    [InlineData("", "status=Active")]
    [InlineData("   ", "status=Active")]
    [InlineData("Gauteng", "")]
    [InlineData("Gauteng", "page=2&pageSize=20")] // nothing left once paging is stripped
    [InlineData("Gauteng", "status=Active&redirect=https%3A%2F%2Fevil")]
    [InlineData("Gauteng", "status=Active&status=Lapsed")]
    public void Validator_Rejects(string name, string queryString)
        => Assert.False(Valid(new() { Name = name, QueryString = queryString }));

    [Fact]
    public void Validator_RejectsLongNameAndQueryString()
    {
        Assert.False(Valid(new() { Name = new string('n', 101), QueryString = "status=Active" }));
        Assert.False(Valid(new() { Name = "x", QueryString = "search=" + new string('a', 500) }));
    }

    [Fact]
    public void Validator_RejectsUnknownPage()
        => Assert.False(Valid(new() { Name = "x", Page = "Tasks", QueryString = "status=Active" }));

    [Fact]
    public void GetValidator_RejectsUnknownPage()
        => Assert.False(new GetSavedFiltersQueryValidator().Validate(new GetSavedFiltersQuery { Page = "Users" }).IsValid);

    // ---- create ----

    [Fact]
    public async Task Create_SavesTrimmedNameAndNormalisedQueryString_ForTheCaller()
    {
        SavedFilter? added = null;
        await _repo.AddAsync(Arg.Do<SavedFilter>(f => added = f), Arg.Any<CancellationToken>());
        var command = new CreateSavedFilterCommand { Name = "  My Gauteng  ", QueryString = "?regionCode=GP&page=4" };

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        Assert.NotNull(added);
        Assert.Equal(_userId, added!.UserId);
        Assert.Equal("My Gauteng", added.Name);
        Assert.Equal("regionCode=GP", added.QueryString);
        Assert.Equal(SavedFilterPage.Donors, added.Page);
        Assert.True(added.IsActive);
        Assert.Equal(added.Id, result.Id);
        Assert.Equal(added.Id, command.EntityId);
        Assert.NotNull(command.NewValues);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_DuplicateActiveName_Throws409()
    {
        _repo.ActiveNameExistsAsync(_userId, "Gauteng", Arg.Any<CancellationToken>()).Returns(true);

        await Assert.ThrowsAsync<ConflictException>(() =>
            CreateHandler().Handle(new() { Name = "Gauteng", QueryString = "regionCode=GP" }, CancellationToken.None));
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_AtTheCap_ThrowsValidation()
    {
        _repo.CountActiveAsync(_userId, Arg.Any<CancellationToken>()).Returns(CreateSavedFilterCommandHandler.MaxActivePerUser);

        await Assert.ThrowsAsync<ValidationException>(() =>
            CreateHandler().Handle(new() { Name = "One more", QueryString = "regionCode=GP" }, CancellationToken.None));
    }

    // ---- delete ----

    [Fact]
    public async Task Delete_SoftDeletesTheCallersFilter()
    {
        var filter = new SavedFilter { Id = Guid.NewGuid(), UserId = _userId, Name = "x", QueryString = "status=Active", IsActive = true };
        _repo.GetActiveForUpdateAsync(filter.Id, _userId, Arg.Any<CancellationToken>()).Returns(filter);
        var command = new DeleteSavedFilterCommand { Id = filter.Id };

        await new DeleteSavedFilterCommandHandler(_repo, _unitOfWork, _currentUser).Handle(command, CancellationToken.None);

        Assert.False(filter.IsActive);
        Assert.Equal(filter.Id, command.EntityId);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_SomeoneElsesOrUnknownFilter_Throws404()
    {
        // The repository is scoped to the caller, so another user's id returns null.
        await Assert.ThrowsAsync<NotFoundException>(() =>
            new DeleteSavedFilterCommandHandler(_repo, _unitOfWork, _currentUser)
                .Handle(new DeleteSavedFilterCommand { Id = Guid.NewGuid() }, CancellationToken.None));
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Get_ListsTheCallersFiltersForThePage()
    {
        await new GetSavedFiltersQueryHandler(_repo, _currentUser).Handle(new GetSavedFiltersQuery { Page = "donors" }, CancellationToken.None);

        await _repo.Received(1).ListActiveAsync(_userId, SavedFilterPage.Donors, Arg.Any<CancellationToken>());
    }
}
