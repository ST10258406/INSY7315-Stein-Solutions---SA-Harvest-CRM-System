namespace CRM.Application.Modules.Vetting.Uploads;

using CRM.Domain.Entities.Vetting;

/// <summary>
/// Allowed types and storage columns for vetting uploads. Declared here, not in the DocumentType
/// enum, because that enum is donor_documents.document_type: extending it would let donor uploads
/// accept vetting types.
/// </summary>
public static class VettingUploadSlots
{
    private static readonly string[] PdfOrImage = ["application/pdf", "image/jpeg", "image/png"];
    private static readonly string[] ImageOnly = ["image/jpeg", "image/png"];

    /// <summary>Evidence photo shot indexes 0–3 (the route constrains it).</summary>
    public static readonly IReadOnlyCollection<string> PhotoMimeTypes = ImageOnly;

    /// <summary>Display-name cap for vetting_evidence_photos.file_name.</summary>
    public const int PhotoFileNameMax = 100;

    // ponytail: one file per *_path column, as the schema has it; a re-upload replaces the path.
    // Several kitchen/facility photos would need a child table.
    public static readonly IReadOnlyDictionary<string, FileSlot> Files = new Dictionary<string, FileSlot>
    {
        ["npo-certificate"] = new(PdfOrImage, d => d.NpoCertificatePath, (d, p) => d.NpoCertificatePath = p),
        ["pbo-certificate"] = new(PdfOrImage, d => d.PboCertificatePath, (d, p) => d.PboCertificatePath = p),
        ["kitchen-storage-photos"] = new(ImageOnly, d => d.KitchenStoragePhotosPath, (d, p) => d.KitchenStoragePhotosPath = p),
        ["facility-photos"] = new(ImageOnly, d => d.FacilityPhotosPath, (d, p) => d.FacilityPhotosPath = p),
        ["certificates"] = new(PdfOrImage, d => d.CertificatesPath, (d, p) => d.CertificatesPath = p),
    };

    public sealed record FileSlot(
        IReadOnlyCollection<string> MimeTypes,
        Func<VettingAssessmentDetails, string?> GetPath,
        Action<VettingAssessmentDetails, string> SetPath);
}
