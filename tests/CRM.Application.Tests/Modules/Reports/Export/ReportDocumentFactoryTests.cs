using CRM.Application.Modules.Reports.Dtos;
using CRM.Application.Modules.Reports.Export;

namespace CRM.Application.Tests.Modules.Reports.Export;

public class ReportDocumentFactoryTests
{
    private static readonly DateTime GeneratedAt = new(2026, 9, 8, 10, 30, 0, DateTimeKind.Utc);

    private static DonorsContactedReportDto ContactedReport(Guid managerId) => new()
    {
        Period = new ReportPeriodDto { StartDate = new DateOnly(2026, 7, 1), EndDate = new DateOnly(2026, 7, 31) },
        TotalDonorsContacted = 4,
        ByManager =
        [
            new ManagerContactedDto { Manager = new ReportManagerDto { Id = managerId, FullName = "Jane Doe" }, DonorsContacted = 3, TotalInteractions = 5 }
        ]
    };

    [Fact]
    public void DonorsContacted_MapsManagersToRowsAndSummarisesPeriodAndTotal()
    {
        var document = ReportDocumentFactory.DonorsContacted(ContactedReport(Guid.NewGuid()), null, GeneratedAt);

        Assert.Equal("Donors Contacted", document.Title);
        Assert.Equal(GeneratedAt, document.GeneratedAtUtc);
        Assert.Equal(["Relationship manager", "Donors contacted", "Total interactions"], document.Columns.Select(c => c.Header));
        Assert.Equal(new object[] { "Jane Doe", 3, 5 }, document.Rows.Single());
        Assert.Contains(document.Metadata, m => m.Label == "Period" && m.Value == "1 Jul 2026 – 31 Jul 2026");
        Assert.Contains(document.Metadata, m => m.Label == "Relationship manager" && m.Value == "All relationship managers");
        Assert.Contains(document.Metadata, m => m.Label == "Total donors contacted" && m.Value == "4");
    }

    [Fact]
    public void DonorsContacted_ManagerFilter_ShowsTheManagersName()
    {
        var managerId = Guid.NewGuid();

        var document = ReportDocumentFactory.DonorsContacted(ContactedReport(managerId), managerId, GeneratedAt);

        Assert.Contains(document.Metadata, m => m.Label == "Relationship manager" && m.Value == "Jane Doe");
    }

    [Fact]
    public void DonorsByRegion_MapsNameCodeAndCount()
    {
        var document = ReportDocumentFactory.DonorsByRegion(
            [new DonorsByRegionDto { Region = "JHB", RegionName = "Johannesburg", DonorCount = 2 }], GeneratedAt);

        Assert.Equal(new object[] { "Johannesburg", "JHB", 2 }, document.Rows.Single());
        Assert.Contains(document.Metadata, m => m.Label == "Snapshot as at" && m.Value == "8 Sep 2026 10:30 UTC");
    }

    [Fact]
    public void DonorsByStatus_SplitsEnumNamesAndTotalsDonors()
    {
        var document = ReportDocumentFactory.DonorsByStatus(
            [
                new DonorsByStatusDto { Status = "Active", DonorCount = 5 },
                new DonorsByStatusDto { Status = "PendingReview", DonorCount = 2 }
            ],
            GeneratedAt);

        Assert.Equal(["Active", "Pending Review"], document.Rows.Select(r => (string)r[0]));
        Assert.Contains(document.Metadata, m => m.Label == "Total donors" && m.Value == "7");
    }
}
