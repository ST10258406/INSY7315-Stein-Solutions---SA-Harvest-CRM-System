using CRM.Application.Common.Files;

namespace CRM.Application.Tests.Common;

public class UploadedFileInspectorTests
{
    [Fact]
    public void Sniff_RecognisesPdfJpegAndPng()
    {
        Assert.Equal("application/pdf", UploadedFileInspector.Sniff(TestFiles.Pdf())!.MimeType);
        Assert.Equal("image/jpeg", UploadedFileInspector.Sniff(TestFiles.Jpeg())!.MimeType);
        Assert.Equal("image/png", UploadedFileInspector.Sniff(TestFiles.Png())!.MimeType);
    }

    [Fact]
    public void Sniff_ExecutableOrEmptyOrTruncated_ReturnsNull()
    {
        Assert.Null(UploadedFileInspector.Sniff(TestFiles.Executable()));
        Assert.Null(UploadedFileInspector.Sniff([]));
        Assert.Null(UploadedFileInspector.Sniff(new byte[] { 0x89, 0x50, 0x4E }));
    }

    [Fact]
    public void Sniff_Stream_RestoresPosition()
    {
        var stream = TestFiles.PdfStream();

        UploadedFileInspector.Sniff(stream);

        Assert.Equal(0, stream.Position);
    }

    [Theory]
    [InlineData("a\\..\\..\\x.pdf", "x.pdf")]
    [InlineData("../../etc/passwd.pdf", "passwd.pdf")]
    [InlineData("cert\u0000\u0007.pdf", "cert.pdf")]
    [InlineData("   ", UploadedFileInspector.DefaultDisplayName)]
    [InlineData("", UploadedFileInspector.DefaultDisplayName)]
    [InlineData(null, UploadedFileInspector.DefaultDisplayName)]
    [InlineData("folder/", UploadedFileInspector.DefaultDisplayName)]
    [InlineData("...", UploadedFileInspector.DefaultDisplayName)]
    public void SanitizeDisplayName_StripsSeparatorsAndControlCharacters(string? input, string expected)
    {
        Assert.Equal(expected, UploadedFileInspector.SanitizeDisplayName(input));
    }

    [Fact]
    public void SanitizeDisplayName_LongName_IsCappedAndKeepsExtension()
    {
        var result = UploadedFileInspector.SanitizeDisplayName(new string('a', 500) + ".pdf");

        Assert.Equal(UploadedFileInspector.MaxDisplayNameLength, result.Length);
        Assert.EndsWith(".pdf", result);
    }

    [Fact]
    public void Validate_SignatureRejectsPdfContent()
    {
        var error = UploadedFileInspector.Validate(
            TestFiles.PdfStream(), 10, "application/pdf", "sig.pdf", CRM.Domain.Enums.DocumentType.Signature);

        Assert.NotNull(error);
    }
}
