namespace CRM.Application.Modules.Reports.Queries.GetDonorsContactedReport;

using FluentValidation;

public class GetDonorsContactedReportQueryValidator : AbstractValidator<GetDonorsContactedReportQuery>
{
    public GetDonorsContactedReportQueryValidator()
    {
        RuleFor(x => x.StartDate).NotNull().WithMessage("startDate is required.");
        RuleFor(x => x.EndDate).NotNull().WithMessage("endDate is required.");

        RuleFor(x => x)
            .Must(x => x.StartDate!.Value <= x.EndDate!.Value)
            .WithMessage("startDate must not be after endDate.")
            .When(x => x.StartDate.HasValue && x.EndDate.HasValue);
    }
}
