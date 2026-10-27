using System.Text.Json;
using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Vetting.Officers.Commands.CreateOfficer;
using CRM.Application.Modules.Vetting.Officers.Commands.UpdateOfficer;
using CRM.Application.Modules.Vetting.Officers.Dtos;
using CRM.Domain.Entities.Vetting;
using NSubstitute;

namespace CRM.Application.Tests.Modules.Vetting;

public class OfficerCommandHandlerTests
{
    private readonly IVettingOfficerRepository _officers = Substitute.For<IVettingOfficerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly List<VettingOfficer> _added = [];

    public OfficerCommandHandlerTests()
    {
        _officers.AddAsync(Arg.Do<VettingOfficer>(_added.Add), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _officers.GetDtoByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ci => new VettingOfficerDto { Id = ci.ArgAt<Guid>(0) });
    }

    [Fact]
    public async Task Create_StagesOfficerWithZeroWorkload_SavesAndRecordsAudit()
    {
        var command = new CreateOfficerCommand { FullName = "Thandi Mokoena", Initials = "TM", AssignedRegion = "Gauteng North" };

        var dto = await new CreateOfficerCommandHandler(_officers, _unitOfWork).Handle(command, CancellationToken.None);

        var officer = Assert.Single(_added);
        Assert.Equal(("Thandi Mokoena", "TM", "Gauteng North", 0), (officer.FullName, officer.Initials, officer.AssignedRegion, officer.WorkloadCount));
        Assert.Equal(officer.Id, dto.Id);
        Assert.Equal(officer.Id, command.EntityId);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_ChangesOnlyTheGivenFields_AndRecordsOldAndNewValues()
    {
        var officer = new VettingOfficer { Id = Guid.NewGuid(), FullName = "Thandi Mokoena", Initials = "TM", AssignedRegion = "Gauteng North" };
        _officers.GetForUpdateAsync(officer.Id, Arg.Any<CancellationToken>()).Returns(officer);
        var command = new UpdateOfficerCommand { Id = officer.Id, AssignedRegion = "Limpopo" };

        await new UpdateOfficerCommandHandler(_officers, _unitOfWork).Handle(command, CancellationToken.None);

        Assert.Equal(("Thandi Mokoena", "TM", "Limpopo"), (officer.FullName, officer.Initials, officer.AssignedRegion));
        // Anonymous types never compare equal across assemblies, so compare the audit JSON.
        Assert.Equal("""{"FullName":"Thandi Mokoena","Initials":"TM","AssignedRegion":"Gauteng North"}""", JsonSerializer.Serialize(command.OldValues));
        Assert.Equal("""{"FullName":"Thandi Mokoena","Initials":"TM","AssignedRegion":"Limpopo"}""", JsonSerializer.Serialize(command.NewValues));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_UnknownOfficer_ThrowsNotFound_AndSavesNothing()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            new UpdateOfficerCommandHandler(_officers, _unitOfWork).Handle(new UpdateOfficerCommand { Id = Guid.NewGuid() }, CancellationToken.None));

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
