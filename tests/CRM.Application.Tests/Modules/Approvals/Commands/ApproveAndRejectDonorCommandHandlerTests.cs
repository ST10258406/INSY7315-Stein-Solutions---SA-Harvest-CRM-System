using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Approvals.Commands.ApproveDonor;
using CRM.Application.Modules.Approvals.Commands.RejectDonor;
using CRM.Domain.Entities;
using CRM.Domain.Enums;
using NSubstitute;
using AppValidationException = CRM.Application.Common.Exceptions.ValidationException;
using NotFoundException = CRM.Application.Common.Exceptions.NotFoundException;

namespace CRM.Application.Tests.Modules.Approvals.Commands;

public class ApproveAndRejectDonorCommandHandlerTests
{
    private readonly IApprovalRepository _approvalsMock = Substitute.For<IApprovalRepository>();
    private readonly IUnitOfWork _unitOfWorkMock = Substitute.For<IUnitOfWork>();
    private readonly INotificationService _notificationsMock = Substitute.For<INotificationService>();
    private readonly ICurrentUserService _currentUserMock = Substitute.For<ICurrentUserService>();

    private readonly Guid _reviewerId = Guid.NewGuid();
    private readonly ApproveDonorCommandHandler _approve;
    private readonly RejectDonorCommandHandler _reject;

    public ApproveAndRejectDonorCommandHandlerTests()
    {
        _currentUserMock.GetCurrentUserId().Returns(_reviewerId);
        _approve = new ApproveDonorCommandHandler(_approvalsMock, _unitOfWorkMock, _notificationsMock, _currentUserMock);
        _reject = new RejectDonorCommandHandler(_approvalsMock, _unitOfWorkMock, _notificationsMock, _currentUserMock);
    }

    private DonorApproval Seed(ApprovalStatus status = ApprovalStatus.Pending, Guid? relationshipManagerId = null)
    {
        var approval = new DonorApproval
        {
            Id = Guid.NewGuid(),
            DonorId = Guid.NewGuid(),
            RequestedByUserId = Guid.NewGuid(),
            Status = status,
            Donor = new Donor
            {
                CompanyName = "FoodCorp SA",
                Status = DonorStatus.PendingReview,
                RelationshipManagerId = relationshipManagerId
            }
        };
        approval.Donor.Id = approval.DonorId;
        _approvalsMock.GetForUpdateAsync(approval.Id, Arg.Any<CancellationToken>()).Returns(approval);
        return approval;
    }

    // ---- approve ---------------------------------------------------------

    [Fact]
    public async Task Approve_PendingApproval_SetsApprovedAndDonorActive_OneSave()
    {
        var rmId = Guid.NewGuid();
        var approval = Seed(relationshipManagerId: rmId);

        await _approve.Handle(new ApproveDonorCommand { ApprovalId = approval.Id }, CancellationToken.None);

        Assert.Equal(ApprovalStatus.Approved, approval.Status);
        Assert.Equal(_reviewerId, approval.ReviewedByUserId);
        Assert.NotNull(approval.ReviewedAt);
        Assert.Equal(DonorStatus.Active, approval.Donor.Status);
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Approve_NotifiesTheRelationshipManager_NotTheSubmitter()
    {
        var rmId = Guid.NewGuid();
        var approval = Seed(relationshipManagerId: rmId);

        await _approve.Handle(new ApproveDonorCommand { ApprovalId = approval.Id }, CancellationToken.None);

        await _notificationsMock.Received(1).CreateAsync(
            rmId,
            Arg.Any<string>(),
            Arg.Is<string>(m => m.Contains("FoodCorp SA")),
            NotificationType.DonorApproved,
            approval.DonorId,
            "Donor",
            Arg.Any<CancellationToken>());
        await _notificationsMock.DidNotReceive().CreateAsync(
            approval.RequestedByUserId, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<NotificationType>(),
            Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Approve_NoRelationshipManager_StillApproves_NoNotification()
    {
        var approval = Seed(relationshipManagerId: null);

        await _approve.Handle(new ApproveDonorCommand { ApprovalId = approval.Id }, CancellationToken.None);

        Assert.Equal(ApprovalStatus.Approved, approval.Status);
        await _notificationsMock.DidNotReceive().CreateAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<NotificationType>(),
            Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(ApprovalStatus.Approved)]
    [InlineData(ApprovalStatus.Rejected)]
    public async Task Approve_NotPending_Throws400_NoSave(ApprovalStatus status)
    {
        var approval = Seed(status);

        await Assert.ThrowsAsync<AppValidationException>(() =>
            _approve.Handle(new ApproveDonorCommand { ApprovalId = approval.Id }, CancellationToken.None));

        await _unitOfWorkMock.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Approve_MissingApproval_ThrowsNotFound()
    {
        _approvalsMock.GetForUpdateAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((DonorApproval?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _approve.Handle(new ApproveDonorCommand { ApprovalId = Guid.NewGuid() }, CancellationToken.None));
    }

    [Fact]
    public async Task Approve_SetsAuditFields()
    {
        var approval = Seed();
        var command = new ApproveDonorCommand { ApprovalId = approval.Id };

        await _approve.Handle(command, CancellationToken.None);

        Assert.Equal(approval.Id, command.EntityId);
        Assert.NotNull(command.NewValues);
        Assert.Equal(AuditAction.Approved, command.Action);
    }

    // ---- reject ---------------------------------------------------------

    [Fact]
    public async Task Reject_PendingApproval_SetsRejectedWithReasonAndDonorRejected()
    {
        var rmId = Guid.NewGuid();
        var approval = Seed(relationshipManagerId: rmId);

        await _reject.Handle(
            new RejectDonorCommand { ApprovalId = approval.Id, RejectionReason = "Failed vetting." },
            CancellationToken.None);

        Assert.Equal(ApprovalStatus.Rejected, approval.Status);
        Assert.Equal("Failed vetting.", approval.RejectionReason);
        Assert.Equal(_reviewerId, approval.ReviewedByUserId);
        Assert.Equal(DonorStatus.Rejected, approval.Donor.Status);
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Reject_NotificationIncludesTheReason_AndGoesToRelationshipManager()
    {
        var rmId = Guid.NewGuid();
        var approval = Seed(relationshipManagerId: rmId);

        await _reject.Handle(
            new RejectDonorCommand { ApprovalId = approval.Id, RejectionReason = "Failed vetting." },
            CancellationToken.None);

        await _notificationsMock.Received(1).CreateAsync(
            rmId,
            Arg.Any<string>(),
            Arg.Is<string>(m => m.Contains("Failed vetting.")),
            NotificationType.DonorRejected,
            approval.DonorId,
            "Donor",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Reject_NotPending_Throws400_NoSave()
    {
        var approval = Seed(ApprovalStatus.Approved);

        await Assert.ThrowsAsync<AppValidationException>(() =>
            _reject.Handle(new RejectDonorCommand { ApprovalId = approval.Id, RejectionReason = "x" }, CancellationToken.None));

        await _unitOfWorkMock.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Reject_MissingApproval_ThrowsNotFound()
    {
        _approvalsMock.GetForUpdateAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((DonorApproval?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _reject.Handle(new RejectDonorCommand { ApprovalId = Guid.NewGuid(), RejectionReason = "x" }, CancellationToken.None));
    }
}
