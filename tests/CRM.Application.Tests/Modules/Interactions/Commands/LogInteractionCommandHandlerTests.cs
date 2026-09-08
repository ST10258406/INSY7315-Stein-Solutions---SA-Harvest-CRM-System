using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Interactions.Commands.LogInteraction;
using CRM.Application.Modules.Interactions.Dtos;
using CRM.Domain.Entities;
using CRM.Domain.Enums;
using NSubstitute;

namespace CRM.Application.Tests.Modules.Interactions.Commands;

public class LogInteractionCommandHandlerTests
{
    private readonly IDonorRepository _donorsMock = Substitute.For<IDonorRepository>();
    private readonly IInteractionLogRepository _interactionsMock = Substitute.For<IInteractionLogRepository>();
    private readonly IUnitOfWork _unitOfWorkMock = Substitute.For<IUnitOfWork>();
    private readonly INotificationService _notificationsMock = Substitute.For<INotificationService>();
    private readonly ICurrentUserService _currentUserMock = Substitute.For<ICurrentUserService>();
    private readonly LogInteractionCommandHandler _handler;

    private readonly Guid _currentUserId = Guid.NewGuid();
    private readonly List<InteractionLog> _added = [];

    public LogInteractionCommandHandlerTests()
    {
        _currentUserMock.GetCurrentUserId().Returns(_currentUserId);

        _interactionsMock
            .AddAsync(Arg.Do<InteractionLog>(l => _added.Add(l)), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        _interactionsMock
            .GetDtoByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ci => new InteractionLogDto
            {
                Id = ci.ArgAt<Guid>(0),
                CreatedBy = new InteractionUserDto { Id = _currentUserId, FullName = "Case Worker" }
            });

        _handler = new LogInteractionCommandHandler(
            _donorsMock, _interactionsMock, _unitOfWorkMock, _notificationsMock, _currentUserMock);
    }

    private Guid SetupExistingDonor(Guid? relationshipManagerId = null, string companyName = "FoodCorp SA")
    {
        var donorId = Guid.NewGuid();
        _donorsMock.ExistsAsync(donorId, Arg.Any<CancellationToken>()).Returns(true);
        _donorsMock.GetForUpdateAsync(donorId, Arg.Any<CancellationToken>()).Returns(new Donor
        {
            Id = donorId,
            CompanyName = companyName,
            RelationshipManagerId = relationshipManagerId
        });
        return donorId;
    }

    private static LogInteractionCommand MakeCommand(Guid donorId, DateTime? followUp = null) => new()
    {
        DonorId = donorId,
        InteractionType = "Call",
        Subject = "Intro call",
        Body = "Spoke to the procurement lead.",
        FollowUpDate = followUp
    };

    [Fact]
    public async Task Handle_NoFollowUp_AppendsLogSavesOnceAndDoesNotNotify()
    {
        var donorId = SetupExistingDonor();

        await _handler.Handle(MakeCommand(donorId), CancellationToken.None);

        var log = Assert.Single(_added);
        Assert.Equal(donorId, log.DonorId);
        Assert.Equal(_currentUserId, log.CreatedByUserId);
        Assert.Equal(InteractionType.Call, log.InteractionType);
        Assert.Equal("Intro call", log.Subject);
        Assert.Equal("Spoke to the procurement lead.", log.Body);
        Assert.NotEqual(default, log.CreatedAt);

        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _donorsMock.DidNotReceive().GetForUpdateAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _notificationsMock.DidNotReceive().CreateAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<NotificationType>(),
            Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithFollowUp_MovesDonorFollowUpDateAndNotifiesRelationshipManager()
    {
        var managerId = Guid.NewGuid();
        var donorId = SetupExistingDonor(managerId, "FoodCorp SA");
        var followUp = DateTime.UtcNow.AddDays(5);

        Donor? capturedDonor = null;
        _donorsMock.GetForUpdateAsync(donorId, Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                capturedDonor = new Donor { Id = donorId, CompanyName = "FoodCorp SA", RelationshipManagerId = managerId };
                return capturedDonor;
            });

        await _handler.Handle(MakeCommand(donorId, followUp), CancellationToken.None);

        Assert.Equal(followUp, capturedDonor!.FollowUpDate);
        await _notificationsMock.Received(1).CreateAsync(
            managerId,
            Arg.Any<string>(),
            Arg.Is<string>(m => m.Contains("FoodCorp SA")),
            NotificationType.FollowUpReminder,
            donorId,
            "Donor",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithFollowUp_NoRelationshipManager_MovesDateButDoesNotNotify()
    {
        var donorId = SetupExistingDonor(relationshipManagerId: null);

        await _handler.Handle(MakeCommand(donorId, DateTime.UtcNow.AddDays(2)), CancellationToken.None);

        await _donorsMock.Received(1).GetForUpdateAsync(donorId, Arg.Any<CancellationToken>());
        await _notificationsMock.DidNotReceive().CreateAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<NotificationType>(),
            Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SetsAuditEntityIdToTheNewLogId()
    {
        var donorId = SetupExistingDonor();
        var command = MakeCommand(donorId);

        await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(_added[0].Id, command.EntityId);
        Assert.NotNull(command.NewValues);
    }

    [Fact]
    public async Task Handle_UnknownDonor_ThrowsNotFoundAndAppendsNothing()
    {
        _donorsMock.ExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _handler.Handle(MakeCommand(Guid.NewGuid()), CancellationToken.None));

        Assert.Empty(_added);
        await _unitOfWorkMock.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReturnsDtoReReadThroughTheRepositoryProjection()
    {
        var donorId = SetupExistingDonor();

        var result = await _handler.Handle(MakeCommand(donorId), CancellationToken.None);

        Assert.Equal(_added[0].Id, result.Id);
        Assert.Equal("Case Worker", result.CreatedBy.FullName);
        await _interactionsMock.Received(1).GetDtoByIdAsync(_added[0].Id, Arg.Any<CancellationToken>());
    }
}
