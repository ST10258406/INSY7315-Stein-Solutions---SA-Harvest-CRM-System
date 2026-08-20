using AutoMapper;
using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Donors.Commands.UpdateDonor;
using CRM.Application.Modules.Donors.Dtos;
using CRM.Application.Modules.Donors.Mappings;
using CRM.Domain.Entities;
using CRM.Domain.Entities.Lookups;
using CRM.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.NSubstitute;
using NSubstitute;

namespace CRM.Application.Tests.Modules.Donors.Commands;

public class UpdateDonorCommandHandlerTests
{
    private readonly IApplicationDbContext _contextMock;
    private readonly IMapper _mapper;
    private readonly UpdateDonorCommandHandler _handler;

    public UpdateDonorCommandHandlerTests()
    {
        _contextMock = Substitute.For<IApplicationDbContext>();

        var config = new MapperConfiguration(cfg => cfg.AddProfile<DonorMappingProfile>(), NullLoggerFactory.Instance);
        _mapper = config.CreateMapper();

        _handler = new UpdateDonorCommandHandler(_contextMock, _mapper);
    }

    private void SetupDonors(List<Donor> donors)
    {
        var mockDonorsDbSet = donors.BuildMockDbSet();
        _contextMock.Donors.Returns(mockDonorsDbSet);
    }

    private static Donor MakeFullyPopulatedDonor()
    {
        var companyType = new LookupCompanyType { Id = 1, Name = "Manufacturer", IsActive = true };
        var entityType = new LookupEntityType { Id = 1, Name = "Private Company", IsActive = true };
        var frequency = new LookupDonationFrequency { Id = 2, Name = "Monthly", IsActive = true };
        var province = new LookupProvince { Id = 3, Code = "GP", Name = "Gauteng", IsActive = true };
        var region = new LookupOperationalRegion { Id = 1, Code = "JHB", Name = "Johannesburg", IsActive = true };
        var donationType = new LookupDonationType { Id = 3, Name = "Meat", IsActive = true };
        var creator = new User { Id = Guid.NewGuid(), FirstName = "Creator", LastName = "User", Email = "creator@test.com" };

        var donor = new Donor
        {
            Id = Guid.NewGuid(),
            CompanyName = "FoodCorp SA",
            CompanyTypeId = companyType.Id,
            CompanyType = companyType,
            Website = "https://foodcorp.co.za",
            RegisteredCompanyName = "FoodCorp (Pty) Ltd",
            TradingName = "FoodCorp SA",
            EntityTypeId = entityType.Id,
            EntityType = entityType,
            CompanyRegistrationNumber = "2010/012345/07",
            IncomeTaxNumber = "9012345678",
            DonationFrequencyId = frequency.Id,
            DonationFrequency = frequency,
            CollectionAddress = "Gate 3, 12 Industrial Rd",
            OperationsLogisticsDetails = "Contact John before arrival",
            AdditionalInformation = "Prefers morning calls",
            Status = DonorStatus.Active,
            SubmissionSource = SubmissionSource.PublicForm,
            MarketingConsent = false,
            ImpactReportingPreferences = "Quarterly PDF via email",
            CreatedByUserId = creator.Id,
            CreatedByUser = creator
        };

        donor.LegalAddress = new DonorLegalAddress
        {
            DonorId = donor.Id,
            Donor = donor,
            StreetAddress = "12 Industrial Road",
            Suburb = "Meadowdale",
            City = "Johannesburg",
            ProvinceId = province.Id,
            Province = province,
            PostalCode = "1609"
        };

        donor.Contacts = new List<DonorContact>
        {
            new() { DonorId = donor.Id, Donor = donor, ContactType = ContactType.Primary, Name = "John Smith", JobTitle = "Operations Manager", Phone = "+27821234567", Email = "john@foodcorp.co.za" }
        };

        donor.OperationalRegions = new List<DonorOperationalRegion>
        {
            new() { DonorId = donor.Id, Donor = donor, OperationalRegionId = region.Id, OperationalRegion = region }
        };

        donor.DonationTypes = new List<DonorDonationType>
        {
            new() { DonorId = donor.Id, Donor = donor, DonationTypeId = donationType.Id, DonationType = donationType }
        };

        return donor;
    }

