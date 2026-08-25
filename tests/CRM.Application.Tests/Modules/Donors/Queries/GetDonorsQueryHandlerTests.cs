using AutoMapper;
using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Donors.Mappings;
using CRM.Application.Modules.Donors.Queries.GetDonors;
using CRM.Application.Modules.Lookups.Mappings;
using CRM.Domain.Entities;
using CRM.Domain.Entities.Lookups;
using CRM.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.NSubstitute;
using NSubstitute;

namespace CRM.Application.Tests.Modules.Donors.Queries;

public class GetDonorsQueryHandlerTests
{
    private readonly IApplicationDbContext _contextMock;
    private readonly IMapper _mapper;
    private readonly GetDonorsQueryHandler _handler;

    public GetDonorsQueryHandlerTests()
    {
        _contextMock = Substitute.For<IApplicationDbContext>();

        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<DonorMappingProfile>();
            cfg.AddProfile<LookupsMappingProfile>();
        }, NullLoggerFactory.Instance);
        _mapper = config.CreateMapper();

        _handler = new GetDonorsQueryHandler(_contextMock, _mapper);
    }

    private void SetupDonors(List<Donor> donors)
    {
        var mockDonorsDbSet = donors.BuildMockDbSet();
        _contextMock.Donors.Returns(mockDonorsDbSet);
    }

    private static Donor MakeDonor(string companyName, DonorStatus status, string regionCode = "JHB")
    {
        var companyType = new LookupCompanyType { Id = 1, Name = "Manufacturer", IsActive = true };
        var frequency = new LookupDonationFrequency { Id = 1, Name = "Monthly", IsActive = true };
        var region = new LookupOperationalRegion { Id = 1, Name = "Johannesburg", Code = regionCode, IsActive = true };

        return new Donor
        {
            Id = Guid.NewGuid(),
            CompanyName = companyName,
            CompanyTypeId = companyType.Id,
            CompanyType = companyType,
            RegisteredCompanyName = companyName,
            EntityTypeId = 1,
            EntityType = new LookupEntityType { Id = 1, Name = "Pty Ltd", IsActive = true },
            DonationFrequencyId = frequency.Id,
            DonationFrequency = frequency,
            Status = status,
            SubmissionSource = SubmissionSource.ManualCapture,
            CreatedByUserId = Guid.NewGuid(),
            CreatedByUser = new User { Id = Guid.NewGuid(), FirstName = "Creator", LastName = "User", Email = "creator@test.com" },
            OperationalRegions = new List<DonorOperationalRegion>
            {
                new() { OperationalRegionId = region.Id, OperationalRegion = region }
            },
            DonationTypes = new List<DonorDonationType>(),
            InteractionLogs = new List<InteractionLog>()
        };
    }

    [Fact]
    public async Task Handle_FiltersByStatus_ReturnsOnlyMatchingDonors()
    {
        var donors = new List<Donor>
        {
            MakeDonor("FoodCorp SA", DonorStatus.Active),
            MakeDonor("Lapsed Co", DonorStatus.Lapsed),
            MakeDonor("Pending Co", DonorStatus.PendingReview)
        };
        SetupDonors(donors);

        var query = new GetDonorsQuery { Status = "Active" };

        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.Single(result.Data);
        Assert.Equal("FoodCorp SA", result.Data[0].CompanyName);
        Assert.Equal(1, result.Pagination.TotalCount);
    }

    [Fact]
    public async Task Handle_FiltersBySearch_IsCaseInsensitiveAndMatchesSubstring()
    {
        var donors = new List<Donor>
        {
            MakeDonor("FoodCorp SA", DonorStatus.Active),
            MakeDonor("Other Company", DonorStatus.Active)
        };
        SetupDonors(donors);

        var query = new GetDonorsQuery { Search = "foodcorp" };

        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.Single(result.Data);
        Assert.Equal("FoodCorp SA", result.Data[0].CompanyName);
    }

    [Fact]
    public async Task Handle_FiltersByRegionCode_ReturnsOnlyMatchingDonors()
    {
        var donors = new List<Donor>
        {
            MakeDonor("Jhb Donor", DonorStatus.Active, "JHB"),
            MakeDonor("Cpt Donor", DonorStatus.Active, "CPT")
        };
        SetupDonors(donors);

        var query = new GetDonorsQuery { RegionCode = "CPT" };

        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.Single(result.Data);
        Assert.Equal("Cpt Donor", result.Data[0].CompanyName);
    }

    [Fact]
    public async Task Handle_EmptyResultSet_ReturnsEmptyDataAndZeroTotalCount()
    {
        SetupDonors(new List<Donor>());

        var query = new GetDonorsQuery { Status = "Active" };

        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.Empty(result.Data);
        Assert.Equal(0, result.Pagination.TotalCount);
        Assert.Equal(0, result.Pagination.TotalPages);
    }

    [Fact]
    public async Task Handle_Pagination_ReturnsCorrectPageAndTotalPages()
    {
        var donors = Enumerable.Range(1, 25)
            .Select(i => MakeDonor($"Donor {i:00}", DonorStatus.Active))
            .ToList();
        SetupDonors(donors);

        var query = new GetDonorsQuery { Page = 2, PageSize = 10 };

        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.Equal(10, result.Data.Count);
        Assert.Equal(25, result.Pagination.TotalCount);
        Assert.Equal(3, result.Pagination.TotalPages);
    }

    [Fact]
    public async Task Handle_MapsDonationFrequencyAndOperationalRegions()
    {
        var donor = MakeDonor("FoodCorp SA", DonorStatus.Active, "JHB");
        SetupDonors(new List<Donor> { donor });

        var query = new GetDonorsQuery();

        var result = await _handler.Handle(query, CancellationToken.None);

        var dto = Assert.Single(result.Data);
        Assert.Equal("Monthly", dto.DonationFrequency);
        Assert.Equal("Manufacturer", dto.CompanyType);
        Assert.Contains("JHB", dto.OperationalRegions);
        Assert.Equal("Active", dto.Status);
    }
}
