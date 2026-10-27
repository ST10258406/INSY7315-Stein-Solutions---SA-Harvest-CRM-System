using CRM.Application.Common.Exceptions;
using CRM.Infrastructure.Services.Agent;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CRM.Infrastructure.Tests.Services;

public class AgentServiceCollectionExtensionsTests
{
    private static IServiceCollection AddAgent(Dictionary<string, string?> values) =>
        new ServiceCollection().AddAgent(new ConfigurationBuilder().AddInMemoryCollection(values).Build());

    [Theory]
    [InlineData("openai-compatible", "https://api.groq.com/openai/v1/", null, "AGENT_API_KEY")]
    [InlineData("anthropic", "https://api.example.com", "key", "Agent:Provider")]
    [InlineData("ollama", "not a url", null, "Agent:Endpoint")]
    public void AddAgent_EnabledWithBadSettings_FailsAtStartup(string provider, string endpoint, string? apiKey, string expected)
    {
        var error = Assert.Throws<InvalidOperationException>(() => AddAgent(new()
        {
            ["Agent:Enabled"] = "true",
            ["Agent:Provider"] = provider,
            ["Agent:Endpoint"] = endpoint,
            ["AGENT_API_KEY"] = apiKey,
        }));

        Assert.Contains(expected, error.Message);
    }

    [Fact]
    public void AddAgent_Disabled_ResolvingTheClientThrowsAgentUnavailable()
    {
        using var provider = AddAgent(new() { ["Agent:Enabled"] = "false", ["Agent:Provider"] = "openai-compatible" })
            .BuildServiceProvider();

        Assert.Throws<AgentUnavailableException>(() => provider.GetRequiredService<IChatClient>());
    }

    [Fact]
    public void AddAgent_EnabledOllama_ResolvesClient()
    {
        using var provider = AddAgent(new() { ["Agent:Enabled"] = "true" }).BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<IChatClient>());
    }
}
