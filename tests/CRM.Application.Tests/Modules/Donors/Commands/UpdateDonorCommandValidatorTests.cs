using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Donors.Commands.UpdateDonor;
using CRM.Application.Modules.Donors.Dtos;
using NSubstitute;

namespace CRM.Application.Tests.Modules.Donors.Commands;

public class UpdateDonorCommandValidatorTests
{
    private readonly ILookupRepository _lookupsMock = Substitute.For<ILookupRepository>();
    private readonly IUserRepository _usersMock = Substitute.For<IUserRepository>();
    private readonly UpdateDonorCommandValidator _validator;

    // The only active reference data these tests know about. Anything else is
    // inactive/unknown, which is the default NSubstitute returns (false / 0).
    private static readonly short[] ActiveDonationTypeIds = [1];
    private static readonly short[] ActiveRegionIds = [1];

    public UpdateDonorCommandValidatorTests()
    {
        _lookupsMock.CompanyTypeExistsActiveAsync(1, Arg.Any<CancellationToken>()).Returns(true);
        _lookupsMock.EntityTypeExistsActiveAsync(1, Arg.Any<CancellationToken>()).Returns(true);
        _lookupsMock.ProvinceExistsActiveAsync(3, Arg.Any<CancellationToken>()).Returns(true);
        _lookupsMock.DonationFrequencyExistsActiveAsync(1, Arg.Any<CancellationToken>()).Returns(true);
        _lookupsMock.BbbeeStatusExistsActiveAsync(1, Arg.Any<CancellationToken>()).Returns(true);

        _lookupsMock.CountActiveDonationTypesAsync(Arg.Any<IEnumerable<short>>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.ArgAt<IEnumerable<short>>(0).Count(ActiveDonationTypeIds.Contains));

        _lookupsMock.CountActiveOperationalRegionsAsync(Arg.Any<IEnumerable<short>>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.ArgAt<IEnumerable<short>>(0).Count(ActiveRegionIds.Contains));

        _usersMock.ExistsAndActiveAsync(_relationshipManagerId, Arg.Any<CancellationToken>()).Returns(true);

        _validator = new UpdateDonorCommandValidator(_lookupsMock, _usersMock);
    }

    private readonly Guid _relationshipManagerId = Guid.NewGuid();

    [Fact]
    public async Task EmptyRequest_AllSectionsNull_IsValid()
    {
        var command = new UpdateDonorCommand { Id = Guid.NewGuid(), Request = new UpdateDonorRequest() };

        var result = await _validator.ValidateAsync(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task EmptyId_HasError()
    {
        var command = new UpdateDonorCommand { Id = Guid.Empty, Request = new UpdateDonorRequest() };

        var result = await _validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Id");
    }

    [Theory]
    [InlineData("4012345678")]
    [InlineData("40")]
    public async Task IncomeTaxNumberStartingWith4_HasError(string tin)
    {
        var command = new UpdateDonorCommand
        {
            Id = Guid.NewGuid(),
            Request = new UpdateDonorRequest { Company = new UpdateDonorCompanyRequest { IncomeTaxNumber = tin } }
        };

        var result = await _validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == "Request.Company.IncomeTaxNumber" &&
            e.ErrorMessage == "Income tax number cannot start with 4.");
    }

    [Fact]
    public async Task IncomeTaxNumberNotStartingWith4_IsValid()
    {
        var command = new UpdateDonorCommand
        {
            Id = Guid.NewGuid(),
            Request = new UpdateDonorRequest { Company = new UpdateDonorCompanyRequest { IncomeTaxNumber = "9012345678" } }
        };

        var result = await _validator.ValidateAsync(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Donations_EmptyRegionIdsButPresent_HasError()
    {
        var command = new UpdateDonorCommand
        {
            Id = Guid.NewGuid(),
            Request = new UpdateDonorRequest { Donations = new UpdateDonorDonationsRequest { RegionIds = [] } }
        };

        var result = await _validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Request.Donations.RegionIds");
    }

    [Fact]
    public async Task Donations_EmptyTypeIdsButPresent_HasError()
    {
        var command = new UpdateDonorCommand
        {
            Id = Guid.NewGuid(),
            Request = new UpdateDonorRequest { Donations = new UpdateDonorDonationsRequest { TypeIds = [] } }
        };

        var result = await _validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Request.Donations.TypeIds");
    }

    [Fact]
    public async Task Donations_BogusProvinceId_HasError()
    {
        var command = new UpdateDonorCommand
        {
            Id = Guid.NewGuid(),
            Request = new UpdateDonorRequest { LegalAddress = new UpdateDonorLegalAddressRequest { ProvinceId = 999 } }
        };

        var result = await _validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Request.LegalAddress.ProvinceId");
    }

    [Fact]
    public async Task BogusFrequencyId_HasError()
    {
        var command = new UpdateDonorCommand
        {
            Id = Guid.NewGuid(),
            Request = new UpdateDonorRequest { Donations = new UpdateDonorDonationsRequest { FrequencyId = 999 } }
        };

        var result = await _validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Request.Donations.FrequencyId");
    }

    [Fact]
    public async Task BogusCompanyTypeId_HasError()
    {
        var command = new UpdateDonorCommand
        {
            Id = Guid.NewGuid(),
            Request = new UpdateDonorRequest { Company = new UpdateDonorCompanyRequest { CompanyTypeId = 999 } }
        };

        var result = await _validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Request.Company.CompanyTypeId");
    }

    [Fact]
    public async Task EmptyCompanyNameWhenSupplied_HasError()
    {
        var command = new UpdateDonorCommand
        {
            Id = Guid.NewGuid(),
            Request = new UpdateDonorRequest { Company = new UpdateDonorCompanyRequest { CompanyName = string.Empty } }
        };

        var result = await _validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Request.Company.CompanyName");
    }

    [Fact]
    public async Task PrimaryContact_InvalidEmailWhenSupplied_HasError()
    {
        var command = new UpdateDonorCommand
        {
            Id = Guid.NewGuid(),
            Request = new UpdateDonorRequest { PrimaryContact = new UpdateDonorContactRequest { Email = "not-an-email" } }
        };

        var result = await _validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Request.PrimaryContact.Email");
    }

    [Fact]
    public async Task ValidRelationshipManagerId_IsValid()
    {
        var command = new UpdateDonorCommand
        {
            Id = Guid.NewGuid(),
            Request = new UpdateDonorRequest { Crm = new UpdateDonorCrmRequest { RelationshipManagerId = _relationshipManagerId } }
        };

        var result = await _validator.ValidateAsync(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task UnknownRelationshipManagerId_HasError()
    {
        var command = new UpdateDonorCommand
        {
            Id = Guid.NewGuid(),
            Request = new UpdateDonorRequest { Crm = new UpdateDonorCrmRequest { RelationshipManagerId = Guid.NewGuid() } }
        };

        var result = await _validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Request.Crm.RelationshipManagerId");
    }
}
