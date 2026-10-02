namespace CRM.Application.Modules.Reports.Commands.ExportReport;

using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Reports.Dtos;
using CRM.Application.Modules.Reports.Export;
using CRM.Application.Modules.Reports.Queries.GetDonorsByRegionReport;
using CRM.Application.Modules.Reports.Queries.GetDonorsByStatusReport;
using CRM.Application.Modules.Reports.Queries.GetDonorsByTypeReport;
using CRM.Application.Modules.Reports.Queries.GetDonorsContactedReport;
using MediatR;

public class ExportReportCommandHandler : IRequestHandler<ExportReportCommand, ReportExportDto>
{
    // Same hard ceiling as document downloads — never source this from config.
    private static readonly TimeSpan SasExpiry = TimeSpan.FromMinutes(15);

    private readonly ISender _sender;
    private readonly IReportExportService _reportExport;
    private readonly IBlobStorageService _blobStorage;

    public ExportReportCommandHandler(
        ISender sender,
        IReportExportService reportExport,
        IBlobStorageService blobStorage)
    {
        _sender = sender;
        _reportExport = reportExport;
        _blobStorage = blobStorage;
    }

    public async Task<ReportExportDto> Handle(ExportReportCommand request, CancellationToken cancellationToken)
    {
        // Validator has already guaranteed ReportType/Format are known values and, for
        // donors-contacted, that both dates are present with StartDate <= EndDate.
        var reportType = request.ReportType!;
        var isDonorsContacted = reportType == ReportTypes.DonorsContacted;
        var generatedAtUtc = DateTime.UtcNow;

        var document = await BuildDocumentAsync(request, generatedAtUtc, cancellationToken);
        var format = request.Format == ReportExportFormats.Pdf ? ReportExportFormat.Pdf : ReportExportFormat.Excel;
        var file = await _reportExport.GenerateAsync(document, format, cancellationToken);

        var fileName = ReportExportFileName.Build(
            reportType,
            isDonorsContacted ? request.Filters!.StartDate : null,
            isDonorsContacted ? request.Filters!.EndDate : null,
            DateOnly.FromDateTime(generatedAtUtc),
            file.FileExtension);

        // Every export gets its own folder under the reports/ prefix: repeat exports of the
        // same report/period never overwrite each other, and a storage lifecycle rule can
        // target the whole prefix without touching donor documents in the same container.
        var exportId = Guid.NewGuid();
        var blobPath = $"reports/{reportType}/{exportId}/{fileName}";

        using (var stream = new MemoryStream(file.Content, writable: false))
            await _blobStorage.UploadAsync(stream, blobPath, file.ContentType);

        var downloadUrl = await _blobStorage.GenerateSasUrlAsync(blobPath, SasExpiry);
        var expiresAt = DateTime.UtcNow.Add(SasExpiry);

        request.EntityId = exportId;
        request.OldValues = null;
        request.NewValues = new
        {
            ReportType = reportType,
            request.Format,
            Filters = isDonorsContacted ? request.Filters : null,
            FileName = fileName,
            FileSizeBytes = file.Content.LongLength,
            BlobPath = blobPath
        };

        return new ReportExportDto
        {
            DownloadUrl = downloadUrl,
            ExpiresAt = expiresAt,
            FileName = fileName,
            FileSizeBytes = file.Content.LongLength
        };
    }

    // Dispatches the exact same query the matching GET endpoint sends, so the export and the
    // on-screen report share one aggregation path (and the same validation) — no second copy
    // of the reporting SQL.
    private async Task<ReportDocument> BuildDocumentAsync(
        ExportReportCommand request, DateTime generatedAtUtc, CancellationToken cancellationToken)
    {
        switch (request.ReportType)
        {
            case ReportTypes.DonorsContacted:
                var filters = request.Filters!;
                var contacted = await _sender.Send(new GetDonorsContactedReportQuery
                {
                    StartDate = filters.StartDate,
                    EndDate = filters.EndDate,
                    RelationshipManagerId = filters.RelationshipManagerId
                }, cancellationToken);
                return ReportDocumentFactory.DonorsContacted(contacted, filters.RelationshipManagerId, generatedAtUtc);

            case ReportTypes.DonorsByRegion:
                var byRegion = await _sender.Send(new GetDonorsByRegionReportQuery(), cancellationToken);
                return ReportDocumentFactory.DonorsByRegion(byRegion, generatedAtUtc);

            case ReportTypes.DonorsByType:
                var byType = await _sender.Send(new GetDonorsByTypeReportQuery(), cancellationToken);
                return ReportDocumentFactory.DonorsByType(byType, generatedAtUtc);

            case ReportTypes.DonorsByStatus:
                var byStatus = await _sender.Send(new GetDonorsByStatusReportQuery(), cancellationToken);
                return ReportDocumentFactory.DonorsByStatus(byStatus, generatedAtUtc);

            default:
                throw new InvalidOperationException($"Unsupported report type '{request.ReportType}'.");
        }
    }
}
