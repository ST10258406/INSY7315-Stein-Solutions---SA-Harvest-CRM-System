using System.Text.Json;
using CRM.Application.Common.Interfaces;
using CRM.Application.Common.Models;
using CRM.Application.Modules.Reports.Commands.ExportReport;
using CRM.Application.Modules.Reports.Dtos;
using CRM.Application.Modules.Reports.Export;
using CRM.Application.Modules.Reports.Queries.GetDonorsByRegionReport;
using CRM.Application.Modules.Reports.Queries.GetDonorsByStatusReport;
using CRM.Application.Modules.Reports.Queries.GetDonorsByTypeReport;
using CRM.Application.Modules.Reports.Queries.GetDonorsContactedReport;
using CRM.Domain.Enums;
using MediatR;
using NSubstitute;

namespace CRM.Application.Tests.Modules.Reports.Commands;

public class ExportReportCommandHandlerTests
{
    private static readonly byte[] RenderedBytes = [0x25, 0x50, 0x44, 0x46, 0x2D];

    private readonly ISender _senderMock = Substitute.For<ISender>();
    private readonly IReportExportService _reportExportMock = Substitute.For<IReportExportService>();
    private readonly IBlobStorageService _blobStorageMock = Substitute.For<IBlobStorageService>();
    private readonly ExportReportCommandHandler _handler;

    public ExportReportCommandHandlerTests()
    {
        _senderMock.Send(Arg.Any<GetDonorsContactedReportQuery>(), Arg.Any<CancellationToken>())
            .Returns(new DonorsContactedReportDto
            {
                Period = new ReportPeriodDto { StartDate = new DateOnly(2026, 7, 1), EndDate = new DateOnly(2026, 7, 31) },
                TotalDonorsContacted = 3,
                ByManager = [new ManagerContactedDto { Manager = new ReportManagerDto { Id = Guid.NewGuid(), FullName = "Jane Doe" }, DonorsContacted = 3, TotalInteractions = 5 }]
            });
        _senderMock.Send(Arg.Any<GetDonorsByRegionReportQuery>(), Arg.Any<CancellationToken>())
            .Returns([new DonorsByRegionDto { Region = "JHB", RegionName = "Johannesburg", DonorCount = 2 }]);
        _senderMock.Send(Arg.Any<GetDonorsByTypeReportQuery>(), Arg.Any<CancellationToken>())
            .Returns([new DonorsByTypeDto { DonationType = "Food", DonorCount = 4 }]);
        _senderMock.Send(Arg.Any<GetDonorsByStatusReportQuery>(), Arg.Any<CancellationToken>())
            .Returns([new DonorsByStatusDto { Status = "Active", DonorCount = 7 }]);

        _reportExportMock
            .GenerateAsync(Arg.Any<ReportDocument>(), Arg.Any<ReportExportFormat>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.ArgAt<ReportExportFormat>(1) == ReportExportFormat.Pdf
                ? new ReportFile(RenderedBytes, "application/pdf", "pdf")
                : new ReportFile(RenderedBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "xlsx"));

        _blobStorageMock
            .UploadAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(ci => new BlobUploadResult(ci.ArgAt<string>(1), "https://raw.blob/" + ci.ArgAt<string>(1)));
        _blobStorageMock
            .GenerateSasUrlAsync(Arg.Any<string>(), Arg.Any<TimeSpan>())
            .Returns(ci => "https://storage.blob.core.windows.net/" + ci.ArgAt<string>(0) + "?sig=sas");

        _handler = new ExportReportCommandHandler(_senderMock, _reportExportMock, _blobStorageMock);
    }

    private static ExportReportCommand DonorsContacted(string format = "pdf", Guid? managerId = null) => new()
    {
        ReportType = "donors-contacted",
        Format = format,
        Filters = new ExportReportFilters
        {
            StartDate = new DateOnly(2026, 7, 1),
            EndDate = new DateOnly(2026, 7, 31),
            RelationshipManagerId = managerId
        }
    };

    [Fact]
    public void Command_ImplementsIAuditableCommand_WithExportedAction()
    {
        IAuditableCommand command = new ExportReportCommand();

        Assert.Equal(AuditAction.Exported, command.Action);
        Assert.Equal("Report", command.EntityType);
    }

