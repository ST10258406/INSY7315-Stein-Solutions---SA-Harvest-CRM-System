namespace CRM.Application.Common.Files;

/// <summary>
/// Decodes a "data:image/png;base64,..." data URI into raw PNG bytes, verifying
/// the decoded bytes are structurally a real PNG rather than trusting the
/// "image/png" claim in the prefix — a client can put any string before the
/// comma, and a plain 8-byte magic-number check alone accepts any garbage
/// prefixed with those bytes. This additionally parses the mandatory first
/// chunk (IHDR, always exactly 13 bytes for every valid PNG) and verifies its
/// CRC-32 against the PNG spec's own checksum — a hand-crafted payload would
/// need a correctly computed CRC over its claimed IHDR content to pass, which
/// forging a "PNG-shaped" blob of arbitrary bytes does not give you for free.
/// This is not a full image decoder (later chunks/IEND aren't parsed) — that
/// tradeoff is deliberate: verifying the one chunk every PNG is required to
/// start with is enough to catch "magic bytes + garbage" payloads without
/// pulling in a full image-decoding dependency for a single upload path.
/// Used by SubmitPublicDonorCommand's validator (to reject bad input with a
/// 400) and its handler (to get the bytes to upload); both call the same
/// method so there is exactly one place that defines "is this a valid PNG".
/// </summary>
public static class Base64PngDecoder
{
    public const long MaxDecodedSizeBytes = 5 * 1024 * 1024;

    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] IhdrType = "IHDR"u8.ToArray();
    private const int IhdrDataLength = 13; // fixed by the PNG spec for every IHDR chunk
    private const string ExpectedPrefix = "data:image/png;base64,";

    public static bool TryDecode(string? dataUri, out byte[] pngBytes)
    {
        pngBytes = [];

        if (string.IsNullOrWhiteSpace(dataUri))
            return false;

        if (!dataUri.StartsWith(ExpectedPrefix, StringComparison.OrdinalIgnoreCase))
            return false;

        var base64 = dataUri[ExpectedPrefix.Length..];

        // Reject on the encoded string's length before ever calling
        // Convert.FromBase64String — that call allocates and decodes the whole
        // payload up front, so checking MaxDecodedSizeBytes only after it runs
        // (see below) already cost the allocation. Base64 expands 3 bytes to 4
        // characters, so this is the smallest encoded length that could possibly
        // decode to more than MaxDecodedSizeBytes; anything longer is rejected
        // without ever decoding it.
        if (base64.Length > ((MaxDecodedSizeBytes + 2) / 3) * 4)
            return false;

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

        if (!IsStructurallyValidPng(decoded))
            return false;

        pngBytes = decoded;
        return true;
    }

    private static bool IsStructurallyValidPng(byte[] data)
    {
        // Signature (8) + IHDR length (4) + IHDR type (4) + IHDR data (13) + CRC (4).
        const int minLength = 8 + 4 + 4 + IhdrDataLength + 4;
        if (data.Length < minLength)
            return false;

        if (!data.AsSpan(0, PngSignature.Length).SequenceEqual(PngSignature))
            return false;

        var chunkLength = ReadUInt32BigEndian(data.AsSpan(8, 4));
        if (chunkLength != IhdrDataLength)
            return false;

        var typeAndData = data.AsSpan(12, 4 + IhdrDataLength); // "IHDR" + its 13 data bytes
        if (!typeAndData[..4].SequenceEqual(IhdrType))
            return false;

        var expectedCrc = ReadUInt32BigEndian(data.AsSpan(12 + 4 + IhdrDataLength, 4));
        var actualCrc = Crc32.Compute(typeAndData);

        return expectedCrc == actualCrc;
    }

    private static uint ReadUInt32BigEndian(ReadOnlySpan<byte> bytes) =>
        (uint)(bytes[0] << 24 | bytes[1] << 16 | bytes[2] << 8 | bytes[3]);

    /// <summary>
    /// The CRC-32 variant mandated by the PNG spec (Annex D) — identical
    /// algorithm/polynomial to zlib's crc32() and the one used by every PNG
    /// encoder to checksum each chunk. Implemented directly (no external
    /// dependency) since this is the one narrowly-scoped use of it in the
    /// codebase.
    /// </summary>
    private static class Crc32
    {
        private static readonly uint[] Table = BuildTable();

        private static uint[] BuildTable()
        {
            var table = new uint[256];
            for (uint n = 0; n < 256; n++)
            {
                var c = n;
                for (var k = 0; k < 8; k++)
                {
                    c = (c & 1) != 0 ? 0xEDB88320 ^ (c >> 1) : c >> 1;
                }
                table[n] = c;
            }
            return table;
        }

        public static uint Compute(ReadOnlySpan<byte> bytes)
        {
            var crc = 0xFFFFFFFFu;
            foreach (var b in bytes)
            {
                crc = Table[(crc ^ b) & 0xFF] ^ (crc >> 8);
            }
            return crc ^ 0xFFFFFFFFu;
        }
    }
}
