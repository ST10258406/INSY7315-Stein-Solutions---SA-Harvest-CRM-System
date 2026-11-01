namespace CRM.Application.Modules.Vetting.Officers.Commands.UpdateOfficer;

using FluentValidation;

public class UpdateOfficerCommandValidator : AbstractValidator<UpdateOfficerCommand>
{
    public UpdateOfficerCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.FullName).OfficerFullName().When(x => x.FullName is not null);
        RuleFor(x => x.Initials).OfficerInitials().When(x => x.Initials is not null);
        RuleFor(x => x.AssignedRegion).OfficerAssignedRegion().When(x => x.AssignedRegion is not null);
    }
}
