namespace CRM.Domain.Enums;

/// <summary>
/// Type of file uploaded against a donor record.
/// </summary>
public enum DocumentType
{
    /// <summary>BBBEE compliance certificate — restricted to Admin/SuperAdmin.</summary>
    BBBEECertificate,

    /// <summary>Captured signature from the onboarding form.</summary>
    Signature
}
