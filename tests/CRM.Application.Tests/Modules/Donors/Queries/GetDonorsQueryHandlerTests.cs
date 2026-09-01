using CRM.Application.Common.Interfaces;
using CRM.Application.Common.Models;
using CRM.Application.Modules.Donors.Dtos;
using CRM.Application.Modules.Donors.Queries.GetDonors;
using CRM.Domain.Enums;
using NSubstitute;

namespace CRM.Application.Tests.Modules.Donors.Queries;

// Filtering, sorting and pagination SQL now live in DonorRepository and are covered
// against a real database in CRM.Infrastructure.Tests. What remains here is the
// handler's own job: translating the query into DonorSearchCriteria and assembling
// the PaginatedResult envelope.
public class GetDonorsQueryHandlerTests
{
    private readonly IDonorRepository _donorsMock = Substitute.For<IDonorRepository>();
    private readonly GetDonorsQueryHandler _handler;

    private DonorSearchCriteria? _capturedCriteria;

    public GetDonorsQueryHandlerTests()
    {
        _donorsMock
            .SearchAsync(Arg.Any<DonorSearchCriteria>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                _capturedCriteria = ci.ArgAt<DonorSearchCriteria>(0);
                return (new List<DonorListItemDto>(), 0);
            });

        _handler = new GetDonorsQueryHandler(_donorsMock);
    }

    [Fact]
    public async Task Handle_MapsEveryFilterOntoTheCriteria()
    {
        var managerId = Guid.NewGuid();

        var query = new GetDonorsQuery
        {
            Search = "foodcorp",
            Status = "Active",
            CompanyTypeId = 1,
            RegionCode = "CPT",
            DonationTypeId = 3,
            DonationFrequencyId = 2,
            RelationshipManagerId = managerId,
            FollowUpBefore = new DateOnly(2026, 8, 15),
            SortBy = "companyName",
            SortDir = "desc",
            Page = 2,
            PageSize = 10
        };

        await _handler.Handle(query, CancellationToken.None);

        Assert.NotNull(_capturedCriteria);
        Assert.Equal("foodcorp", _capturedCriteria!.Search);
        Assert.Equal(DonorStatus.Active, _capturedCriteria.Status);
        Assert.Equal((short)1, _capturedCriteria.CompanyTypeId);
        Assert.Equal("CPT", _capturedCriteria.RegionCode);
        Assert.Equal((short)3, _capturedCriteria.DonationTypeId);
        Assert.Equal((short)2, _capturedCriteria.DonationFrequencyId);
        Assert.Equal(managerId, _capturedCriteria.RelationshipManagerId);
        Assert.Equal("companyName", _capturedCriteria.SortBy);
        Assert.Equal("desc", _capturedCriteria.SortDir);
        Assert.Equal(2, _capturedCriteria.Page);
        Assert.Equal(10, _capturedCriteria.PageSize);
    }

    [Fact]
    public async Task Handle_FollowUpBefore_IsWidenedToTheEndOfThatDay()
    {
        var query = new GetDonorsQuery { FollowUpBefore = new DateOnly(2026, 8, 15) };

        await _handler.Handle(query, CancellationToken.None);

        Assert.Equal(
            new DateOnly(2026, 8, 15).ToDateTime(TimeOnly.MaxValue),
            _capturedCriteria!.FollowUpBefore);
    }

    [Fact]
    public async Task Handle_NoFollowUpBefore_LeavesCriteriaNull()
    {
        await _handler.Handle(new GetDonorsQuery(), CancellationToken.None);

        Assert.Null(_capturedCriteria!.FollowUpBefore);
    }

    [Theory]
    [InlineData("Active", DonorStatus.Active)]
    [InlineData("active", DonorStatus.Active)]
    [InlineData("Lapsed", DonorStatus.Lapsed)]
    public async Task Handle_ParsesStatusCaseInsensitively(string raw, DonorStatus expected)
    {
        await _handler.Handle(new GetDonorsQuery { Status = raw }, CancellationToken.None);

        Assert.Equal(expected, _capturedCriteria!.Status);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("NotAStatus")]
    public async Task Handle_UnusableStatus_AppliesNoStatusFilter(string? raw)
    {
        await _handler.Handle(new GetDonorsQuery { Status = raw }, CancellationToken.None);

        Assert.Null(_capturedCriteria!.Status);
    }

    [Fact]
    public async Task Handle_WrapsRepositoryResultInPaginationEnvelope()
    {
        var items = new List<DonorListItemDto>
        {
            new() { Id = Guid.NewGuid(), CompanyName = "FoodCorp SA", Status = "Active" }
        };

        _donorsMock
            .SearchAsync(Arg.Any<DonorSearchCriteria>(), Arg.Any<CancellationToken>())
            .Returns((items, 25));

        var result = await _handler.Handle(new GetDonorsQuery { Page = 2, PageSize = 10 }, CancellationToken.None);

        Assert.Same(items[0], result.Data.Single());
        Assert.Equal(2, result.Pagination.Page);
        Assert.Equal(10, result.Pagination.PageSize);
        Assert.Equal(25, result.Pagination.TotalCount);
        Assert.Equal(3, result.Pagination.TotalPages);
    }

    [Fact]
    public async Task Handle_EmptyResultSet_ReturnsEmptyDataAndZeroTotalCount()
    {
        var result = await _handler.Handle(new GetDonorsQuery { Status = "Active" }, CancellationToken.None);

        Assert.Empty(result.Data);
        Assert.Equal(0, result.Pagination.TotalCount);
        Assert.Equal(0, result.Pagination.TotalPages);
    }
}
