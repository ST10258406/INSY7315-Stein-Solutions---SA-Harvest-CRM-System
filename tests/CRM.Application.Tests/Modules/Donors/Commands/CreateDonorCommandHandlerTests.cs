using AutoMapper;
using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Donors.Commands.CreateDonor;
using CRM.Application.Modules.Donors.Dtos;
using CRM.Application.Modules.Donors.Mappings;
using CRM.Application.Modules.Lookups.Mappings;
using CRM.Domain.Entities;
using CRM.Domain.Entities.Lookups;
using CRM.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.NSubstitute;
using NSubstitute;

namespace CRM.Application.Tests.Modules.Donors.Commands;

public class CreateDonorCommandHandlerTests
{
    private readonly IApplicationDbContext _contextMock;
    private readonly ICurrentUserService _currentUserServiceMock;
    private readonly INotificationService _notificationServiceMock;
    private readonly IMapper _mapper;
    private readonly CreateDonorCommandHandler _handler;

    private readonly List<Donor> _donors = [];
    private readonly List<DonorApproval> _approvals = [];
    private readonly Guid _currentUserId = Guid.NewGuid();

    private readonly LookupCompanyType _companyType = new() { Id = 1, Name = "Manufacturer", IsActive = true };
    private readonly LookupEntityType _entityType = new() { Id = 1, Name = "Private Company", IsActive = true };
    private readonly LookupDonationFrequency _frequency = new() { Id = 1, Name = "Monthly", IsActive = true };
    private readonly LookupProvince _province = new() { Id = 3, Code = "GP", Name = "Gauteng", IsActive = true };
    private readonly LookupOperationalRegion _region = new() { Id = 1, Code = "JHB", Name = "Johannesburg", IsActive = true };
    private readonly LookupDonationType _donationType = new() { Id = 1, Name = "Meat", IsActive = true };

