namespace CRM.Application.Modules.Vetting.SiteVisits.Commands.UpdateSiteVisit;

using CRM.Domain.Enums;
using FluentValidation;

public class UpdateSiteVisitCommandValidator : AbstractValidator<UpdateSiteVisitCommand>
{
    public UpdateSiteVisitCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.LocationLabel).NotEmpty().MaximumLength(100).When(x => x.LocationLabel is not null);
        RuleFor(x => x.VisitNotes).MaximumLength(255);

        RuleFor(x => x.VisitStatus)
            // Names only: Enum.TryParse would also accept "7".
            .Must(s => Enum.GetNames<SiteVisitStatus>().Contains(s, StringComparer.OrdinalIgnoreCase))
            .When(x => x.VisitStatus is not null)
            .WithMessage("visitStatus must be one of: ToDo, InProgress, Completed.");

        RuleFor(x => x.OfficerId)
            .Must(v => v!.Value != Guid.Empty)
            .When(x => x.OfficerId.HasValue)
            .WithMessage("officerId must be a non-empty id.");
    }
}
