using CRM.Application.Common.Interfaces;
using CRM.Application.Modules.Donors.Commands.CreateDonor;
using CRM.Application.Modules.Donors.Commands.UpdateDonor;
using CRM.Application.Modules.Donors.Dtos;
using NSubstitute;

namespace CRM.Application.Tests.Modules.Donors.Commands;

/// <summary>Boundary tests for the shared donor field rules (create + update validators).</summary>
public class DonorInputLimitsTests
{
    private readonly ILookupRepository _lookups = Substitute.For<ILookupRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly CreateDonorRequestValidator _create;
    private readonly UpdateDonorCommandValidator _update;

    public DonorInputLimitsTests()
    {
        _lookups.CompanyTypeExistsActiveAsync(Arg.Any<short>(), Arg.Any<CancellationToken>()).Returns(true);
        _lookups.EntityTypeExistsActiveAsync(Arg.Any<short>(), Arg.Any<CancellationToken>()).Returns(true);
        _lookups.ProvinceExistsActiveAsync(Arg.Any<short>(), Arg.Any<CancellationToken>()).Returns(true);
        _lookups.DonationFrequencyExistsActiveAsync(Arg.Any<short>(), Arg.Any<CancellationToken>()).Returns(true);
        _lookups.CountActiveDonationTypesAsync(Arg.Any<IEnumerable<short>>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.ArgAt<IEnumerable<short>>(0).Count());
        _lookups.CountActiveOperationalRegionsAsync(Arg.Any<IEnumerable<short>>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.ArgAt<IEnumerable<short>>(0).Count());

        _create = new CreateDonorRequestValidator(_lookups, _users);
        _update = new UpdateDonorCommandValidator(_lookups, _users);
    }

    private static CreateDonorRequest Valid() => new()
    {
        Company = new CreateDonorCompanyRequest
        {
            CompanyName = "Test Donor", CompanyTypeId = 1, Website = "https://test.co.za",
            RegisteredCompanyName = "Test Donor (Pty) Ltd", TradingName = "Test", EntityTypeId = 1,
            CompanyRegistrationNumber = "2020/000000/07", IncomeTaxNumber = "9012345678"
        },
        PrimaryContact = new CreateDonorContactRequest { Name = "Jane", Phone = "+27820000000", Email = "jane@test.co.za" },
        LegalAddress = new CreateDonorLegalAddressRequest
        {
            StreetAddress = "1 Test St", Suburb = "Testville", City = "Joburg", ProvinceId = 1, PostalCode = "2000"
        },
        Donations = new CreateDonorDonationsRequest
        {
            FrequencyId = 1, TypeIds = [1], CollectionAddress = "Gate 1", RegionIds = [1]
        }
    };

    private async Task<bool> IsValidAsync(Action<CreateDonorRequest> mutate)
    {
        var r = Valid();
        mutate(r);
        return (await _create.ValidateAsync(r)).IsValid;
    }

    [Fact]
    public async Task ValidBaseline_Passes() => Assert.True(await IsValidAsync(_ => { }));

    [Theory]
    [InlineData(255, true)]
    [InlineData(256, false)]
    public async Task CompanyName_Boundary(int length, bool ok) =>
        Assert.Equal(ok, await IsValidAsync(r => r.Company.CompanyName = new string('a', length)));

    [Theory]
    [InlineData(255, true)]
    [InlineData(256, false)]
    public async Task RegisteredAndTradingName_Boundary(int length, bool ok)
    {
        Assert.Equal(ok, await IsValidAsync(r => r.Company.RegisteredCompanyName = new string('a', length)));
        Assert.Equal(ok, await IsValidAsync(r => r.Company.TradingName = new string('a', length)));
    }

    [Theory]
    [InlineData(100, true)]
    [InlineData(101, false)]
    public async Task RegistrationAndTaxNumber_Boundary(int length, bool ok)
    {
        Assert.Equal(ok, await IsValidAsync(r => r.Company.CompanyRegistrationNumber = new string('1', length)));
        Assert.Equal(ok, await IsValidAsync(r => r.Company.IncomeTaxNumber = new string('9', length)));
    }

    [Theory]
    [InlineData(500, true)]
    [InlineData(501, false)]
    public async Task Website_LengthBoundary(int length, bool ok)
    {
        const string prefix = "https://a.co/";
        var url = prefix + new string('a', length - prefix.Length);
        Assert.Equal(ok, await IsValidAsync(r => r.Company.Website = url));
    }

    [Theory]
    [InlineData("https://example.com", true)]
    [InlineData("http://example.com/path?q=1", true)]
    [InlineData("javascript:alert(1)", false)]
    [InlineData("data:text/html;base64,AAAA", false)]
    [InlineData("ftp://example.com", false)]
    [InlineData("/relative/path", false)]
    [InlineData("example.com", false)]
    public async Task Website_MustBeAbsoluteHttpOrHttps(string website, bool ok) =>
        Assert.Equal(ok, await IsValidAsync(r => r.Company.Website = website));

    [Theory]
    [InlineData(255, true)]
    [InlineData(256, false)]
    public async Task ContactNameAndJobTitle_Boundary(int length, bool ok)
    {
        Assert.Equal(ok, await IsValidAsync(r => r.PrimaryContact.Name = new string('a', length)));
        Assert.Equal(ok, await IsValidAsync(r => r.PrimaryContact.JobTitle = new string('a', length)));
    }

