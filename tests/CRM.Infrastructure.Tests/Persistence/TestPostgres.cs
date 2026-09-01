namespace CRM.Infrastructure.Tests.Persistence;

using System;

/// <summary>
/// Connection string for the real-Postgres Infrastructure tests
/// (RefreshTokenConfigurationTests, and everything under Persistence/Repositories/).
/// Defaults to the same local/CI Postgres instance already configured in
/// docker-compose.yml and .github/workflows/backend.yml, so `dotnet test` keeps
/// working with no extra setup. Set CRM_TEST_POSTGRES_CONNECTION_STRING to point
/// these tests at a different host, user, or password instead.
/// </summary>
internal static class TestPostgres
{
    private const string DefaultTemplate =
        "Host=localhost;Database={0};Username=postgres;Password=P@ss1234ID";

    public static string ConnectionString(string databaseName)
    {
        var overrideValue = Environment.GetEnvironmentVariable("CRM_TEST_POSTGRES_CONNECTION_STRING");
        return string.IsNullOrWhiteSpace(overrideValue)
            ? string.Format(DefaultTemplate, databaseName)
            : overrideValue;
    }
}
