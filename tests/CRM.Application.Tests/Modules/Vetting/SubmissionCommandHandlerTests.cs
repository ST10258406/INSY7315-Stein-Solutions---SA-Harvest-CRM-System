using AutoMapper;
using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Vetting.Mappings;
using CRM.Application.Modules.Vetting.Submissions.Commands.CreateSubmission;
using CRM.Application.Modules.Vetting.Submissions.Commands.SubmitSubmission;
using CRM.Application.Modules.Vetting.Submissions.Commands.UpdateSubmission;
using CRM.Application.Modules.Vetting.Submissions.Dtos;
using CRM.Domain.Entities.Vetting;
using CRM.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace CRM.Application.Tests.Modules.Vetting;

public class SubmissionCommandHandlerTests
{
    private readonly IDonorRepository _donors = Substitute.For<IDonorRepository>();
    private readonly IVettingOfficerRepository _officers = Substitute.For<IVettingOfficerRepository>();
    private readonly IVettingSiteVisitRepository _visits = Substitute.For<IVettingSiteVisitRepository>();
    private readonly IVettingSubmissionRepository _submissions = Substitute.For<IVettingSubmissionRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IMapper _mapper =
        new MapperConfiguration(cfg => cfg.AddProfile<VettingProfile>(), NullLoggerFactory.Instance).CreateMapper();

    private readonly Guid _donorId = Guid.NewGuid();
    private readonly Guid _officerId = Guid.NewGuid();
    private readonly List<VettingSubmission> _added = [];

    public SubmissionCommandHandlerTests()
    {
        // The transaction boundary is Infrastructure's job; here it just runs the work.
        _unitOfWork.ExecuteInTransactionAsync(Arg.Any<Func<Task<bool>>>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Func<Task<bool>>>()());
        _donors.ExistsAsync(_donorId, Arg.Any<CancellationToken>()).Returns(true);
        _officers.ExistsAsync(_officerId, Arg.Any<CancellationToken>()).Returns(true);
        _submissions.GetNextFormReferenceAsync(Arg.Any<CancellationToken>()).Returns("VET-2026-0001");
        _submissions.AddAsync(Arg.Do<VettingSubmission>(_added.Add), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _submissions.GetDtoByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ci => new VettingSubmissionDto { Id = ci.ArgAt<Guid>(0) });
    }

    private CreateSubmissionCommandHandler Create() => new(_donors, _officers, _visits, _submissions, _unitOfWork, _mapper);
    private UpdateSubmissionCommandHandler Update() => new(_officers, _submissions, _unitOfWork, _mapper);
    private SubmitSubmissionCommandHandler Submit() => new(_officers, _visits, _submissions, _unitOfWork);

    private VettingSubmission Existing(VettingApprovalState state, bool photosCaptured = true)
    {
        var s = new VettingSubmission
        {
            Id = Guid.NewGuid(),
            FormReference = "VET-2026-0007",
            DonorId = _donorId,
            OfficerId = _officerId,
            ApprovalState = state,
            Details = VettingFixtures.CleanDetails(),
            Photos = Enum.GetValues<EvidencePhotoCategory>()
                .Select(c => new VettingEvidencePhoto { ShotIndex = (int)c, Category = c, IsCaptured = photosCaptured })
                .ToList()
        };
        _submissions.GetForUpdateAsync(s.Id, Arg.Any<CancellationToken>()).Returns(s);
        return s;
    }

