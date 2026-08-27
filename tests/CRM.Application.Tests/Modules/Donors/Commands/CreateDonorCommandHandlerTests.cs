using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Donors.Commands.CreateDonor;
using CRM.Application.Modules.Donors.Dtos;
using CRM.Domain.Entities;
using CRM.Domain.Enums;
using NSubstitute;

namespace CRM.Application.Tests.Modules.Donors.Commands;

public class CreateDonorCommandHandlerTests
{
    private readonly IDonorRepository _donorsMock = Substitute.For<IDonorRepository>();
    private readonly IUserRepository _usersMock = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _unitOfWorkMock = Substitute.For<IUnitOfWork>();
    private readonly INotificationService _notificationServiceMock = Substitute.For<INotificationService>();
    private readonly ICurrentUserService _currentUserServiceMock = Substitute.For<ICurrentUserService>();
    private readonly CreateDonorCommandHandler _handler;

    private readonly List<Donor> _donors = [];
    private readonly List<DonorApproval> _approvals = [];
    private readonly Guid _currentUserId = Guid.NewGuid();

    public CreateDonorCommandHandlerTests()
    {
        _currentUserServiceMock.GetCurrentUserId().Returns(_currentUserId);

        _donorsMock.AddAsync(Arg.Do<Donor>(_donors.Add), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _donorsMock.AddApprovalAsync(Arg.Do<DonorApproval>(_approvals.Add), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Stands in for the post-save re-read the handler performs through the same
        // projection GetDonorById uses; the projection itself is covered in
        // CRM.Infrastructure.Tests against a real database.
        _donorsMock.GetDetailByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var id = ci.ArgAt<Guid>(0);
                var donor = _donors.SingleOrDefault(d => d.Id == id);
                return donor is null
                    ? null
                    : new DonorDetailDto
                    {
                        Id = donor.Id,
                        Status = donor.Status.ToString(),
                        SubmissionSource = donor.SubmissionSource.ToString()
                    };
            });

        _usersMock.GetActiveUserIdsByRoleAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([]);

        _handler = new CreateDonorCommandHandler(
            _donorsMock, _usersMock, _unitOfWorkMock, _notificationServiceMock, _currentUserServiceMock);
    }

    private void SetupAdmins(int adminCount)
    {
        _usersMock.GetActiveUserIdsByRoleAsync("Admin", Arg.Any<CancellationToken>())
            .Returns(Enumerable.Range(0, adminCount).Select(_ => Guid.NewGuid()).ToList());
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
        SetupAdmins(adminCount: 2);
        var command = new CreateDonorCommand { Request = MakeValidRequest() };

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.Single(_donors);
        Assert.Single(_approvals);
        Assert.Equal(ApprovalStatus.Pending, _approvals[0].Status);
        Assert.Equal(_donors[0].Id, _approvals[0].DonorId);
        Assert.Equal(_currentUserId, _approvals[0].RequestedByUserId);

        // Donor, children and the approval commit in a single transaction.
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());

        await _notificationServiceMock.Received(2).CreateAsync(
            Arg.Any<Guid>(), NotificationType.NewDonorPendingReview, Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<Guid?>(), Arg.Any<string?>());

        Assert.Equal("PendingReview", result.Status);
        Assert.Equal("ManualCapture", result.SubmissionSource);
    }

    [Fact]
    public async Task Handle_AlwaysSetsPendingReviewAndManualCapture_RegardlessOfInputShape()
    {
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
        SetupAdmins(adminCount: 0);
        var command = new CreateDonorCommand { Request = MakeValidRequest() };

        await _handler.Handle(command, CancellationToken.None);

        await _notificationServiceMock.DidNotReceive().CreateAsync(
            Arg.Any<Guid>(), Arg.Any<NotificationType>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<Guid?>(), Arg.Any<string?>());
    }

    [Fact]
    public async Task Handle_FansOutOnlyToTheAdminRole()
    {
        var command = new CreateDonorCommand { Request = MakeValidRequest() };

        await _handler.Handle(command, CancellationToken.None);

        await _usersMock.Received(1).GetActiveUserIdsByRoleAsync("Admin", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SetsEntityIdForAuditBehaviour()
    {
        var command = new CreateDonorCommand { Request = MakeValidRequest() };

        await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(_donors[0].Id, command.EntityId);
        Assert.NotEqual(Guid.Empty, command.EntityId);
        Assert.NotNull(command.NewValues);
    }
}
