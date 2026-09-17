using CRM.Application.Modules.PublicDonors.Commands.SubmitPublicDonorDocument;

namespace CRM.Application.Tests.Modules.PublicDonors.Commands;

public class SubmitPublicDonorDocumentCommandValidatorTests
{
    private readonly SubmitPublicDonorDocumentCommandValidator _validator = new();

    private static SubmitPublicDonorDocumentCommand MakeValidCommand() => new()
    {
        SessionToken = "opaque-session-token",
        DocumentType = "BBBEECertificate",
        FileStream = new MemoryStream(),
        OriginalFileName = "cert.pdf",
        ContentType = "application/pdf",
        FileSizeBytes = 1024
    };

    [Theory]
    [InlineData("application/pdf")]
    [InlineData("image/jpeg")]
    [InlineData("image/png")]
    public void Validate_AllowedMimeType_Passes(string mimeType)
    {
        var command = MakeValidCommand();
        command.ContentType = mimeType;

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
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
        command.FileSizeBytes = 6_000_000;

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_ZeroSizeFile_Fails()
    {
        var command = MakeValidCommand();
        command.FileSizeBytes = 0;

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