    [Fact]
    public async Task Handle_UpdateOnlyCompanyName_LeavesEverythingElseUntouched()
    {
        var donor = MakeFullyPopulatedDonor();
        SetupDonors(new List<Donor> { donor });

        var command = new UpdateDonorCommand
        {
            Id = donor.Id,
            Request = new UpdateDonorRequest
            {
                Company = new UpdateDonorCompanyRequest { CompanyName = "New Name Pty Ltd" }
            }
        };

        await _handler.Handle(command, CancellationToken.None);

        Assert.Equal("New Name Pty Ltd", donor.CompanyName);

        // Untouched fields
        Assert.Equal("FoodCorp (Pty) Ltd", donor.RegisteredCompanyName);
        Assert.Equal("FoodCorp SA", donor.TradingName);
        Assert.Equal("9012345678", donor.IncomeTaxNumber);
        Assert.Single(donor.Contacts);
        Assert.Equal("John Smith", donor.Contacts.First().Name);
        Assert.Equal("Meadowdale", donor.LegalAddress!.Suburb);
        Assert.Single(donor.OperationalRegions);
        Assert.Single(donor.DonationTypes);

        await _contextMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UpdateRegionIds_ReplacesJunctionRowsEntirely()
    {
        var donor = MakeFullyPopulatedDonor();
        SetupDonors(new List<Donor> { donor });

        var newRegion = new LookupOperationalRegion { Id = 5, Code = "CPT", Name = "Cape Town", IsActive = true };

        // Real EF fixes up navigation properties for newly attached junction rows
        // automatically via the change tracker. The mocked context doesn't, so this
        // stands in for that fixup ahead of the handler's post-save ProjectTo re-query.
        _contextMock.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(_ =>
        {
            foreach (var region in donor.OperationalRegions.Where(r => r.OperationalRegion is null))
                region.OperationalRegion = newRegion;
            return Task.FromResult(1);
        });

        var command = new UpdateDonorCommand
        {
            Id = donor.Id,
            Request = new UpdateDonorRequest
            {
                Donations = new UpdateDonorDonationsRequest { RegionIds = [newRegion.Id] }
            }
        };

        await _handler.Handle(command, CancellationToken.None);

        var region = Assert.Single(donor.OperationalRegions);
        Assert.Equal(newRegion.Id, region.OperationalRegionId);
        Assert.Equal(donor.Id, region.DonorId);
    }

    [Fact]
    public async Task Handle_MarketingContactSuppliedForDonorWithoutOne_CreatesContact()
    {
        var donor = MakeFullyPopulatedDonor();
        SetupDonors(new List<Donor> { donor });

        var command = new UpdateDonorCommand
        {
            Id = donor.Id,
            Request = new UpdateDonorRequest
            {
                MarketingContact = new UpdateDonorContactRequest { Name = "Sarah Jones", Email = "sarah@foodcorp.co.za", Phone = "+27831234567" }
            }
        };

        await _handler.Handle(command, CancellationToken.None);

        var marketingContact = donor.Contacts.SingleOrDefault(c => c.ContactType == ContactType.Marketing);
        Assert.NotNull(marketingContact);
        Assert.Equal("Sarah Jones", marketingContact!.Name);
        Assert.Equal(2, donor.Contacts.Count);
    }

    [Fact]
    public async Task Handle_NonExistentDonorId_ThrowsNotFoundException()
    {
        SetupDonors(new List<Donor>());

        var command = new UpdateDonorCommand { Id = Guid.NewGuid(), Request = new UpdateDonorRequest() };

        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_SetsEntityIdForAuditBehaviour()
    {
        var donor = MakeFullyPopulatedDonor();
        SetupDonors(new List<Donor> { donor });

        var command = new UpdateDonorCommand
        {
            Id = donor.Id,
            Request = new UpdateDonorRequest { Company = new UpdateDonorCompanyRequest { TradingName = "New Trading Name" } }
        };

        await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(donor.Id, command.EntityId);
        Assert.NotNull(command.NewValues);
    }

    [Fact]
    public async Task Handle_EmptyRequest_DoesNotChangeAnyFields()
    {
        var donor = MakeFullyPopulatedDonor();
        var originalName = donor.CompanyName;
        SetupDonors(new List<Donor> { donor });

        var command = new UpdateDonorCommand { Id = donor.Id, Request = new UpdateDonorRequest() };

        await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(originalName, donor.CompanyName);
        await _contextMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
