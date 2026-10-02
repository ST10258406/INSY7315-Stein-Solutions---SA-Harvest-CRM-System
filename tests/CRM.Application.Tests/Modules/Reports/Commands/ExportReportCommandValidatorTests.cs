using CRM.Application.Modules.Reports.Commands.ExportReport;
using CRM.Application.Modules.Reports.Dtos;

namespace CRM.Application.Tests.Modules.Reports.Commands;

public class ExportReportCommandValidatorTests
{
    private readonly ExportReportCommandValidator _validator = new();

    private static ExportReportCommand ValidDonorsContacted() => new()
    {
        ReportType = "donors-contacted",
        Format = "pdf",
        Filters = new ExportReportFilters
        {
            StartDate = new DateOnly(2026, 7, 1),
            EndDate = new DateOnly(2026, 7, 31)
        }
    };

    [Fact]
    public void Validate_ValidDonorsContactedRequest_Passes()
    {
        Assert.True(_validator.Validate(ValidDonorsContacted()).IsValid);
    }

    [Theory]
    [InlineData("donors-contacted")]
    [InlineData("donors-by-region")]
    [InlineData("donors-by-type")]
    [InlineData("donors-by-status")]
    public void Validate_EachSupportedReportType_Passes(string reportType)
    {
        var command = ValidDonorsContacted();
        command.ReportType = reportType;

        Assert.True(_validator.Validate(command).IsValid);
    }

    [Theory]
    [InlineData("donors")]
    [InlineData("DONORS-CONTACTED")]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_InvalidReportType_Fails(string? reportType)
    {
        var command = ValidDonorsContacted();
        command.ReportType = reportType;

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ExportReportCommand.ReportType));
    }

    [Theory]
    [InlineData("excel")]
    [InlineData("pdf")]
    public void Validate_SupportedFormat_Passes(string format)
    {
        var command = ValidDonorsContacted();
        command.Format = format;

        Assert.True(_validator.Validate(command).IsValid);
    }

    [Theory]
    [InlineData("csv")]
    [InlineData("xlsx")]
    [InlineData("PDF")]
    [InlineData(null)]
    public void Validate_InvalidFormat_Fails(string? format)
    {
        var command = ValidDonorsContacted();
        command.Format = format;

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ExportReportCommand.Format));
    }

    [Fact]
    public void Validate_DonorsContactedWithoutFilters_Fails()
    {
        var command = ValidDonorsContacted();
        command.Filters = null;

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ExportReportCommand.Filters));
    }

    [Fact]
    public void Validate_DonorsContactedMissingStartDate_Fails()
    {
        var command = ValidDonorsContacted();
        command.Filters!.StartDate = null;

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Filters.StartDate");
    }

    [Fact]
    public void Validate_DonorsContactedMissingEndDate_Fails()
    {
        var command = ValidDonorsContacted();
        command.Filters!.EndDate = null;

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Filters.EndDate");
    }

    [Fact]
    public void Validate_DonorsContactedStartDateAfterEndDate_Fails()
    {
        var command = ValidDonorsContacted();
        command.Filters!.StartDate = new DateOnly(2026, 8, 1);
        command.Filters.EndDate = new DateOnly(2026, 7, 31);

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("must not be after"));
    }

    [Fact]
    public void Validate_DonorsContactedSingleDayRange_Passes()
    {
        var command = ValidDonorsContacted();
        command.Filters!.StartDate = new DateOnly(2026, 7, 15);
        command.Filters.EndDate = new DateOnly(2026, 7, 15);

        Assert.True(_validator.Validate(command).IsValid);
    }

    [Theory]
    [InlineData("donors-by-region")]
    [InlineData("donors-by-type")]
    [InlineData("donors-by-status")]
    public void Validate_SnapshotReportWithInvalidFilters_IgnoresFiltersAndPasses(string reportType)
    {
        var command = new ExportReportCommand
        {
            ReportType = reportType,
            Format = "excel",
            Filters = new ExportReportFilters
            {
                StartDate = new DateOnly(2026, 8, 1),
                EndDate = new DateOnly(2026, 7, 1)
            }
        };

        Assert.True(_validator.Validate(command).IsValid);
    }

    [Theory]
    [InlineData("donors-by-region")]
    [InlineData("donors-by-type")]
    [InlineData("donors-by-status")]
    public void Validate_SnapshotReportWithoutFilters_Passes(string reportType)
    {
        var command = new ExportReportCommand { ReportType = reportType, Format = "pdf" };

        Assert.True(_validator.Validate(command).IsValid);
    }
}
