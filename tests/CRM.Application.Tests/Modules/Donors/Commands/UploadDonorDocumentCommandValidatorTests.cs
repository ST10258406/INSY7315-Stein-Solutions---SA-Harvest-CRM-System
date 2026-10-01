using CRM.Application.Tests.Common;
using CRM.Application.Modules.Donors.Commands.UploadDonorDocument;

namespace CRM.Application.Tests.Modules.Donors.Commands;

public class UploadDonorDocumentCommandValidatorTests
{
    private readonly UploadDonorDocumentCommandValidator _validator = new();

    private static UploadDonorDocumentCommand MakeValidCommand() => new()
    {
        DonorId = Guid.NewGuid(),
        DocumentType = "BBBEECertificate",
        FileStream = TestFiles.PdfStream(),
        OriginalFileName = "cert.pdf",
        ContentType = "application/pdf",
        FileSizeBytes = 1024
    };

    // PDF header followed by padding past the 5MB limit — the limit applies to the real stream length.
    private static byte[] Oversized()
    {
        var bytes = new byte[5 * 1024 * 1024 + 1];
        TestFiles.Pdf().CopyTo(bytes, 0);
        return bytes;
    }

    [Fact]
    public void Validate_NullFileStream_FailsWithoutThrowing()
    {
        var command = MakeValidCommand();
        command.FileStream = null!;

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_ValidBbbeeCertificatePdf_Passes()
    {
        var result = _validator.Validate(MakeValidCommand());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_SignatureAsPng_Passes()
    {
        var command = MakeValidCommand();
        command.DocumentType = "Signature";
        command.ContentType = "image/png";
        command.OriginalFileName = "signature.png";
        command.FileStream = new MemoryStream(TestFiles.Png());

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
    public void Validate_PngContentDeclaredAsPdf_Fails()
    {
        var command = MakeValidCommand();
        command.FileStream = new MemoryStream(TestFiles.Png());

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
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
    public void Validate_SignatureAsPdf_Fails()
    {
        var command = MakeValidCommand();
        command.DocumentType = "Signature";
        command.ContentType = "application/pdf";

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_UnsupportedMimeTypeForDocumentType_Fails()
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
    public void Validate_UnknownDocumentType_Fails()
    {
        var command = MakeValidCommand();
        command.DocumentType = "NotARealType";

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyDonorId_Fails()
    {
        var command = MakeValidCommand();
        command.DonorId = Guid.Empty;

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
    }
}
