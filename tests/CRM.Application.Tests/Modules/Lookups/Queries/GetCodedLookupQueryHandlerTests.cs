using AutoMapper;
using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Lookups.Mappings;
using CRM.Application.Modules.Lookups.Queries.GetCodedLookup;
using CRM.Domain.Entities.Lookups;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.NSubstitute;
using NSubstitute;

namespace CRM.Application.Tests.Modules.Lookups.Queries;

public class GetCodedLookupQueryHandlerTests
{
    private readonly IApplicationDbContext _contextMock;
    private readonly GetCodedLookupQueryHandler _handler;

    private readonly List<LookupOperationalRegion> _regions = [];
    private readonly List<LookupProvince> _provinces = [];

    public GetCodedLookupQueryHandlerTests()
    {
        _contextMock = Substitute.For<IApplicationDbContext>();

        var config = new MapperConfiguration(cfg => cfg.AddProfile<LookupsMappingProfile>(), NullLoggerFactory.Instance);
        var mapper = config.CreateMapper();

        var regionsDbSet = _regions.BuildMockDbSet();
        _contextMock.LookupOperationalRegions.Returns(regionsDbSet);

        var provincesDbSet = _provinces.BuildMockDbSet();
        _contextMock.LookupProvinces.Returns(provincesDbSet);

        _handler = new GetCodedLookupQueryHandler(_contextMock, mapper);
    }

    [Fact]
    public async Task Handle_OperationalRegions_ReturnsIdCodeNamePairs()
    {
        _regions.Add(new LookupOperationalRegion { Id = 1, Code = "GP", Name = "Gauteng", IsActive = true, SortOrder = 1 });
        _regions.Add(new LookupOperationalRegion { Id = 2, Code = "WC", Name = "Western Cape", IsActive = true, SortOrder = 2 });

        var result = await _handler.Handle(new GetCodedLookupQuery(CodedLookupType.OperationalRegions), CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.Id == 1 && r.Code == "GP" && r.Name == "Gauteng");
        Assert.Contains(result, r => r.Id == 2 && r.Code == "WC" && r.Name == "Western Cape");
    }

    [Fact]
    public async Task Handle_Provinces_ReturnsIdCodeNamePairs()
    {
        _provinces.Add(new LookupProvince { Id = 1, Code = "GP", Name = "Gauteng", IsActive = true, SortOrder = 1 });

        var result = await _handler.Handle(new GetCodedLookupQuery(CodedLookupType.Provinces), CancellationToken.None);

        var item = Assert.Single(result);
        Assert.Equal(1, item.Id);
        Assert.Equal("GP", item.Code);
        Assert.Equal("Gauteng", item.Name);
    }

    [Fact]
    public async Task Handle_EmptyLookupTable_ReturnsEmptyList()
    {
        var result = await _handler.Handle(new GetCodedLookupQuery(CodedLookupType.Provinces), CancellationToken.None);

        Assert.Empty(result);
    }
}
