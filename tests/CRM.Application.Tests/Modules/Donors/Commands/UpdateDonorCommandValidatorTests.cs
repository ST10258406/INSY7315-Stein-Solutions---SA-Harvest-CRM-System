using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Donors.Commands.UpdateDonor;
using CRM.Application.Modules.Donors.Dtos;
using CRM.Domain.Entities;
using CRM.Domain.Entities.Lookups;
using MockQueryable.NSubstitute;
using NSubstitute;

namespace CRM.Application.Tests.Modules.Donors.Commands;

public class UpdateDonorCommandValidatorTests
{
    private readonly IApplicationDbContext _contextMock;
    private readonly UpdateDonorCommandValidator _validator;

    public UpdateDonorCommandValidatorTests()
    {
        _contextMock = Substitute.For<IApplicationDbContext>();

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

        var bbbeeStatuses = new List<LookupBbbeeStatus> { new() { Id = 1, Name = "Level 1", IsActive = true } }.BuildMockDbSet();
        _contextMock.LookupBbbeeStatuses.Returns(bbbeeStatuses);

        var users = new List<User> { new() { Id = _relationshipManagerId, Email = "rm@test.co.za", FirstName = "RM", LastName = "Test", IsActive = true } }.BuildMockDbSet();
        _contextMock.Users.Returns(users);

        _validator = new UpdateDonorCommandValidator(_contextMock);
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
