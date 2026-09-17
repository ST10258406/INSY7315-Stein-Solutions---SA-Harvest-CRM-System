namespace CRM.Application.Common.Files;

/// <summary>
/// Decodes a "data:image/png;base64,..." data URI into raw PNG bytes, verifying
/// the decoded bytes actually start with the PNG file signature rather than
/// trusting the "image/png" claim in the prefix — a client can put any string
/// before the comma. Used by SubmitPublicDonorCommand's validator (to reject bad
/// input with a 400) and its handler (to get the bytes to upload); both call the
/// same method so there is exactly one place that defines "is this a valid PNG".
/// </summary>
public static class Base64PngDecoder
{
    public const long MaxDecodedSizeBytes = 5 * 1024 * 1024;

    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private const string ExpectedPrefix = "data:image/png;base64,";

    public static bool TryDecode(string? dataUri, out byte[] pngBytes)
    {
        pngBytes = [];

        if (string.IsNullOrWhiteSpace(dataUri))
            return false;

        if (!dataUri.StartsWith(ExpectedPrefix, StringComparison.OrdinalIgnoreCase))
            return false;

        var base64 = dataUri[ExpectedPrefix.Length..];

        byte[] decoded;
        try
        {
            decoded = Convert.FromBase64String(base64);
        }
        catch (FormatException)
        {
            return false;
        }

        if (decoded.Length == 0 || decoded.Length > MaxDecodedSizeBytes)
            return false;

        if (decoded.Length < PngSignature.Length || !decoded.AsSpan(0, PngSignature.Length).SequenceEqual(PngSignature))
            return false;

        pngBytes = decoded;
        return true;
    }
}
