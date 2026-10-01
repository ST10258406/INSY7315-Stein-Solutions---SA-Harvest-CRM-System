namespace CRM.Application.Tests.Common;

/// <summary>Minimal byte payloads that carry the real magic bytes of each allowed upload type.</summary>
public static class TestFiles
{
    public static byte[] Pdf() => "%PDF-1.7\n%test"u8.ToArray();

    public static byte[] Jpeg() => [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46];

    public static byte[] Png() => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00];

    /// <summary>A Windows executable header — what a renamed .exe looks like.</summary>
    public static byte[] Executable() => [0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00];

    public static MemoryStream PdfStream() => new(Pdf());
}
