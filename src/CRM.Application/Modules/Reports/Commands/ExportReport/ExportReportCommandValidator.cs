namespace CRM.Application.Modules.Reports.Commands.ExportReport;

using CRM.Application.Modules.Reports.Export;
using FluentValidation;

public class ExportReportCommandValidator : AbstractValidator<ExportReportCommand>
{
    public ExportReportCommandValidator()
    {
        RuleFor(x => x.ReportType)
            .Must(type => type is not null && ReportTypes.All.Contains(type))
            .WithMessage($"reportType must be one of: {string.Join(", ", ReportTypes.All)}.");

        RuleFor(x => x.Format)
            .Must(format => format is not null && ReportExportFormats.All.Contains(format))
            .WithMessage($"format must be one of: {string.Join(", ", ReportExportFormats.All)}.");

        // Filters only apply to donors-contacted. For the snapshot reports they're ignored
        // entirely, so a client sending stale or even invalid filters there doesn't get a 400.
        When(x => x.ReportType == ReportTypes.DonorsContacted, () =>
        {
            RuleFor(x => x.Filters)
                .NotNull()
                .WithMessage("filters.startDate and filters.endDate are required for the donors-contacted report.");

            When(x => x.Filters is not null, () =>
            {
                RuleFor(x => x.Filters!.StartDate)
                    .NotNull()
                    .WithMessage("filters.startDate is required for the donors-contacted report.");

                RuleFor(x => x.Filters!.EndDate)
                    .NotNull()
                    .WithMessage("filters.endDate is required for the donors-contacted report.");

                RuleFor(x => x.Filters!)
                    .Must(f => f.StartDate!.Value <= f.EndDate!.Value)
                    .WithMessage("filters.startDate must not be after filters.endDate.")
                    .When(x => x.Filters!.StartDate.HasValue && x.Filters.EndDate.HasValue);
            });
        });
    }
}