    [Fact]
    public async Task Create_MakesNewSubmission_WithDetails_FourEmptyPhotoSlots_AndRecountsWorkload()
    {
        var command = new CreateSubmissionCommand { DonorId = _donorId, OfficerId = _officerId, Details = VettingFixtures.ValidDetailsDto() };

        var result = await Create().Handle(command, CancellationToken.None);

        var s = Assert.Single(_added);
        Assert.Equal("VET-2026-0001", result.FormReference);
        Assert.Equal(VettingApprovalState.New, s.ApprovalState);
        Assert.Equal("Sipho Dlamini", s.Details!.ContactName);
        Assert.Equal(["Mon", "Wed", "Fri"], s.Details.FeedingDays);
        Assert.Equal([0, 1, 2, 3], s.Photos.Select(p => p.ShotIndex));
        Assert.Equal(Enum.GetValues<EvidencePhotoCategory>(), s.Photos.Select(p => p.Category));
        Assert.All(s.Photos, p => Assert.False(p.IsCaptured));
        await _officers.Received(1).RecalculateWorkloadAsync(_officerId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_SiteVisitForAnotherDonor_Throws400_AndCreatesNothing()
    {
        var visit = new VettingSiteVisit { Id = Guid.NewGuid(), DonorId = Guid.NewGuid(), OfficerId = _officerId };
        _visits.GetForUpdateAsync(visit.Id, Arg.Any<CancellationToken>()).Returns(visit);

        await Assert.ThrowsAsync<ValidationException>(() => Create().Handle(new CreateSubmissionCommand
        {
            DonorId = _donorId, OfficerId = _officerId, SiteVisitId = visit.Id, Details = VettingFixtures.ValidDetailsDto()
        }, CancellationToken.None));

        Assert.Empty(_added);
        Assert.Equal(SiteVisitStatus.ToDo, visit.VisitStatus);
    }

    [Theory]
    [InlineData(SiteVisitStatus.Completed, false)]
    [InlineData(SiteVisitStatus.ToDo, true)]
    public async Task Create_SiteVisitCompletedOrAlreadyLinked_Throws409_AndLeavesItAlone(SiteVisitStatus status, bool linked)
    {
        var visit = new VettingSiteVisit { Id = Guid.NewGuid(), DonorId = _donorId, OfficerId = _officerId, VisitStatus = status };
        _visits.GetForUpdateAsync(visit.Id, Arg.Any<CancellationToken>()).Returns(visit);
        _submissions.IsSiteVisitLinkedAsync(visit.Id, Arg.Any<CancellationToken>()).Returns(linked);

        await Assert.ThrowsAsync<ConflictException>(() => Create().Handle(new CreateSubmissionCommand
        {
            DonorId = _donorId, OfficerId = _officerId, SiteVisitId = visit.Id, Details = VettingFixtures.ValidDetailsDto()
        }, CancellationToken.None));

        Assert.Equal(status, visit.VisitStatus);
        Assert.Empty(_added);
    }

    [Fact]
    public async Task Create_MatchingSiteVisit_MovesItToInProgress()
    {
        var visit = new VettingSiteVisit { Id = Guid.NewGuid(), DonorId = _donorId, OfficerId = _officerId };
        _visits.GetForUpdateAsync(visit.Id, Arg.Any<CancellationToken>()).Returns(visit);

        await Create().Handle(new CreateSubmissionCommand
        {
            DonorId = _donorId, OfficerId = _officerId, SiteVisitId = visit.Id, Details = VettingFixtures.ValidDetailsDto()
        }, CancellationToken.None);

        Assert.Equal(SiteVisitStatus.InProgress, visit.VisitStatus);
        Assert.Equal(visit.Id, Assert.Single(_added).SiteVisitId);
    }

    [Theory]
    [InlineData(VettingApprovalState.Ready)]
    [InlineData(VettingApprovalState.Approved)]
    [InlineData(VettingApprovalState.Declined)]
    public async Task Patch_WhenNotWithTheOfficer_Throws409(VettingApprovalState state)
    {
        var s = Existing(state);

        await Assert.ThrowsAsync<ConflictException>(() => Update().Handle(
            new UpdateSubmissionCommand { Id = s.Id, Details = VettingFixtures.ValidDetailsDto() }, CancellationToken.None));
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Patch_ReplacesDetails_ButKeepsUploadedPaths()
    {
        var s = Existing(VettingApprovalState.Returned);
        var detailsId = s.Details!.Id = Guid.NewGuid();

        await Update().Handle(new UpdateSubmissionCommand { Id = s.Id, Details = VettingFixtures.ValidDetailsDto() }, CancellationToken.None);

        Assert.Equal("Sipho Dlamini", s.Details.ContactName);
        Assert.Equal(detailsId, s.Details.Id);
        Assert.Equal("vetting/x/files/npo-certificate.pdf", s.Details.NpoCertificatePath);
    }

    [Fact]
    public async Task Submit_MissingPhotos_Throws400NamingThem_AndChangesNothing()
    {
        var s = Existing(VettingApprovalState.New);
        s.Photos.Single(p => p.Category == EvidencePhotoCategory.DryStorage).IsCaptured = false;
        s.Photos.Single(p => p.Category == EvidencePhotoCategory.ToiletsWater).IsCaptured = false;

        var ex = await Assert.ThrowsAsync<ValidationException>(() => Submit().Handle(new SubmitSubmissionCommand { Id = s.Id }, CancellationToken.None));

        Assert.Contains("DryStorage, ToiletsWater", Assert.Single(ex.Errors["Photos"]));
        Assert.Equal(VettingApprovalState.New, s.ApprovalState);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Submit_FromNew_MovesToReady_RaisesAutoFlags_CompletesVisit_AndRecounts()
    {
        var s = Existing(VettingApprovalState.New);
        s.Details!.SlaSigned = false;
        var visit = new VettingSiteVisit { Id = Guid.NewGuid(), DonorId = _donorId, OfficerId = _officerId, VisitStatus = SiteVisitStatus.InProgress };
        s.SiteVisitId = visit.Id;
        _visits.GetForUpdateAsync(visit.Id, Arg.Any<CancellationToken>()).Returns(visit);

        await Submit().Handle(new SubmitSubmissionCommand { Id = s.Id }, CancellationToken.None);

        Assert.Equal(VettingApprovalState.Ready, s.ApprovalState);
        Assert.NotNull(s.SubmittedAt);
        var flag = Assert.Single(s.Flags);
        Assert.Equal(("SLA not signed", VettingFlagSource.Auto, true), (flag.Description, flag.Source, flag.IsActive));
        Assert.Equal(SiteVisitStatus.Completed, visit.VisitStatus);
        await _officers.Received(1).RecalculateWorkloadAsync(_officerId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Resubmit_FromReturned_DeactivatesOldAutoFlags_KeepsReviewerFlags_AndClearsDecision()
    {
        var s = Existing(VettingApprovalState.Returned);
        s.AdminDecision = VettingDecision.Return;
        s.DecisionReason = "Fix the subtotals";
        var oldAuto = new VettingFlag { Id = Guid.NewGuid(), Description = "Gender subtotals (1) do not match total people served (2)", Source = VettingFlagSource.Auto };
        var reviewer = new VettingFlag { Id = Guid.NewGuid(), Description = "Check the fridge", Source = VettingFlagSource.Reviewer };
        s.Flags = [oldAuto, reviewer];

        await Submit().Handle(new SubmitSubmissionCommand { Id = s.Id }, CancellationToken.None);

        Assert.False(oldAuto.IsActive);
        Assert.True(reviewer.IsActive);
        Assert.Equal(2, s.Flags.Count); // clean details: no new auto flags
        Assert.Equal(VettingApprovalState.Ready, s.ApprovalState);
        Assert.Null(s.AdminDecision);
        Assert.Null(s.DecisionReason);
    }

    [Fact]
    public async Task Submit_WhenAlreadyReady_Throws409()
    {
        var s = Existing(VettingApprovalState.Ready);
        await Assert.ThrowsAsync<ConflictException>(() => Submit().Handle(new SubmitSubmissionCommand { Id = s.Id }, CancellationToken.None));
    }
}
