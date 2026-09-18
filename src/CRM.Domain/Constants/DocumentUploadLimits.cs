namespace CRM.Domain.Constants;

using CRM.Domain.Enums;

/// <summary>
/// Shared document-upload rules for both the internal (POST /donors/{id}/documents)
/// and public (POST /public/donors/submit/document) upload paths, so the two never
/// drift apart. Originally defined only in UploadDonorDocumentCommandValidator;
/// pulled out here when the public BBBEE-certificate follow-up endpoint needed the
/// exact same limits.
/// </summary>
public static class DocumentUploadLimits
{
    public const long MaxFileSizeBytes = 5 * 1024 * 1024;

    public static readonly IReadOnlyDictionary<DocumentType, string[]> AllowedMimeTypes = new Dictionary<DocumentType, string[]>
    {
        [DocumentType.BBBEECertificate] = ["application/pdf", "image/jpeg", "image/png"],
        [DocumentType.Signature] = ["image/png"]
    };
}
