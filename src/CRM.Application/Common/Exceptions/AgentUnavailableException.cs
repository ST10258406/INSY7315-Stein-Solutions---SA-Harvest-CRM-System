namespace CRM.Application.Common.Exceptions;

/// <summary>
/// The CRM agent can't answer right now: it is disabled, the LLM provider failed or
/// rate-limited us, or the request timed out. Mapped to 503 AGENT_UNAVAILABLE. The client
/// message is deliberately generic; the provider's detail stays in <see cref="Exception.InnerException"/>
/// for the server log only.
/// </summary>
public class AgentUnavailableException : Exception
{
    public AgentUnavailableException(string reason, Exception? innerException = null)
        : base(reason, innerException) { }
}
