using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Vetting.SiteVisits.Commands.CreateSiteVisit;
using CRM.Application.Modules.Vetting.SiteVisits.Commands.UpdateSiteVisit;
using CRM.Application.Modules.Vetting.SiteVisits.Dtos;
using CRM.Domain.Entities.Vetting;
using CRM.Domain.Enums;
using NSubstitute;

namespace CRM.Application.Tests.Modules.Vetting;

public class SiteVisitCommandHandlerTests
{
    private readonly IDonorRepository _donors = Substitute.For<IDonorRepository>();
    private readonly IVettingOfficerRepository _officers = Substitute.For<IVettingOfficerRepository>();
    private readonly IVettingSiteVisitRepository _visits = Substitute.For<IVettingSiteVisitRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly List<VettingSiteVisit> _added = [];

    private readonly Guid _donorId = Guid.NewGuid();
    private readonly Guid _officerId = Guid.NewGuid();

    public SiteVisitCommandHandlerTests()
    {
        // The transaction boundary is Infrastructure's job; here it just runs the work.
        _unitOfWork.ExecuteInTransactionAsync(Arg.Any<Func<Task<bool>>>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Func<Task<bool>>>()());
        _visits.AddAsync(Arg.Do<VettingSiteVisit>(_added.Add), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _visits.GetDtoByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ci => new VettingSiteVisitDto { Id = ci.ArgAt<Guid>(0) });
        _donors.ExistsAsync(_donorId, Arg.Any<CancellationToken>()).Returns(true);
        _officers.ExistsAsync(_officerId, Arg.Any<CancellationToken>()).Returns(true);
    }

    private CreateSiteVisitCommandHandler CreateHandler() => new(_donors, _officers, _visits, _unitOfWork);
    private UpdateSiteVisitCommandHandler UpdateHandler() => new(_officers, _visits, _unitOfWork);

    private CreateSiteVisitCommand NewCommand(string? label = null) => new()
    {
        DonorId = _donorId,
        OfficerId = _officerId,
        ScheduledDate = new DateOnly(2026, 10, 21),
        ScheduledTime = new TimeOnly(9, 30),
        LocationLabel = label
    };

    [Fact]
    public async Task Create_NoLabel_DefaultsToDonorSuburb_StartsToDo_AndRecountsWorkload()
    {
        _donors.GetLegalAddressSuburbAsync(_donorId, Arg.Any<CancellationToken>()).Returns("Midrand");

        var command = NewCommand();
        await CreateHandler().Handle(command, CancellationToken.None);

        var visit = Assert.Single(_added);
        Assert.Equal("Midrand", visit.LocationLabel);
        Assert.Equal(SiteVisitStatus.ToDo, visit.VisitStatus);
        Assert.Equal(visit.Id, command.EntityId);
        Received.InOrder(() =>
        {
            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>());
            _officers.RecalculateWorkloadAsync(_officerId, Arg.Any<CancellationToken>());
            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task Create_NoLabel_DonorWithoutLegalAddress_ThrowsValidation()
    {
        await Assert.ThrowsAsync<ValidationException>(() => CreateHandler().Handle(NewCommand(), CancellationToken.None));
        Assert.Empty(_added);
    }

    [Fact]
    public async Task Create_UnknownDonorOrOfficer_ThrowsNotFound()
    {
        var unknownDonor = NewCommand("Soweto");
        unknownDonor.DonorId = Guid.NewGuid();
        var unknownOfficer = NewCommand("Soweto");
        unknownOfficer.OfficerId = Guid.NewGuid();

        await Assert.ThrowsAsync<NotFoundException>(() => CreateHandler().Handle(unknownDonor, CancellationToken.None));
        await Assert.ThrowsAsync<NotFoundException>(() => CreateHandler().Handle(unknownOfficer, CancellationToken.None));
        Assert.Empty(_added);
    }

    [Fact]
    public async Task Update_Reassignment_RecountsBothOfficers()
    {
        var newOfficerId = Guid.NewGuid();
        _officers.ExistsAsync(newOfficerId, Arg.Any<CancellationToken>()).Returns(true);
        var visit = new VettingSiteVisit { Id = Guid.NewGuid(), DonorId = _donorId, OfficerId = _officerId, LocationLabel = "Soweto" };
        _visits.GetForUpdateAsync(visit.Id, Arg.Any<CancellationToken>()).Returns(visit);

        await UpdateHandler().Handle(
            new UpdateSiteVisitCommand { Id = visit.Id, OfficerId = newOfficerId, VisitStatus = "inprogress" }, CancellationToken.None);

        Assert.Equal(newOfficerId, visit.OfficerId);
        Assert.Equal(SiteVisitStatus.InProgress, visit.VisitStatus);
        await _officers.Received(1).RecalculateWorkloadAsync(_officerId, Arg.Any<CancellationToken>());
        await _officers.Received(1).RecalculateWorkloadAsync(newOfficerId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_UnknownNewOfficer_ThrowsNotFound_WithoutSaving()
    {
        var visit = new VettingSiteVisit { Id = Guid.NewGuid(), OfficerId = _officerId };
        _visits.GetForUpdateAsync(visit.Id, Arg.Any<CancellationToken>()).Returns(visit);

        await Assert.ThrowsAsync<NotFoundException>(() => UpdateHandler().Handle(
            new UpdateSiteVisitCommand { Id = visit.Id, OfficerId = Guid.NewGuid() }, CancellationToken.None));

        Assert.Equal(_officerId, visit.OfficerId);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
