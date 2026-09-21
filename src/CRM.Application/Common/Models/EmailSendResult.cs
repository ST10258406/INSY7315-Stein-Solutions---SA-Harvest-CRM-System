namespace CRM.Application.Common.Models;

using CRM.Domain.Enums;

/// <summary>
/// What <see cref="Interfaces.IEmailService.SendAsync"/> did. The EmailLog row is
/// always written (success or failure) before this is returned — callers that need
/// to react differently to a failed send (e.g. SendDonorEmailCommandHandler, which
/// must not write an InteractionLog for a failed send) check <see cref="Status"/>.
/// </summary>
public record EmailSendResult(Guid EmailLogId, EmailStatus Status);
