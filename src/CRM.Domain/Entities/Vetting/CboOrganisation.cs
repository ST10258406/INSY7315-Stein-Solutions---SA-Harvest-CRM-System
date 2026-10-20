namespace CRM.Domain.Entities.Vetting;

using CRM.Domain.Common;
using CRM.Domain.Enums;

public class CboOrganisation : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    /// <summary>One of VettingOptions.Provinces.</summary>
    public string Province { get; set; } = string.Empty;
    public string AreaSuburb { get; set; } = string.Empty;
    public int? MealsPerDay { get; set; }
    public int? PeopleServed { get; set; }
    public CboVettingStatus VettingStatus { get; set; } = CboVettingStatus.New;

    public ICollection<VettingSiteVisit> SiteVisits { get; set; } = new List<VettingSiteVisit>();
    public ICollection<VettingSubmission> Submissions { get; set; } = new List<VettingSubmission>();
}
