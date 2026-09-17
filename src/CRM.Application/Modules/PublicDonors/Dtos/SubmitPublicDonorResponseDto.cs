namespace CRM.Application.Modules.PublicDonors.Dtos;

// Deliberately just these three fields — never the donor's Guid Id. See
// SubmitPublicDonorCommandHandler for why SubmissionToken is safe to return
// (it's an unrelated random value, not derived from or decodable to the Id).
public class SubmitPublicDonorResponseDto
{
    public string Message { get; set; } = string.Empty;
    public string ReferenceNumber { get; set; } = string.Empty;

    /// <summary>
    /// One-time opaque credential for linking a follow-up
    /// POST /public/donors/submit/document call to this donor. Expires in 1 hour
    /// (SubmitPublicDonorCommandHandler.SubmissionTokenLifetime).
    /// </summary>
    public string SubmissionToken { get; set; } = string.Empty;
}
