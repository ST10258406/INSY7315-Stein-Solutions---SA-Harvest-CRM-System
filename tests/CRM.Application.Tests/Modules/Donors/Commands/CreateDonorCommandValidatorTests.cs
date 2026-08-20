using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Donors.Commands.CreateDonor;
using CRM.Application.Modules.Donors.Dtos;
using CRM.Domain.Entities;
using CRM.Domain.Entities.Lookups;
using MockQueryable.NSubstitute;
using NSubstitute;

namespace CRM.Application.Tests.Modules.Donors.Commands;

public class CreateDonorCommandValidatorTests
{
    private readonly IApplicationDbContext _contextMock;
    private readonly CreateDonorCommandValidator _validator;

    public CreateDonorCommandValidatorTests()
    {
        _contextMock = Substitute.For<IApplicationDbContext>();

        // Each mock DbSet is built as its own statement before being handed to
        // Returns() — chaining BuildMockDbSet() straight into Returns() confuses
        // NSubstitute's call-tracking (its internal substitute calls overwrite
        // the "last call" the outer Returns() is trying to configure).
        var companyTypes = new List<LookupCompanyType> { new() { Id = 1, Name = "Manufacturer", IsActive = true } }.BuildMockDbSet();
        _contextMock.LookupCompanyTypes.Returns(companyTypes);

        var entityTypes = new List<LookupEntityType> { new() { Id = 1, Name = "Private Company", IsActive = true } }.BuildMockDbSet();
        _contextMock.LookupEntityTypes.Returns(entityTypes);

        var provinces = new List<LookupProvince> { new() { Id = 3, Code = "GP", Name = "Gauteng", IsActive = true } }.BuildMockDbSet();
        _contextMock.LookupProvinces.Returns(provinces);

        var frequencies = new List<LookupDonationFrequency> { new() { Id = 1, Name = "Monthly", IsActive = true } }.BuildMockDbSet();
        _contextMock.LookupDonationFrequencies.Returns(frequencies);

        var donationTypes = new List<LookupDonationType> { new() { Id = 1, Name = "Meat", IsActive = true } }.BuildMockDbSet();
        _contextMock.LookupDonationTypes.Returns(donationTypes);

        var regions = new List<LookupOperationalRegion> { new() { Id = 1, Code = "JHB", Name = "Johannesburg", IsActive = true } }.BuildMockDbSet();
        _contextMock.LookupOperationalRegions.Returns(regions);

        var bbbeeStatuses = new List<LookupBbbeeStatus>().BuildMockDbSet();
        _contextMock.LookupBbbeeStatuses.Returns(bbbeeStatuses);

        var users = new List<User>().BuildMockDbSet();
        _contextMock.Users.Returns(users);

        _validator = new CreateDonorCommandValidator(_contextMock);
    }

    private static CreateDonorRequest MakeValidRequest() => new()
    {
        Company = new CreateDonorCompanyRequest
        {
            CompanyName = "Test Co",
            CompanyTypeId = 1,
            Website = "https://test.co.za",
            RegisteredCompanyName = "Test Co (Pty) Ltd",
            TradingName = "Test Co",
            EntityTypeId = 1,
            CompanyRegistrationNumber = "2020/000000/07",
            IncomeTaxNumber = "9012345678"
        },
        PrimaryContact = new CreateDonorContactRequest { Name = "Jane Tester", Phone = "+27820000000", Email = "jane@test.co.za" },
        LegalAddress = new CreateDonorLegalAddressRequest
        {
            StreetAddress = "1 Test Street",
            Suburb = "Testville",
            City = "Johannesburg",
            ProvinceId = 3,
            PostalCode = "2000"
        },
        Donations = new CreateDonorDonationsRequest
        {
            FrequencyId = 1,
            TypeIds = [1],
            CollectionAddress = "Gate 1",
            RegionIds = [1]
        }
    };

    [Fact]
    public async Task ValidRequest_HasNoErrors()
    {
        var command = new CreateDonorCommand { Request = MakeValidRequest() };

        var result = await _validator.ValidateAsync(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task MissingCompany_HasError()
    {
        var request = MakeValidRequest();
        request.Company = null!;
        var command = new CreateDonorCommand { Request = request };

        var result = await _validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Request.Company");
    }

    [Theory]
    [InlineData("4012345678")]
    [InlineData("40")]
    public async Task IncomeTaxNumberStartingWith4_HasError(string tin)
    {
        var request = MakeValidRequest();
        request.Company.IncomeTaxNumber = tin;
        var command = new CreateDonorCommand { Request = request };

        var result = await _validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == "Request.Company.IncomeTaxNumber" &&
            e.ErrorMessage == "Income tax number cannot start with 4.");
    }

    [Fact]
    public async Task EmptyRegionIds_HasError()
    {
        var request = MakeValidRequest();
        request.Donations.RegionIds = [];
        var command = new CreateDonorCommand { Request = request };

        var result = await _validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Request.Donations.RegionIds");
    }

    [Fact]
    public async Task EmptyTypeIds_HasError()
    {
        var request = MakeValidRequest();
        request.Donations.TypeIds = [];
        var command = new CreateDonorCommand { Request = request };

        var result = await _validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Request.Donations.TypeIds");
    }

    [Fact]
    public async Task FrequencyIdNotInLookup_HasError()
    {
        var request = MakeValidRequest();
        request.Donations.FrequencyId = 999;
        var command = new CreateDonorCommand { Request = request };

        var result = await _validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Request.Donations.FrequencyId");
    }

    [Fact]
    public async Task RegionIdNotInLookup_HasError()
    {
        var request = MakeValidRequest();
        request.Donations.RegionIds = [999];
        var command = new CreateDonorCommand { Request = request };

        var result = await _validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Request.Donations.RegionIds");
    }

    [Fact]
    public async Task MissingCollectionAddress_HasError()
    {
        var request = MakeValidRequest();
        request.Donations.CollectionAddress = string.Empty;
        var command = new CreateDonorCommand { Request = request };

        var result = await _validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Request.Donations.CollectionAddress");
    }

    [Fact]
    public async Task MissingPrimaryContactEmail_HasError()
    {
        var request = MakeValidRequest();
        request.PrimaryContact.Email = string.Empty;
        var command = new CreateDonorCommand { Request = request };

        var result = await _validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Request.PrimaryContact.Email");
    }
}