    public CreateDonorCommandHandlerTests()
    {
        _contextMock = Substitute.For<IApplicationDbContext>();
        _currentUserServiceMock = Substitute.For<ICurrentUserService>();
        _notificationServiceMock = Substitute.For<INotificationService>();

        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<DonorMappingProfile>();
            cfg.AddProfile<LookupsMappingProfile>();
        }, NullLoggerFactory.Instance);
        _mapper = config.CreateMapper();

        _currentUserServiceMock.GetCurrentUserId().Returns(_currentUserId);

        var donorsDbSet = _donors.BuildMockDbSet();
        _contextMock.Donors.Returns(donorsDbSet);
        _contextMock.Donors.Add(Arg.Do<Donor>(SimulateDatabaseJoin));

        var approvalsDbSet = _approvals.BuildMockDbSet();
        _contextMock.DonorApprovals.Returns(approvalsDbSet);
        _contextMock.DonorApprovals.Add(Arg.Do<DonorApproval>(a => _approvals.Add(a)));

        _handler = new CreateDonorCommandHandler(_contextMock, _mapper, _notificationServiceMock, _currentUserServiceMock);
    }

    // The handler re-queries via ProjectTo after SaveChangesAsync, which against a real
    // EF provider resolves navigation properties through server-side joins. Against the
    // in-memory mock DbSet there's no join engine, so this stands in for "what the DB
    // would have returned" by wiring up the same navigation properties by Id.
    private void SimulateDatabaseJoin(Donor donor)
    {
        donor.CompanyType = _companyType;
        donor.EntityType = _entityType;
        donor.DonationFrequency = _frequency;
        donor.CreatedByUser = new User { Id = donor.CreatedByUserId, FirstName = "Creator", LastName = "User", Email = "creator@test.com" };

        if (donor.LegalAddress is not null)
            donor.LegalAddress.Province = _province;

        foreach (var region in donor.OperationalRegions)
            region.OperationalRegion = _region;

        foreach (var donationType in donor.DonationTypes)
            donationType.DonationType = _donationType;

        _donors.Add(donor);
    }

    private void SetupAdmins(int adminCount, int nonAdminCount = 0)
    {
        var adminRole = new Role { Id = Guid.NewGuid(), Name = "Admin" };
        var otherRole = new Role { Id = Guid.NewGuid(), Name = "Procurement" };
        var users = new List<User>();

        for (var i = 0; i < adminCount; i++)
        {
            var admin = new User { Id = Guid.NewGuid(), Email = $"admin{i}@test.com", FirstName = "Admin", LastName = $"{i}", IsActive = true };
            admin.UserRoles.Add(new UserRole { UserId = admin.Id, User = admin, RoleId = adminRole.Id, Role = adminRole });
            users.Add(admin);
        }

        for (var i = 0; i < nonAdminCount; i++)
        {
            var other = new User { Id = Guid.NewGuid(), Email = $"other{i}@test.com", FirstName = "Other", LastName = $"{i}", IsActive = true };
            other.UserRoles.Add(new UserRole { UserId = other.Id, User = other, RoleId = otherRole.Id, Role = otherRole });
            users.Add(other);
        }

        var usersDbSet = users.BuildMockDbSet();
        _contextMock.Users.Returns(usersDbSet);
    }

    private static CreateDonorRequest MakeValidRequest(short? statusLikeFieldsIgnored = null) => new()
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
        MarketingContact = new CreateDonorContactRequest { Name = "Marketing Person", Phone = "+27820000001", Email = "marketing@test.co.za" },
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
    public async Task Handle_ValidCommand_PersistsDonorApprovalAndNotifiesAdmins()
    {
        SetupAdmins(adminCount: 2, nonAdminCount: 1);
        var command = new CreateDonorCommand { Request = MakeValidRequest() };

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.Single(_donors);
        Assert.Single(_approvals);
        Assert.Equal(ApprovalStatus.Pending, _approvals[0].Status);
        Assert.Equal(_donors[0].Id, _approvals[0].DonorId);
        Assert.Equal(_currentUserId, _approvals[0].RequestedByUserId);

        await _contextMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _notificationServiceMock.Received(2).CreateAsync(
            Arg.Any<Guid>(), NotificationType.NewDonorPendingReview, Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<Guid?>(), Arg.Any<string?>());

        Assert.Equal("PendingReview", result.Status);
        Assert.Equal("ManualCapture", result.SubmissionSource);
    }

    [Fact]
    public async Task Handle_AlwaysSetsPendingReviewAndManualCapture_RegardlessOfInputShape()
    {
        SetupAdmins(adminCount: 0);
        var command = new CreateDonorCommand { Request = MakeValidRequest() };

        await _handler.Handle(command, CancellationToken.None);

        // CreateDonorRequest structurally has no Status/SubmissionSource property to
        // pass through — proving there is no path by which a caller can override these.
        Assert.Null(typeof(CreateDonorRequest).GetProperty("Status"));
        Assert.Null(typeof(CreateDonorRequest).GetProperty("SubmissionSource"));

        Assert.Equal(DonorStatus.PendingReview, _donors[0].Status);
        Assert.Equal(SubmissionSource.ManualCapture, _donors[0].SubmissionSource);
    }

    [Fact]
    public async Task Handle_JunctionRows_PersistWithCompositeKeys()
    {
        SetupAdmins(adminCount: 0);
        var request = MakeValidRequest();
        request.Donations.RegionIds = [1];
        request.Donations.TypeIds = [1];
        var command = new CreateDonorCommand { Request = request };

        await _handler.Handle(command, CancellationToken.None);

        var donor = _donors[0];
        var region = Assert.Single(donor.OperationalRegions);
        Assert.Equal(donor.Id, region.DonorId);
        Assert.Equal((short)1, region.OperationalRegionId);

        var donationType = Assert.Single(donor.DonationTypes);
        Assert.Equal(donor.Id, donationType.DonorId);
        Assert.Equal((short)1, donationType.DonationTypeId);
    }

    [Fact]
    public async Task Handle_NoAdmins_DoesNotCallNotificationService()
    {
        SetupAdmins(adminCount: 0, nonAdminCount: 1);
        var command = new CreateDonorCommand { Request = MakeValidRequest() };

        await _handler.Handle(command, CancellationToken.None);

        await _notificationServiceMock.DidNotReceive().CreateAsync(
            Arg.Any<Guid>(), Arg.Any<NotificationType>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<Guid?>(), Arg.Any<string?>());
    }

    [Fact]
    public async Task Handle_SetsEntityIdForAuditBehaviour()
    {
        SetupAdmins(adminCount: 0);
        var command = new CreateDonorCommand { Request = MakeValidRequest() };

        await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(_donors[0].Id, command.EntityId);
        Assert.NotEqual(Guid.Empty, command.EntityId);
        Assert.NotNull(command.NewValues);
    }
}
