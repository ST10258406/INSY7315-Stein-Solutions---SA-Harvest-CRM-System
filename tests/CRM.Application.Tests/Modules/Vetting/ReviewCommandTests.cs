using AutoMapper;
using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Vetting.Mappings;
using CRM.Application.Modules.Vetting.Review.Commands.AddReviewerFlag;
using CRM.Application.Modules.Vetting.Review.Commands.DecideSubmission;
using CRM.Application.Modules.Vetting.Submissions.Dtos;
using CRM.Domain.Entities.Vetting;
using CRM.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace CRM.Application.Tests.Modules.Vetting;

public class ReviewCommandTests
{
    private readonly IVettingSubmissionRepository _submissions = Substitute.For<IVettingSubmissionRepository>();
    private readonly IVettingOfficerRepository _officers = Substitute.For<IVettingOfficerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IMapper _mapper =
        new MapperConfiguration(cfg => cfg.AddProfile<VettingProfile>(), NullLoggerFactory.Instance).CreateMapper();

    private readonly Guid _id = Guid.NewGuid();
    private readonly Guid _officerId = Guid.NewGuid();

    public ReviewCommandTests()
    {
        _unitOfWork.ExecuteInTransactionAsync(Arg.Any<Func<Task<bool>>>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Func<Task<bool>>>()());
        _submissions.GetOfficerIdAsync(_id, Arg.Any<CancellationToken>()).Returns(_officerId);
        _submissions.GetDtoByIdAsync(_id, Arg.Any<CancellationToken>()).Returns(new VettingSubmissionDto { Id = _id });
    }

    private DecideSubmissionCommandHandler Decide() => new(_submissions, _officers, _unitOfWork);

    // ---- validator

    [Theory]
    [InlineData("Approve", null, true)]
    [InlineData("approve", "  ", true)]
    [InlineData("Return", "Fix the subtotals", true)]
    [InlineData("Return", null, false)]
    [InlineData("Decline", "   ", false)]
    [InlineData("Decline", "", false)]
    [InlineData("1", "x", false)]       // numeric enum value must not slip through
    [InlineData("Escalate", "x", false)]
    public void Validator(string decision, string? reason, bool valid)
        => Assert.Equal(valid, new DecideSubmissionCommandValidator()
            .Validate(new DecideSubmissionCommand { Id = _id, Decision = decision, Reason = reason }).IsValid);

    [Fact]
    public void Validator_ReasonOver2000_Fails()
        => Assert.False(new DecideSubmissionCommandValidator()
            .Validate(new DecideSubmissionCommand { Id = _id, Decision = "Approve", Reason = new string('a', 2001) }).IsValid);

    // ---- decision handler

    [Theory]
    [InlineData("Approve", VettingApprovalState.Approved, VettingDecision.Approve, AuditAction.Approved)]
    [InlineData("Return", VettingApprovalState.Returned, VettingDecision.Return, AuditAction.Rejected)]
    [InlineData("decline", VettingApprovalState.Declined, VettingDecision.Decline, AuditAction.Rejected)]
    public async Task Decision_MovesToItsState_RecountsWorkload_AndAudits(
        string decision, VettingApprovalState expectedState, VettingDecision expectedDecision, AuditAction expectedAction)
    {
        _submissions.TryRecordDecisionAsync(_id, expectedState, expectedDecision, "  Needs work  ", Arg.Any<CancellationToken>())
            .Returns(false);
        _submissions.TryRecordDecisionAsync(_id, expectedState, expectedDecision, "Needs work", Arg.Any<CancellationToken>())
            .Returns(true);
        var command = new DecideSubmissionCommand { Id = _id, Decision = decision, Reason = "  Needs work  " };

        await Decide().Handle(command, CancellationToken.None);

        await _officers.Received(1).RecalculateWorkloadAsync(_officerId, Arg.Any<CancellationToken>());
        Assert.Equal(expectedAction, command.Action);
        Assert.Equal(_id, command.EntityId);
        Assert.Contains("Needs work", command.NewValues!.ToString());
        Assert.Contains(expectedDecision.ToString(), command.NewValues.ToString());
    }

    [Fact]
    public async Task Decision_WhenNotReady_Throws409_AndDoesNotRecount()
    {
        _submissions.TryRecordDecisionAsync(_id, Arg.Any<VettingApprovalState>(), Arg.Any<VettingDecision>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(false);

        await Assert.ThrowsAsync<ConflictException>(() =>
            Decide().Handle(new DecideSubmissionCommand { Id = _id, Decision = "Approve" }, CancellationToken.None));
        await _officers.DidNotReceive().RecalculateWorkloadAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Decision_UnknownSubmission_Throws404()
        => await Assert.ThrowsAsync<NotFoundException>(() =>
            Decide().Handle(new DecideSubmissionCommand { Id = Guid.NewGuid(), Decision = "Approve" }, CancellationToken.None));

    // ---- reviewer flags

    private VettingSubmission Existing(VettingApprovalState state)
    {
        var s = new VettingSubmission { Id = _id, ApprovalState = state };
        _submissions.GetForUpdateAsync(_id, Arg.Any<CancellationToken>()).Returns(s);
        return s;
    }

    [Fact]
    public async Task ReviewerFlag_WhenReady_IsAddedActive_AsReviewer()
    {
        var s = Existing(VettingApprovalState.Ready);

        var dto = await new AddReviewerFlagCommandHandler(_submissions, _unitOfWork, _mapper)
            .Handle(new AddReviewerFlagCommand { SubmissionId = _id, Description = " Fridge not working " }, CancellationToken.None);

        var flag = Assert.Single(s.Flags);
        Assert.Equal(("Fridge not working", VettingFlagSource.Reviewer, true), (flag.Description, flag.Source, flag.IsActive));
        Assert.Equal("Reviewer", dto.Source);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(VettingApprovalState.New)]
    [InlineData(VettingApprovalState.Returned)]
    [InlineData(VettingApprovalState.Approved)]
    public async Task ReviewerFlag_WhenNotReady_Throws409(VettingApprovalState state)
    {
        var s = Existing(state);
        await Assert.ThrowsAsync<ConflictException>(() => new AddReviewerFlagCommandHandler(_submissions, _unitOfWork, _mapper)
            .Handle(new AddReviewerFlagCommand { SubmissionId = _id, Description = "x" }, CancellationToken.None));
        Assert.Empty(s.Flags);
    }

    [Fact]
    public void ReviewerFlag_DescriptionOver255_Fails()
        => Assert.False(new AddReviewerFlagCommandValidator()
            .Validate(new AddReviewerFlagCommand { SubmissionId = _id, Description = new string('a', 256) }).IsValid);
}
