namespace CRM.Infrastructure.Services.Agent;

using System.ClientModel;
using CRM.Application.Common.Exceptions;
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

        var apiKey = configuration["AGENT_API_KEY"];

        // Fail at startup, not on the first request, when an enabled agent is misconfigured.
        if (options.Enabled && Validate(options, apiKey) is { } error)
            throw new InvalidOperationException(error);

        services.AddChatClient(_ => options.Enabled
                ? CreateProviderClient(options, apiKey!)
                // Resolved when the handler is built, so a disabled agent maps to 503 like any other outage.
                : throw new AgentUnavailableException("The agent is disabled (Agent:Enabled = false)."))
            .UseFunctionInvocation(configure: f => f.MaximumIterationsPerRequest = options.MaxToolIterations);

        return services;
    }

    /// <summary>The startup error for an enabled agent's settings, or null when they are usable.</summary>
    public static string? Validate(AgentOptions options, string? apiKey)
    {
        if (options.Provider is not ("ollama" or "openai-compatible"))
            return $"Unknown Agent:Provider '{options.Provider}'. Use 'ollama' or 'openai-compatible'.";
        if (!Uri.TryCreate(options.Endpoint, UriKind.Absolute, out _))
            return $"Agent:Endpoint '{options.Endpoint}' must be an absolute URL.";
        if (options.Provider == "openai-compatible" && string.IsNullOrWhiteSpace(apiKey))
            return "AGENT_API_KEY is not set. Add it to .env (local) or Azure Key Vault (production).";
        return null;
    }

    private static IChatClient CreateProviderClient(AgentOptions options, string apiKey) =>
        options.Provider == "ollama"
            ? new OllamaApiClient(new Uri(options.Endpoint), options.Model)
            : new OpenAIClient(new ApiKeyCredential(apiKey), new OpenAIClientOptions { Endpoint = new Uri(options.Endpoint) })
                .GetChatClient(options.Model)
                .AsIChatClient();
}
