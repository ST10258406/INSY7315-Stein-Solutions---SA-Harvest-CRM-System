namespace CRM.Application.Modules.Interactions.Dtos;

/// <summary>Request body for POST /api/v1/donors/{id}/interactions.</summary>
public class LogInteractionRequest
{
    public string InteractionType { get; set; } = string.Empty;
    public string? Subject { get; set; }
    public string Body { get; set; } = string.Empty;
    public DateTime? FollowUpDate { get; set; }
}
