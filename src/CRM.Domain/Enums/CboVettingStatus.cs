namespace CRM.Domain.Enums;

/// <summary>Vetting lifecycle of a CBO. Driven by its submissions; Admin may set Active / VettingDue by hand.</summary>
public enum CboVettingStatus
{
    New,
    VettingDue,
    Pending,
    Returned,
    Vetted,
    Active
}
