using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Donors.Commands.CreateDonor;
using CRM.Application.Modules.Donors.Dtos;
using NSubstitute;

namespace CRM.Application.Tests.Modules.Donors.Commands;

public class CreateDonorCommandValidatorTests
{
    private readonly ILookupRepository _lookupsMock = Substitute.For<ILookupRepository>();
    private readonly IUserRepository _usersMock = Substitute.For<IUserRepository>();
    private readonly CreateDonorCommandValidator _validator;

    // The only active reference data these tests know about. Anything else is
    // inactive/unknown, which is the default NSubstitute returns (false / 0).
    private static readonly short[] ActiveDonationTypeIds = [1];
    private static readonly short[] ActiveRegionIds = [1];

    public CreateDonorCommandValidatorTests()
    {
        _lookupsMock.CompanyTypeExistsActiveAsync(1, Arg.Any<CancellationToken>()).Returns(true);
        _lookupsMock.EntityTypeExistsActiveAsync(1, Arg.Any<CancellationToken>()).Returns(true);
        _lookupsMock.ProvinceExistsActiveAsync(3, Arg.Any<CancellationToken>()).Returns(true);
        _lookupsMock.DonationFrequencyExistsActiveAsync(1, Arg.Any<CancellationToken>()).Returns(true);

        _lookupsMock.CountActiveDonationTypesAsync(Arg.Any<IEnumerable<short>>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.ArgAt<IEnumerable<short>>(0).Count(ActiveDonationTypeIds.Contains));

        _lookupsMock.CountActiveOperationalRegionsAsync(Arg.Any<IEnumerable<short>>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.ArgAt<IEnumerable<short>>(0).Count(ActiveRegionIds.Contains));

        // No active BBBEE statuses and no active users configured — both default to
        // "not found", matching the original fixture's empty tables.
        _validator = new CreateDonorCommandValidator(_lookupsMock, _usersMock);
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
