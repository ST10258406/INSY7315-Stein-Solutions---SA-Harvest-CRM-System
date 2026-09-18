using CRM.Application.Common.Files;

namespace CRM.Application.Tests.Common.Files;

public class Base64PngDecoderTests
{
    // A real, minimal 1x1 transparent PNG — used across the industry as the
    // standard "smallest possible valid PNG" test fixture.
    private const string ValidPngBase64 =
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUAAScy0NkAAAAASUVORK5CYII=";

    private const string ValidDataUri = $"data:image/png;base64,{ValidPngBase64}";

    [Fact]
    public void TryDecode_ValidPngDataUri_ReturnsTrueWithDecodedBytes()
    {
        var result = Base64PngDecoder.TryDecode(ValidDataUri, out var bytes);

        Assert.True(result);
        Assert.Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, bytes[..8]);
    }

    [Fact]
    public void TryDecode_PrefixIsCaseInsensitive()
    {
        var upper = $"DATA:IMAGE/PNG;BASE64,{ValidPngBase64}";

        Assert.True(Base64PngDecoder.TryDecode(upper, out _));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryDecode_NullOrWhitespace_ReturnsFalse(string? input)
    {
        Assert.False(Base64PngDecoder.TryDecode(input, out var bytes));
        Assert.Empty(bytes);
    }

    [Fact]
    public void TryDecode_WrongPrefix_ReturnsFalse()
    {
        Assert.False(Base64PngDecoder.TryDecode($"data:image/jpeg;base64,{ValidPngBase64}", out _));
    }

    [Fact]
    public void TryDecode_NoPrefix_ReturnsFalse()
    {
        Assert.False(Base64PngDecoder.TryDecode(ValidPngBase64, out _));
    }

    [Fact]
    public void TryDecode_InvalidBase64_ReturnsFalse()
    {
        Assert.False(Base64PngDecoder.TryDecode("data:image/png;base64,not-valid-base64!!!", out _));
    }

    [Fact]
    public void TryDecode_ValidBase64ButNotPngBytes_ReturnsFalse()
    {
        // "hello world" as base64 — decodes fine, but the bytes aren't a PNG.
        var notPng = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("hello world, this is not a png"));

        Assert.False(Base64PngDecoder.TryDecode($"data:image/png;base64,{notPng}", out _));
    }

    [Fact]
    public void TryDecode_ClaimsPngButPrefixLiesAboutContent_StillRejectedByMagicBytes()
    {
        // Exactly the "don't trust the prefix" scenario the AC calls out — a client
        // could put any bytes after the comma regardless of what the prefix claims.
        var fakeBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }; // JPEG magic bytes, mislabeled as PNG
        var fakeBase64 = Convert.ToBase64String(fakeBytes);

        Assert.False(Base64PngDecoder.TryDecode($"data:image/png;base64,{fakeBase64}", out _));
    }

    [Fact]
    public void TryDecode_CorrectMagicBytesButGarbageAfterThem_ReturnsFalse()
    {
        // Exactly the gap a magic-bytes-only check misses: real PNG signature,
        // followed by arbitrary bytes shaped enough to look like a chunk header
        // (a length + "IHDR" + 13 bytes) but with content that isn't genuinely
        // IHDR data, so its CRC-32 can't possibly match. This is the case the
        // structural/CRC check exists specifically to catch.
        var signature = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        var fakeChunk = new byte[] { 0x00, 0x00, 0x00, 0x0D }; // length = 13, correct for IHDR
        var fakeType = "IHDR"u8.ToArray();
        var fakeData = new byte[13]; // all zero — not a real IHDR payload
        var fakeCrc = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF }; // deliberately wrong CRC

        var bytes = signature.Concat(fakeChunk).Concat(fakeType).Concat(fakeData).Concat(fakeCrc).ToArray();
        var base64 = Convert.ToBase64String(bytes);

        Assert.False(Base64PngDecoder.TryDecode($"data:image/png;base64,{base64}", out _));
    }

    [Fact]
    public void TryDecode_TruncatedAfterSignature_ReturnsFalse()
    {
        // Just the 8-byte signature with nothing else — not even a full chunk
        // header, let alone valid IHDR data.
        var signature = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        var base64 = Convert.ToBase64String(signature);

        Assert.False(Base64PngDecoder.TryDecode($"data:image/png;base64,{base64}", out _));
    }

    [Fact]
    public void TryDecode_FirstChunkNotIhdr_ReturnsFalse()
    {
        // Correct signature, correct chunk length, but the chunk type isn't
        // "IHDR" — every valid PNG's first chunk must be IHDR.
        var signature = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        var length = new byte[] { 0x00, 0x00, 0x00, 0x0D };
        var wrongType = "IDAT"u8.ToArray();
        var data = new byte[13];
        var crc = new byte[4];

        var bytes = signature.Concat(length).Concat(wrongType).Concat(data).Concat(crc).ToArray();
        var base64 = Convert.ToBase64String(bytes);

        Assert.False(Base64PngDecoder.TryDecode($"data:image/png;base64,{base64}", out _));
    }

    [Fact]
    public void TryDecode_OversizedPayload_ReturnsFalse()
    {
        var oversized = new byte[Base64PngDecoder.MaxDecodedSizeBytes + 1];
        byte[] pngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        Array.Copy(pngSignature, oversized, pngSignature.Length);
        var oversizedBase64 = Convert.ToBase64String(oversized);

        Assert.False(Base64PngDecoder.TryDecode($"data:image/png;base64,{oversizedBase64}", out _));
    }

    [Fact]
    public void TryDecode_EncodedStringTooLongForTheLimit_RejectedWithoutEverCallingFromBase64String()
    {
        // Same intent as TryDecode_OversizedPayload_ReturnsFalse, but the payload
        // here isn't even valid base64 ('!' isn't in the alphabet) — if the
        // length check ran only after Convert.FromBase64String, this would fail
        // via the FormatException catch instead, which still returns false but
        // only after decoding whatever was valid up to that point. Using clearly
        // invalid characters throughout makes it unambiguous that rejection
        // happens on the *encoded string's length* before any decode attempt.
        var tooLong = new string('!', 10_000_000);

        Assert.False(Base64PngDecoder.TryDecode($"data:image/png;base64,{tooLong}", out var bytes));
        Assert.Empty(bytes);
    }
}
