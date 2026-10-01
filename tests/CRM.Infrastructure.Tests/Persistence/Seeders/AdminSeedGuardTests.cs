using CRM.Infrastructure.Persistence.Seeders;
using Microsoft.Extensions.Configuration;

namespace CRM.Infrastructure.Tests.Persistence.Seeders;

public class AdminSeedGuardTests
{
    private static IConfiguration Config(params (string Key, string? Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(v => new KeyValuePair<string, string?>(v.Key, v.Value)))
            .Build();

    [Theory]
    [InlineData("ChangeMe123!")]
    [InlineData("changeme123!")]
    [InlineData("Password123!")]
    [InlineData("MyChangeMe-Secret1!")]
    public void EnsureSafe_ExamplePasswordOutsideDevelopment_Throws(string password)
    {
        var config = Config(("ADMIN_DEFAULT_PASSWORD", password), ("ADMIN_EMAIL", "ops@saharvest.org.za"));

        Assert.Throws<InvalidOperationException>(() => AdminSeedGuard.EnsureSafe(config, isDevelopment: false));
    }

    [Theory]
    [InlineData("Sh0rt!pw")] // under 12 characters
    [InlineData("nouppercase-1234567")]
    [InlineData("NoDigitsHere-abcdef")]
    [InlineData("NoSpecialChars12345")]
    public void EnsureSafe_PasswordBreakingPolicyOutsideDevelopment_Throws(string password)
    {
        var config = Config(("ADMIN_DEFAULT_PASSWORD", password), ("ADMIN_EMAIL", "ops@saharvest.org.za"));

        Assert.Throws<InvalidOperationException>(() => AdminSeedGuard.EnsureSafe(config, isDevelopment: false));
    }

    [Fact]
    public void EnsureSafe_StrongUniquePassword_Passes() =>
        AdminSeedGuard.EnsureSafe(
            Config(("ADMIN_DEFAULT_PASSWORD", "k9#Vq2!xLm7@Rt4w"), ("ADMIN_EMAIL", "ops@saharvest.org.za")), isDevelopment: false);

    [Fact]
    public void EnsureSafe_MissingAdminEmailOutsideDevelopment_Throws() =>
        Assert.Throws<InvalidOperationException>(() =>
            AdminSeedGuard.EnsureSafe(Config(("ADMIN_DEFAULT_PASSWORD", "k9#Vq2!xLm7@Rt4w")), isDevelopment: false));

    [Fact]
    public void EnsureSafe_NoPasswordButValidEmail_Passes() =>
        AdminSeedGuard.EnsureSafe(Config(("ADMIN_EMAIL", "ops@saharvest.org.za")), isDevelopment: false);

    [Fact]
    public void EnsureSafe_ExamplePasswordInDevelopment_IsAllowed() =>
        AdminSeedGuard.EnsureSafe(Config(("ADMIN_DEFAULT_PASSWORD", "ChangeMe123!")), isDevelopment: true);

    [Fact]
    public void ResolveAdminEmail_OutsideDevelopmentWithoutConfig_Throws() =>
        Assert.Throws<InvalidOperationException>(() => AdminSeedGuard.ResolveAdminEmail(Config(), isDevelopment: false));

    [Theory]
    [InlineData("admin@crm.local")]
    [InlineData("not-an-email")]
    [InlineData("admin@example.com")]
    [InlineData("admin@localhost")]
    public void ResolveAdminEmail_NonRoutableOutsideDevelopment_Throws(string email) =>
        Assert.Throws<InvalidOperationException>(() =>
            AdminSeedGuard.ResolveAdminEmail(Config(("ADMIN_EMAIL", email)), isDevelopment: false));

    [Fact]
    public void ResolveAdminEmail_RealAddress_IsReturned() =>
        Assert.Equal("ops@saharvest.org.za",
            AdminSeedGuard.ResolveAdminEmail(Config(("ADMIN_EMAIL", "ops@saharvest.org.za")), isDevelopment: false));

    [Fact]
    public void ResolveAdminEmail_DevelopmentWithoutConfig_FallsBackToLocalDefault() =>
        Assert.Equal(AdminSeedGuard.DevelopmentDefaultEmail,
            AdminSeedGuard.ResolveAdminEmail(Config(), isDevelopment: true));
}
