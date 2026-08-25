using AutoMapper;
using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Lookups.Mappings;
using CRM.Application.Modules.Lookups.Queries.GetLookup;
using CRM.Domain.Entities.Lookups;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.NSubstitute;
using NSubstitute;

namespace CRM.Application.Tests.Modules.Lookups.Queries;

public class GetLookupQueryHandlerTests
{
    private readonly IApplicationDbContext _contextMock;
    private readonly GetLookupQueryHandler _handler;

    private readonly List<LookupCompanyType> _companyTypes = [];
    private readonly List<LookupEntityType> _entityTypes = [];
    private readonly List<LookupDonationType> _donationTypes = [];
    private readonly List<LookupDonationFrequency> _donationFrequencies = [];
    private readonly List<LookupBbbeeStatus> _bbbeeStatuses = [];

    public GetLookupQueryHandlerTests()
    {
        _contextMock = Substitute.For<IApplicationDbContext>();

        var config = new MapperConfiguration(cfg => cfg.AddProfile<LookupsMappingProfile>(), NullLoggerFactory.Instance);
        var mapper = config.CreateMapper();

        var companyTypesDbSet = _companyTypes.BuildMockDbSet();
        _contextMock.LookupCompanyTypes.Returns(companyTypesDbSet);

        var entityTypesDbSet = _entityTypes.BuildMockDbSet();
        _contextMock.LookupEntityTypes.Returns(entityTypesDbSet);

        var donationTypesDbSet = _donationTypes.BuildMockDbSet();
        _contextMock.LookupDonationTypes.Returns(donationTypesDbSet);

        var donationFrequenciesDbSet = _donationFrequencies.BuildMockDbSet();
        _contextMock.LookupDonationFrequencies.Returns(donationFrequenciesDbSet);

        var bbbeeStatusesDbSet = _bbbeeStatuses.BuildMockDbSet();
        _contextMock.LookupBbbeeStatuses.Returns(bbbeeStatusesDbSet);

        _handler = new GetLookupQueryHandler(_contextMock, mapper);
    }

    [Fact]
    public async Task Handle_CompanyTypes_ReturnsExpectedIdNamePairs()
    {
        _companyTypes.Add(new LookupCompanyType { Id = 1, Name = "Manufacturer", IsActive = true, SortOrder = 1 });
        _companyTypes.Add(new LookupCompanyType { Id = 2, Name = "Distributor", IsActive = true, SortOrder = 2 });

        var result = await _handler.Handle(new GetLookupQuery(LookupType.CompanyTypes), CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.Id == 1 && r.Name == "Manufacturer");
        Assert.Contains(result, r => r.Id == 2 && r.Name == "Distributor");
    }

    [Fact]
    public async Task Handle_EntityTypes_ReturnsExpectedIdNamePairs()
    {
        _entityTypes.Add(new LookupEntityType { Id = 1, Name = "Pty Ltd", IsActive = true, SortOrder = 1 });

        var result = await _handler.Handle(new GetLookupQuery(LookupType.EntityTypes), CancellationToken.None);

        var item = Assert.Single(result);
        Assert.Equal(1, item.Id);
        Assert.Equal("Pty Ltd", item.Name);
    }

    [Fact]
    public async Task Handle_DonationTypes_ReturnsExpectedIdNamePairs()
    {
        _donationTypes.Add(new LookupDonationType { Id = 1, Name = "Food", IsActive = true, SortOrder = 1 });
        _donationTypes.Add(new LookupDonationType { Id = 2, Name = "Non-Food", IsActive = true, SortOrder = 2 });

        var result = await _handler.Handle(new GetLookupQuery(LookupType.DonationTypes), CancellationToken.None);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task Handle_DonationFrequencies_ReturnsExpectedIdNamePairs()
    {
        _donationFrequencies.Add(new LookupDonationFrequency { Id = 1, Name = "Weekly", IsActive = true, SortOrder = 1 });

        var result = await _handler.Handle(new GetLookupQuery(LookupType.DonationFrequencies), CancellationToken.None);

        var item = Assert.Single(result);
        Assert.Equal("Weekly", item.Name);
    }

    [Fact]
    public async Task Handle_BbbeeStatuses_ReturnsExpectedIdNamePairs()
    {
        _bbbeeStatuses.Add(new LookupBbbeeStatus { Id = 1, Name = "Level 1", IsActive = true, SortOrder = 1 });

        var result = await _handler.Handle(new GetLookupQuery(LookupType.BbbeeStatuses), CancellationToken.None);

        var item = Assert.Single(result);
        Assert.Equal("Level 1", item.Name);
    }

    [Fact]
    public async Task Handle_EmptyLookupTable_ReturnsEmptyList()
    {
        var result = await _handler.Handle(new GetLookupQuery(LookupType.CompanyTypes), CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task Handle_InactiveRow_IsExcluded()
    {
        _companyTypes.Add(new LookupCompanyType { Id = 1, Name = "Manufacturer", IsActive = true, SortOrder = 1 });
        _companyTypes.Add(new LookupCompanyType { Id = 2, Name = "Retired Type", IsActive = false, SortOrder = 2 });

        var result = await _handler.Handle(new GetLookupQuery(LookupType.CompanyTypes), CancellationToken.None);

        var item = Assert.Single(result);
        Assert.Equal("Manufacturer", item.Name);
    }

    [Fact]
    public async Task Handle_ReturnsRowsOrderedBySortOrder()
    {
        _companyTypes.Add(new LookupCompanyType { Id = 1, Name = "Second", IsActive = true, SortOrder = 2 });
        _companyTypes.Add(new LookupCompanyType { Id = 2, Name = "First", IsActive = true, SortOrder = 1 });

        var result = await _handler.Handle(new GetLookupQuery(LookupType.CompanyTypes), CancellationToken.None);

        Assert.Equal(["First", "Second"], result.Select(r => r.Name));
    }
}
