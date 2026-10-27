namespace CRM.Application.Modules.Agent;

/// <summary>
/// In-app CRM agent settings, bound from the "Agent" configuration section
/// (docs/agent-spec.md §9). The provider API key is read separately from AGENT_API_KEY
/// and never lives here.
/// </summary>
public class AgentOptions
{
    public const string SectionName = "Agent";

    public bool Enabled { get; set; }

    /// <summary>"ollama" or "openai-compatible" (Groq, GitHub Models, Gemini's OpenAI endpoint, OpenAI).</summary>
    public string Provider { get; set; } = "ollama";

    public string Endpoint { get; set; } = "http://localhost:11434";

    /// <summary>Must support tool calling.</summary>
    public string Model { get; set; } = "qwen2.5:7b";

    public int TimeoutSeconds { get; set; } = 60;

    public int MaxToolIterations { get; set; } = 5;

    public int MaxHistoryMessages { get; set; } = 20;

    /// <summary>Limit for the user's own messages.</summary>
    public int MaxMessageLength { get; set; } = 2000;

    /// <summary>
    /// Limit for earlier assistant replies sent back as history. Looser than MaxMessageLength
    /// because the model's replies aren't capped; the client trims longer ones before sending.
    /// </summary>
    public int MaxAssistantMessageLength { get; set; } = 8000;
}
