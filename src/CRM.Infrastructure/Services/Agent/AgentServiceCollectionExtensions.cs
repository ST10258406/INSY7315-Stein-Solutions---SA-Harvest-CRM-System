namespace CRM.Infrastructure.Services.Agent;

using System.ClientModel;
using CRM.Application.Modules.Agent;
using CRM.Application.Modules.Agent.Tools;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OllamaSharp;
using OpenAI;

public static class AgentServiceCollectionExtensions
{
    /// <summary>
    /// Registers the agent's <see cref="IChatClient"/> for the configured provider
    /// (docs/agent-spec.md §9), wrapped in function invocation so tool calls run
    /// server-side. Swapping providers is a configuration change only.
    /// </summary>
    public static IServiceCollection AddAgent(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(AgentOptions.SectionName);
        services.Configure<AgentOptions>(section);
        var options = section.Get<AgentOptions>() ?? new AgentOptions();

        services.AddScoped<CrmAgentTools>();

        services.AddChatClient(_ => CreateProviderClient(options, configuration["AGENT_API_KEY"]))
            .UseFunctionInvocation(configure: f => f.MaximumIterationsPerRequest = options.MaxToolIterations);

        return services;
    }

    private static IChatClient CreateProviderClient(AgentOptions options, string? apiKey) =>
        options.Provider switch
        {
            "ollama" => new OllamaApiClient(new Uri(options.Endpoint), options.Model),
            "openai-compatible" => new OpenAIClient(
                    new ApiKeyCredential(apiKey ?? throw new InvalidOperationException(
                        "AGENT_API_KEY is not set. Add it to .env (local) or Azure Key Vault (production).")),
                    new OpenAIClientOptions { Endpoint = new Uri(options.Endpoint) })
                .GetChatClient(options.Model)
                .AsIChatClient(),
            _ => throw new InvalidOperationException($"Unknown Agent:Provider '{options.Provider}'. Use 'ollama' or 'openai-compatible'."),
        };
}
