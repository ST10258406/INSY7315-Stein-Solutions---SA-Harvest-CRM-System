using CRM.Application.Tests.Common;
using CRM.Application.Modules.PublicDonors.Commands.SubmitPublicDonorDocument;

namespace CRM.Application.Tests.Modules.PublicDonors.Commands;

public class SubmitPublicDonorDocumentCommandValidatorTests
{
    private readonly SubmitPublicDonorDocumentCommandValidator _validator = new();

    private static SubmitPublicDonorDocumentCommand MakeValidCommand() => new()
    {
        SessionToken = "opaque-session-token",
        DocumentType = "BBBEECertificate",
        FileStream = TestFiles.PdfStream(),
        OriginalFileName = "cert.pdf",
        ContentType = "application/pdf",
        FileSizeBytes = 1024
    };

    [Theory]
    [InlineData("application/pdf", "cert.pdf", "pdf")]
    [InlineData("image/jpeg", "scan.jpg", "jpeg")]
    [InlineData("image/jpeg", "scan.JPEG", "jpeg")]
    [InlineData("image/png", "scan.png", "png")]
    public void Validate_AllowedTypeWithMatchingContentAndExtension_Passes(string mimeType, string fileName, string kind)
    {
        var command = MakeValidCommand();
        command.ContentType = mimeType;
        command.OriginalFileName = fileName;
        command.FileStream = new MemoryStream(kind switch { "jpeg" => TestFiles.Jpeg(), "png" => TestFiles.Png(), _ => TestFiles.Pdf() });

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ExecutableRenamedToPdf_Fails()
    {
        var command = MakeValidCommand();
        command.FileStream = new MemoryStream(TestFiles.Executable());

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_PngContentWithPdfExtensionAndPdfContentType_Fails()
    {
        var command = MakeValidCommand();
        command.FileStream = new MemoryStream(TestFiles.Png());

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_PngContentWithPdfExtensionButPngContentType_FailsOnExtensionMismatch()
    {
        var command = MakeValidCommand();
        command.FileStream = new MemoryStream(TestFiles.Png());
        command.ContentType = "image/png";

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_ValidPdfWithNoExtension_Fails()
    {
        var command = MakeValidCommand();
        command.OriginalFileName = "cert";

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_FileExactlyAtLimit_Passes()
    {
        var command = MakeValidCommand();
        var bytes = new byte[5 * 1024 * 1024];
        TestFiles.Pdf().CopyTo(bytes, 0);
        command.FileStream = new MemoryStream(bytes);

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_DeclaredSizeSmallButRealStreamOversized_Fails()
    {
        var command = MakeValidCommand();
        command.FileSizeBytes = 10;
        command.FileStream = new MemoryStream(Oversized());

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_HostileFileName_StillPassesWhenContentAndExtensionMatch()
    {
        var command = MakeValidCommand();
        command.OriginalFileName = "a\\..\\..\\x.pdf";

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    // PDF header followed by padding past the 5MB limit — the limit applies to the real stream length.
    private static byte[] Oversized()
    {
        var bytes = new byte[5 * 1024 * 1024 + 1];
        TestFiles.Pdf().CopyTo(bytes, 0);
        return bytes;
    }

    [Fact]
    public void Validate_UnsupportedMimeType_Fails()
    {
        var command = MakeValidCommand();
        command.ContentType = "application/zip";

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_FileOverFiveMegabytes_Fails()
    {
        var command = MakeValidCommand();
        command.FileStream = new MemoryStream(Oversized());

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_ZeroSizeFile_Fails()
    {
        var command = MakeValidCommand();
        command.FileStream = new MemoryStream();

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
    }

    // This endpoint accepts exactly one documentType — unlike the internal
    // upload endpoint's validator, "Signature" (a value that IS a real
    // DocumentType) must still be rejected here, not just garbage strings.
    [Theory]
    [InlineData("Signature")]
    [InlineData("NotARealType")]
    [InlineData("")]
    [InlineData("bbbeecertificate")] // case-sensitive — lowercase must not slip through
    public void Validate_AnyDocumentTypeOtherThanBbbeeCertificate_Fails(string documentType)
    {
        var command = MakeValidCommand();
        command.DocumentType = documentType;

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_EmptySessionToken_Fails()
    {
        var command = MakeValidCommand();
        command.SessionToken = string.Empty;

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyOriginalFileName_Fails()
    {
        var command = MakeValidCommand();
        command.OriginalFileName = string.Empty;

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
    }
}
