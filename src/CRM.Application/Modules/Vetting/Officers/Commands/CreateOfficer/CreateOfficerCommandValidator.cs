namespace CRM.Application.Modules.Vetting.Officers.Commands.CreateOfficer;

using FluentValidation;

public class CreateOfficerCommandValidator : AbstractValidator<CreateOfficerCommand>
{
    public CreateOfficerCommandValidator()
    {
        RuleFor(x => x.FullName).OfficerFullName();
        RuleFor(x => x.Initials).OfficerInitials();
        RuleFor(x => x.AssignedRegion).OfficerAssignedRegion();
    }
}
