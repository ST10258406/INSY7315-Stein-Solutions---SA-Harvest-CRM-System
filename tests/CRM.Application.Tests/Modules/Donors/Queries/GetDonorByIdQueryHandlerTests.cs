using AutoMapper;
using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Donors.Mappings;
using CRM.Application.Modules.Donors.Queries.GetDonorById;
using CRM.Domain.Entities;
using CRM.Domain.Entities.Lookups;
using CRM.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.NSubstitute;
using NSubstitute;

namespace CRM.Application.Tests.Modules.Donors.Queries;

public class GetDonorByIdQueryHandlerTests
{
    private readonly IApplicationDbContext _contextMock;
    private readonly IMapper _mapper;
    private readonly GetDonorByIdQueryHandler _handler;

    public GetDonorByIdQueryHandlerTests()
    {
        _contextMock = Substitute.For<IApplicationDbContext>();

        var config = new MapperConfiguration(cfg => cfg.AddProfile<DonorMappingProfile>(), NullLoggerFactory.Instance);
        _mapper = config.CreateMapper();

        _handler = new GetDonorByIdQueryHandler(_contextMock, _mapper);
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
        var bbbeeStatus = new LookupBbbeeStatus { Id = 2, Name = "Level 2", IsActive = true };
        var province = new LookupProvince { Id = 3, Code = "GP", Name = "Gauteng", IsActive = true };
        var region = new LookupOperationalRegion { Id = 1, Code = "JHB", Name = "Johannesburg", IsActive = true };
        var donationType = new LookupDonationType { Id = 3, Name = "Meat", IsActive = true };
        var relationshipManager = new User { Id = Guid.NewGuid(), FirstName = "Jane", LastName = "Doe", Email = "jane@test.com" };
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
            BbbeeStatusId = bbbeeStatus.Id,
            BbbeeStatus = bbbeeStatus,
            CollectionAddress = "Gate 3, 12 Industrial Rd",
            OperationsLogisticsDetails = "Contact John before arrival",
            AdditionalInformation = "Prefers morning calls",
            RelationshipManagerId = relationshipManager.Id,
            RelationshipManager = relationshipManager,
            Status = DonorStatus.Active,
            SubmissionSource = SubmissionSource.PublicForm,
            MarketingConsent = true,
            MarketingConsentDate = new DateTime(2026, 1, 15, 8, 0, 0, DateTimeKind.Utc),
            ImpactReportingPreferences = "Quarterly PDF via email",
            FollowUpDate = new DateTime(2026, 8, 15),
            FoodspaceCompanyId = null,
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
            new() { DonorId = donor.Id, Donor = donor, ContactType = ContactType.Primary, Name = "John Smith", JobTitle = "Operations Manager", Phone = "+27821234567", Email = "john@foodcorp.co.za" },
            new() { DonorId = donor.Id, Donor = donor, ContactType = ContactType.Marketing, Name = "Sarah Jones", Phone = "+27831234567", Email = "sarah@foodcorp.co.za" },
            new() { DonorId = donor.Id, Donor = donor, ContactType = ContactType.Accounts, Name = "Mike Brown", Phone = "+27841234567", Email = "accounts@foodcorp.co.za" }
        };

        donor.OperationalRegions = new List<DonorOperationalRegion>
        {
            new() { DonorId = donor.Id, Donor = donor, OperationalRegionId = region.Id, OperationalRegion = region }
        };

        donor.DonationTypes = new List<DonorDonationType>
        {
            new() { DonorId = donor.Id, Donor = donor, DonationTypeId = donationType.Id, DonationType = donationType }
        };

        donor.Documents = new List<DonorDocument>
        {
            new()
            {
                Id = Guid.NewGuid(),
                DonorId = donor.Id,
                Donor = donor,
                DocumentType = DocumentType.BBBEECertificate,
                FileName = "bbbee_cert_2025.pdf",
                BlobStoragePath = "blob://donors/private/bbbee_cert_2025.pdf",
                CreatedAt = new DateTime(2026, 1, 15, 8, 0, 0, DateTimeKind.Utc)
            }
        };

        return donor;
    }

    [Fact]
    public async Task Handle_ExistingDonor_ReturnsCorrectDtoShapeForAllSections()
    {
        var donor = MakeFullyPopulatedDonor();
        SetupDonors(new List<Donor> { donor });

        var result = await _handler.Handle(new GetDonorByIdQuery(donor.Id), CancellationToken.None);

        Assert.Equal(donor.Id, result.Id);
        Assert.Equal("Active", result.Status);
        Assert.Equal("PublicForm", result.SubmissionSource);

        Assert.Equal("FoodCorp SA", result.Company.CompanyName);
        Assert.Equal("Manufacturer", result.Company.CompanyType.Name);
        Assert.Equal("Private Company", result.Company.EntityType.Name);

        Assert.Equal("John Smith", result.PrimaryContact.Name);
        Assert.Equal("Sarah Jones", result.MarketingContact!.Name);
        Assert.Equal("Mike Brown", result.AccountsContact!.Name);

        Assert.Equal("Meadowdale", result.LegalAddress!.Suburb);
        Assert.Equal("GP", result.LegalAddress.Province.Code);

        Assert.Equal("Monthly", result.Donations.Frequency.Name);
        Assert.Contains(result.Donations.Types, t => t.Name == "Meat");
        Assert.Contains(result.Donations.OperationalRegions, r => r.Code == "JHB");

        Assert.Equal("Level 2", result.Compliance.BbbeeStatus!.Name);
        var document = Assert.Single(result.Compliance.Documents);
        Assert.Equal("bbbee_cert_2025.pdf", document.OriginalFileName);

        Assert.Equal("Jane Doe", result.Crm.RelationshipManager!.FullName);
        Assert.True(result.Crm.MarketingConsent);
    }

    [Fact]
    public async Task Handle_DocumentPayload_NeverContainsBlobStorageUrl()
    {
        var donor = MakeFullyPopulatedDonor();
        SetupDonors(new List<Donor> { donor });

        var result = await _handler.Handle(new GetDonorByIdQuery(donor.Id), CancellationToken.None);

        var documentDtoType = result.Compliance.Documents.Single().GetType();
        Assert.DoesNotContain(documentDtoType.GetProperties(), p =>
            p.Name.Contains("Blob", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Handle_NonExistentId_ThrowsNotFoundException()
    {
        SetupDonors(new List<Donor>());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(new GetDonorByIdQuery(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_DonorWithOnlyPrimaryContact_MarketingAndAccountsContactsAreNull()
    {
        var donor = MakeFullyPopulatedDonor();
        donor.Contacts = donor.Contacts.Where(c => c.ContactType == ContactType.Primary).ToList();
        SetupDonors(new List<Donor> { donor });

        var result = await _handler.Handle(new GetDonorByIdQuery(donor.Id), CancellationToken.None);

        Assert.NotNull(result.PrimaryContact);
        Assert.Null(result.MarketingContact);
        Assert.Null(result.AccountsContact);
    }
}