    [Theory]
    [InlineData("+27 82 000 0000", true)]
    [InlineData("082 000 0000", true)]
    [InlineData("(011) 555-1234", true)]
    [InlineData("0027820000000", true)]
    [InlineData("+44 20 7946 0958", false)] // non-SA
    [InlineData("12345", false)]
    [InlineData("082 000 000", false)] // too short
    [InlineData("+278200000001", false)] // too long
    [InlineData("abc-defg-hij", false)]
    [InlineData("<script>12345678</script>", false)]
    public async Task Phone_Rules(string phone, bool ok) =>
        Assert.Equal(ok, await IsValidAsync(r => r.PrimaryContact.Phone = phone));

    [Theory]
    [InlineData("2000", true)]
    [InlineData("0001", true)]
    [InlineData("SW1A 1AA", false)] // non-SA
    [InlineData("200", false)]
    [InlineData("20000", false)]
    [InlineData("20 0", false)]
    [InlineData("<b>", false)]
    public async Task PostalCode_Rules(string code, bool ok) =>
        Assert.Equal(ok, await IsValidAsync(r => r.LegalAddress.PostalCode = code));

    [Theory]
    [InlineData(255, true)]
    [InlineData(256, false)]
    public async Task StreetAddress_Boundary(int length, bool ok) =>
        Assert.Equal(ok, await IsValidAsync(r => r.LegalAddress.StreetAddress = new string('a', length)));

    [Theory]
    [InlineData(100, true)]
    [InlineData(101, false)]
    public async Task SuburbAndCity_Boundary(int length, bool ok)
    {
        Assert.Equal(ok, await IsValidAsync(r => r.LegalAddress.Suburb = new string('a', length)));
        Assert.Equal(ok, await IsValidAsync(r => r.LegalAddress.City = new string('a', length)));
    }

    [Theory]
    [InlineData(4000, true)]
    [InlineData(4001, false)]
    public async Task FreeTextFields_Boundary(int length, bool ok)
    {
        var text = new string('a', length);
        Assert.Equal(ok, await IsValidAsync(r => r.Donations.CollectionAddress = text));
        Assert.Equal(ok, await IsValidAsync(r => r.Donations.OperationsLogisticsDetails = text));
        Assert.Equal(ok, await IsValidAsync(r => r.Crm = new CreateDonorCrmRequest { AdditionalInformation = text }));
        Assert.Equal(ok, await IsValidAsync(r => r.Crm = new CreateDonorCrmRequest { ImpactReportingPreferences = text }));
    }

    [Theory]
    [InlineData(20, true)]
    [InlineData(21, false)]
    public async Task TypeIdsAndRegionIds_AreCapped(int count, bool ok)
    {
        var ids = Enumerable.Range(1, count).Select(i => (short)i).ToList();
        Assert.Equal(ok, await IsValidAsync(r => r.Donations.TypeIds = ids));
        Assert.Equal(ok, await IsValidAsync(r => r.Donations.RegionIds = ids));
    }

    [Fact]
    public async Task OversizedIdList_NeverReachesTheDatabaseLookup()
    {
        var ids = Enumerable.Range(1, 500).Select(i => (short)i).ToList();

        await IsValidAsync(r => r.Donations.TypeIds = ids);

        await _lookups.DidNotReceive().CountActiveDonationTypesAsync(Arg.Any<IEnumerable<short>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NonPositiveId_IsRejected() =>
        Assert.False(await IsValidAsync(r => r.Donations.TypeIds = [1, 0]));

    // ---- Update validator uses the same rules ----

    private async Task<bool> UpdateValidAsync(UpdateDonorRequest request) =>
        (await _update.ValidateAsync(new UpdateDonorCommand { Id = Guid.NewGuid(), Request = request })).IsValid;

    [Theory]
    [InlineData(255, true)]
    [InlineData(256, false)]
    public async Task Update_CompanyName_Boundary(int length, bool ok) =>
        Assert.Equal(ok, await UpdateValidAsync(new UpdateDonorRequest
        {
            Company = new UpdateDonorCompanyRequest { CompanyName = new string('a', length) }
        }));

    [Theory]
    [InlineData("https://ok.co.za", true)]
    [InlineData("javascript:alert(1)", false)]
    public async Task Update_Website_MustBeHttpUrl(string website, bool ok) =>
        Assert.Equal(ok, await UpdateValidAsync(new UpdateDonorRequest
        {
            Company = new UpdateDonorCompanyRequest { Website = website }
        }));

    [Theory]
    [InlineData(20, true)]
    [InlineData(21, false)]
    public async Task Update_TypeIds_AreCapped(int count, bool ok) =>
        Assert.Equal(ok, await UpdateValidAsync(new UpdateDonorRequest
        {
            Donations = new UpdateDonorDonationsRequest
            {
                TypeIds = Enumerable.Range(1, count).Select(i => (short)i).ToList()
            }
        }));

    [Theory]
    [InlineData(4, true)]
    [InlineData(5, false)]
    public async Task Update_PostalCode_Boundary(int length, bool ok) =>
        Assert.Equal(ok, await UpdateValidAsync(new UpdateDonorRequest
        {
            LegalAddress = new UpdateDonorLegalAddressRequest { PostalCode = new string('1', length) }
        }));
}
