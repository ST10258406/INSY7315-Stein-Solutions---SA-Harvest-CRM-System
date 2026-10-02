namespace CRM.Application.Common.Models;

/// <summary>A staff member's display name and login address, used as the sender name and Reply-To on mail they compose.</summary>
public record EmailSenderIdentity(string Name, string Email);
