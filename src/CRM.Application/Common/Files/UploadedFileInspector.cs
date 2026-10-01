namespace CRM.Application.Common.Files;

using System.Text;
using CRM.Domain.Constants;
using CRM.Domain.Enums;

/// <summary>The file type identified from a file's leading bytes, never from client-supplied labels.</summary>
public sealed record SniffedFileType(string MimeType, string Extension);

/// <summary>
/// Shared document-upload checks for the internal (POST /donors/{id}/documents) and
/// public (POST /public/donors/submit/document) paths. Identifies the real file type
/// from its magic bytes and cross-checks it against the declared Content-Type and the
/// filename extension, so a renamed executable labelled application/pdf is rejected.
/// Does not replace malware scanning (Defender for Storage).
/// </summary>
public static class UploadedFileInspector
{
    public const int HeaderLength = 8;
    public const int MaxDisplayNameLength = 200;
    public const string DefaultDisplayName = "document";

    private static readonly SniffedFileType Pdf = new("application/pdf", ".pdf");
    private static readonly SniffedFileType Jpeg = new("image/jpeg", ".jpg");
    private static readonly SniffedFileType Png = new("image/png", ".png");

    private static readonly byte[] PdfSignature = "%PDF-"u8.ToArray();
    private static readonly byte[] JpegSignature = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    public static SniffedFileType? Sniff(ReadOnlySpan<byte> header)
    {
        if (header.StartsWith(PdfSignature)) return Pdf;
        if (header.StartsWith(JpegSignature)) return Jpeg;
        if (header.StartsWith(PngSignature)) return Png;
        return null;
    }

    /// <summary>Sniffs a seekable stream and restores its position. Returns null for non-seekable streams.</summary>
    public static SniffedFileType? Sniff(Stream stream)
    {
        if (!stream.CanSeek) return null;

        var start = stream.Position;
        try
        {
            var buffer = new byte[HeaderLength];
            var read = 0;
            while (read < buffer.Length)
            {
                var n = stream.Read(buffer, read, buffer.Length - read);
                if (n == 0) break;
                read += n;
            }
            return Sniff(buffer.AsSpan(0, read));
        }
        finally
        {
            stream.Position = start;
        }
    }

    /// <summary>
    /// Returns a human-readable error, or null when the upload is acceptable. The size
    /// limit is applied to the real stream length where the stream is seekable.
    /// </summary>
    public static string? Validate(
        Stream stream, long declaredSizeBytes, string declaredContentType, string fileName, DocumentType documentType)
    {
        if (!DocumentUploadLimits.AllowedMimeTypes.TryGetValue(documentType, out var allowed))
            return "Unsupported document type.";

        var length = RealLength(stream, declaredSizeBytes);
        if (length <= 0)
            return "File is empty.";
        if (length > DocumentUploadLimits.MaxFileSizeBytes)
            return "File exceeds 5MB limit.";

        var sniffed = Sniff(stream);
        if (sniffed is null || !allowed.Contains(sniffed.MimeType))
            return "Unsupported file type for this document type.";

        if (!string.Equals(declaredContentType, sniffed.MimeType, StringComparison.OrdinalIgnoreCase))
            return "The declared file type does not match the file contents.";

        if (!ExtensionMatches(fileName, sniffed))
            return "The file extension does not match the file contents.";

        return null;
    }

    public static long RealLength(Stream stream, long declaredSizeBytes) =>
        stream.CanSeek ? stream.Length : declaredSizeBytes;

    private static bool ExtensionMatches(string fileName, SniffedFileType sniffed)
    {
        var ext = Path.GetExtension(SanitizeDisplayName(fileName)).ToLowerInvariant();
        return sniffed.MimeType == "image/jpeg"
            ? ext is ".jpg" or ".jpeg"
            : ext == sniffed.Extension;
    }

    /// <summary>
    /// Produces a display-only name: keeps the last path segment, drops control characters,
    /// caps the length (keeping the extension) and falls back to a default when empty.
    /// Never used to build a storage path.
    /// </summary>
    public static string SanitizeDisplayName(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return DefaultDisplayName;

        var lastSeparator = fileName.LastIndexOfAny(['/', '\\']);
        var segment = lastSeparator >= 0 ? fileName[(lastSeparator + 1)..] : fileName;

        var sb = new StringBuilder(segment.Length);
        foreach (var c in segment)
            if (!char.IsControl(c)) sb.Append(c);

        var name = sb.ToString().Trim().TrimStart('.');
        if (name.Length == 0) return DefaultDisplayName;

        if (name.Length > MaxDisplayNameLength)
        {
            var ext = Path.GetExtension(name);
            if (ext.Length > 10) ext = string.Empty;
            name = name[..(MaxDisplayNameLength - ext.Length)] + ext;
        }

        return name;
    }
}