    [Fact]
    public async Task Handle_DonorsContacted_DispatchesTheGetEndpointQueryWithTheRequestFilters()
    {
        var managerId = Guid.NewGuid();

        await _handler.Handle(DonorsContacted(managerId: managerId), CancellationToken.None);

        await _senderMock.Received(1).Send(
            Arg.Is<GetDonorsContactedReportQuery>(q =>
                q.StartDate == new DateOnly(2026, 7, 1) &&
                q.EndDate == new DateOnly(2026, 7, 31) &&
                q.RelationshipManagerId == managerId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DonorsByRegion_DispatchesRegionQueryOnly()
    {
        await _handler.Handle(new ExportReportCommand { ReportType = "donors-by-region", Format = "pdf" }, CancellationToken.None);

        await _senderMock.Received(1).Send(Arg.Any<GetDonorsByRegionReportQuery>(), Arg.Any<CancellationToken>());
        await _senderMock.DidNotReceive().Send(Arg.Any<GetDonorsContactedReportQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DonorsByType_DispatchesTypeQuery()
    {
        await _handler.Handle(new ExportReportCommand { ReportType = "donors-by-type", Format = "pdf" }, CancellationToken.None);

        await _senderMock.Received(1).Send(Arg.Any<GetDonorsByTypeReportQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DonorsByStatus_DispatchesStatusQuery()
    {
        await _handler.Handle(new ExportReportCommand { ReportType = "donors-by-status", Format = "pdf" }, CancellationToken.None);

        await _senderMock.Received(1).Send(Arg.Any<GetDonorsByStatusReportQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DonorsContacted_RendersTheQueryResult()
    {
        await _handler.Handle(DonorsContacted(), CancellationToken.None);

        await _reportExportMock.Received(1).GenerateAsync(
            Arg.Is<ReportDocument>(d =>
                d.Title == "Donors Contacted" &&
                d.Rows.Count == 1 &&
                (string)d.Rows[0][0] == "Jane Doe"),
            ReportExportFormat.Pdf,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExcelFormat_RendersExcelAndNamesFileXlsx()
    {
        var result = await _handler.Handle(DonorsContacted(format: "excel"), CancellationToken.None);

        await _reportExportMock.Received(1).GenerateAsync(
            Arg.Any<ReportDocument>(), ReportExportFormat.Excel, Arg.Any<CancellationToken>());
        Assert.Equal("donors-contacted-july-2026.xlsx", result.FileName);
    }

    [Fact]
    public async Task Handle_DonorsContacted_NamesFileAfterThePeriod()
    {
        var result = await _handler.Handle(DonorsContacted(), CancellationToken.None);

        Assert.Equal("donors-contacted-july-2026.pdf", result.FileName);
    }

    [Fact]
    public async Task Handle_SnapshotReportWithFilters_IgnoresFiltersAndNamesFileAfterExportDate()
    {
        var command = new ExportReportCommand
        {
            ReportType = "donors-by-region",
            Format = "excel",
            Filters = new ExportReportFilters { StartDate = new DateOnly(2026, 7, 1), EndDate = new DateOnly(2026, 7, 31) }
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        var today = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
        Assert.Equal($"donors-by-region-{today}.xlsx", result.FileName);
    }

    [Fact]
    public async Task Handle_UploadsRenderedBytesUnderReportsPrefixWithRenderedContentType()
    {
        byte[]? uploaded = null;
        string? uploadedPath = null;
        _blobStorageMock
            .UploadAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(ci => new BlobUploadResult(ci.ArgAt<string>(1), "https://raw.blob/x"))
            .AndDoes(ci =>
            {
                using var copy = new MemoryStream();
                ci.ArgAt<Stream>(0).CopyTo(copy);
                uploaded = copy.ToArray();
                uploadedPath = ci.ArgAt<string>(1);
            });

        var command = DonorsContacted();
        var result = await _handler.Handle(command, CancellationToken.None);

        await _blobStorageMock.Received(1).UploadAsync(Arg.Any<Stream>(), Arg.Any<string>(), "application/pdf");
        Assert.Equal(RenderedBytes, uploaded);
        Assert.Equal($"reports/donors-contacted/{command.EntityId}/donors-contacted-july-2026.pdf", uploadedPath);
        Assert.Equal(RenderedBytes.LongLength, result.FileSizeBytes);
    }

    [Fact]
    public async Task Handle_ReturnsSasUrlForTheUploadedBlobWithFifteenMinuteExpiry()
    {
        var before = DateTime.UtcNow;

        var command = DonorsContacted();
        var result = await _handler.Handle(command, CancellationToken.None);

        var expectedPath = $"reports/donors-contacted/{command.EntityId}/donors-contacted-july-2026.pdf";
        await _blobStorageMock.Received(1).GenerateSasUrlAsync(expectedPath, TimeSpan.FromMinutes(15));
        Assert.Equal($"https://storage.blob.core.windows.net/{expectedPath}?sig=sas", result.DownloadUrl);
        Assert.InRange(result.ExpiresAt, before.AddMinutes(15), DateTime.UtcNow.AddMinutes(15));
    }

    [Fact]
    public async Task Handle_EachExport_GetsItsOwnBlobFolder()
    {
        var first = DonorsContacted();
        var second = DonorsContacted();

        await _handler.Handle(first, CancellationToken.None);
        await _handler.Handle(second, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, first.EntityId);
        Assert.NotEqual(first.EntityId, second.EntityId);
    }

    [Fact]
    public async Task Handle_PopulatesAuditNewValuesWithExportDetails()
    {
        var command = DonorsContacted();

        await _handler.Handle(command, CancellationToken.None);

        Assert.Null(command.OldValues);
        var newValues = JsonDocument.Parse(JsonSerializer.Serialize(command.NewValues)).RootElement;
        Assert.Equal("donors-contacted", newValues.GetProperty("ReportType").GetString());
        Assert.Equal("pdf", newValues.GetProperty("Format").GetString());
        Assert.Equal("donors-contacted-july-2026.pdf", newValues.GetProperty("FileName").GetString());
        Assert.Equal("2026-07-01", newValues.GetProperty("Filters").GetProperty("StartDate").GetString());
        Assert.Equal(
            $"reports/donors-contacted/{command.EntityId}/donors-contacted-july-2026.pdf",
            newValues.GetProperty("BlobPath").GetString());
    }

    [Fact]
    public async Task Handle_SnapshotReport_DoesNotRecordIgnoredFiltersInAudit()
    {
        var command = new ExportReportCommand
        {
            ReportType = "donors-by-status",
            Format = "pdf",
            Filters = new ExportReportFilters { StartDate = new DateOnly(2026, 7, 1), EndDate = new DateOnly(2026, 7, 31) }
        };

        await _handler.Handle(command, CancellationToken.None);

        var newValues = JsonDocument.Parse(JsonSerializer.Serialize(command.NewValues)).RootElement;
        Assert.Equal(JsonValueKind.Null, newValues.GetProperty("Filters").ValueKind);
    }
}
