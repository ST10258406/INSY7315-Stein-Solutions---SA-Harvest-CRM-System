using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Donors.Dtos;
using CRM.Application.Modules.PublicDonors.Commands.SubmitPublicDonor;
using CRM.Application.Modules.PublicDonors.Dtos;
using NSubstitute;

namespace CRM.Application.Tests.Modules.PublicDonors.Commands;

public class SubmitPublicDonorCommandValidatorTests
{
    private const string ValidPngBase64 =
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUAAScy0NkAAAAASUVORK5CYII=";
    private const string ValidSignatureDataUri = $"data:image/png;base64,{ValidPngBase64}";

    private readonly ILookupRepository _lookupsMock = Substitute.For<ILookupRepository>();
    private readonly IUserRepository _usersMock = Substitute.For<IUserRepository>();
    private readonly SubmitPublicDonorCommandValidator _validator;

    public SubmitPublicDonorCommandValidatorTests()
    {
        _lookupsMock.CompanyTypeExistsActiveAsync(1, Arg.Any<CancellationToken>()).Returns(true);
        _lookupsMock.EntityTypeExistsActiveAsync(1, Arg.Any<CancellationToken>()).Returns(true);
        _lookupsMock.ProvinceExistsActiveAsync(1, Arg.Any<CancellationToken>()).Returns(true);
        _lookupsMock.DonationFrequencyExistsActiveAsync(1, Arg.Any<CancellationToken>()).Returns(true);
        _lookupsMock.CountActiveDonationTypesAsync(Arg.Any<IEnumerable<short>>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.ArgAt<IEnumerable<short>>(0).Count(id => id == 1));
        _lookupsMock.CountActiveOperationalRegionsAsync(Arg.Any<IEnumerable<short>>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.ArgAt<IEnumerable<short>>(0).Count(id => id == 1));

        _validator = new SubmitPublicDonorCommandValidator(_lookupsMock, _usersMock);
    }

    private static SubmitPublicDonorRequest MakeValidRequest() => new()
    {
        Company = new CreateDonorCompanyRequest
        {
            CompanyName = "Test Donor Pty Ltd",
            CompanyTypeId = 1,
            Website = "https://test.co.za",
            RegisteredCompanyName = "Test Donor (Pty) Ltd",
            TradingName = "Test Donor",
            EntityTypeId = 1,
            CompanyRegistrationNumber = "2020/000000/07",
            IncomeTaxNumber = "9012345678"
        },
        PrimaryContact = new CreateDonorContactRequest { Name = "Jane Tester", Phone = "+27820000000", Email = "jane@test.co.za" },
        LegalAddress = new CreateDonorLegalAddressRequest
        {
            StreetAddress = "1 Test Street",
            Suburb = "Testville",
            City = "Johannesburg",
            ProvinceId = 1,
            PostalCode = "2000"
        },
        Donations = new CreateDonorDonationsRequest
        {
            FrequencyId = 1,
            TypeIds = [1],
            CollectionAddress = "Gate 1",
            RegionIds = [1]
        },
        Signature = new PublicDonorSignatureRequest { ImageBase64 = ValidSignatureDataUri }
    };

    [Fact]
    public async Task ValidRequest_HasNoErrors()
    {
        var command = new SubmitPublicDonorCommand { Request = MakeValidRequest() };

        var result = await _validator.ValidateAsync(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task MissingDonorFields_AreCaughtByTheSharedCreateDonorRequestValidator()
    {
        // Proves reuse, not duplication — the same "Company" rule that
        // CreateDonorCommandValidator enforces fires here too, through the cast to
        // CreateDonorRequest, without SubmitPublicDonorCommandValidator repeating it.
        var request = MakeValidRequest();
        request.Company = null!;
        var command = new SubmitPublicDonorCommand { Request = request };

        var result = await _validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Request.Company");
    }

    [Fact]
    public async Task MissingSignature_HasError()
    {
        var request = MakeValidRequest();
        request.Signature = null!;
        var command = new SubmitPublicDonorCommand { Request = request };

        var result = await _validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Request.Signature");
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-data-uri")]
    [InlineData("data:image/jpeg;base64,abc123")]
    [InlineData("data:image/png;base64,not-valid-base64!!!")]
    public async Task InvalidImageBase64_HasError(string imageBase64)
    {
        var request = MakeValidRequest();
        request.Signature.ImageBase64 = imageBase64;
        var command = new SubmitPublicDonorCommand { Request = request };

        var result = await _validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Request.Signature.ImageBase64");
    }

    [Fact]
    public async Task ImageBase64_ClaimsPngButBytesAreNot_HasError()
    {
        var fakeBase64 = Convert.ToBase64String([0xFF, 0xD8, 0xFF, 0xE0]); // JPEG magic bytes
        var request = MakeValidRequest();
        request.Signature.ImageBase64 = $"data:image/png;base64,{fakeBase64}";
        var command = new SubmitPublicDonorCommand { Request = request };

        var result = await _validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Request.Signature.ImageBase64");
    }

    [Fact]
    public async Task EmptyRegionIds_HasError()
    {
        var request = MakeValidRequest();
        request.Donations.RegionIds = [];
        var command = new SubmitPublicDonorCommand { Request = request };

        var result = await _validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Request.Donations.RegionIds");
    }
}
