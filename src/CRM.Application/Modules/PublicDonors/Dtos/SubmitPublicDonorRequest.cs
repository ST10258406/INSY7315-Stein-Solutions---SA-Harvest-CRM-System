namespace CRM.Application.Modules.PublicDonors.Dtos;

using CRM.Application.Modules.Donors.Dtos;

/// <summary>
/// Body of POST /public/donors/submit — per the design doc, "the same donor
/// structure as the internal POST /donors endpoint, plus a signature". Inheriting
/// CreateDonorRequest gives that exact flat shape (every CreateDonorRequest field
/// at the top level, "signature" alongside them) with zero duplicated fields, and
/// lets SubmitPublicDonorCommandValidator reuse CreateDonorRequestValidator as-is.
/// </summary>
public class SubmitPublicDonorRequest : CreateDonorRequest
{
    public PublicDonorSignatureRequest Signature { get; set; } = null!;
}

public class PublicDonorSignatureRequest
{
    /// <summary>A "data:image/png;base64,..." data URI — see Base64PngDecoder.</summary>
    public string ImageBase64 { get; set; } = string.Empty;
}
