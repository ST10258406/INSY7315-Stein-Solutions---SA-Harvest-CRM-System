using CRM.Application.Modules.Donors.Commands.UploadDonorDocument;

namespace CRM.Application.Tests.Modules.Donors.Commands;

public class UploadDonorDocumentCommandValidatorTests
{
    private readonly UploadDonorDocumentCommandValidator _validator = new();

    private static UploadDonorDocumentCommand MakeValidCommand() => new()
    {
        DonorId = Guid.NewGuid(),
        DocumentType = "BBBEECertificate",
        FileStream = new MemoryStream(),
        OriginalFileName = "cert.pdf",
        ContentType = "application/pdf",
        FileSizeBytes = 1024
    };

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

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
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
        command.FileSizeBytes = 6_000_000;

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
