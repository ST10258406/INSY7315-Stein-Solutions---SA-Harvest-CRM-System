namespace CRM.Application.Modules.Vetting.SiteVisits.Commands.CreateSiteVisit;

using FluentValidation;

public class CreateSiteVisitCommandValidator : AbstractValidator<CreateSiteVisitCommand>
{
    public CreateSiteVisitCommandValidator()
    {
        RuleFor(x => x.DonorId).NotEmpty();
        RuleFor(x => x.OfficerId).NotEmpty();
        RuleFor(x => x.ScheduledDate).NotNull();
        RuleFor(x => x.ScheduledTime).NotNull();
        RuleFor(x => x.LocationLabel).NotEmpty().MaximumLength(100).When(x => x.LocationLabel is not null);
        RuleFor(x => x.VisitNotes).MaximumLength(255);
    }
}
