namespace CRM.Application.Modules.Vetting.SiteVisits.Queries.GetSiteVisits;

using FluentValidation;

public class GetSiteVisitsQueryValidator : AbstractValidator<GetSiteVisitsQuery>
{
    public GetSiteVisitsQueryValidator()
    {
        RuleFor(x => x.Date).NotNull();
    }
}
