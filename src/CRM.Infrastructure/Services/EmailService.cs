namespace CRM.Infrastructure.Services;

using CRM.Application.Common.Interfaces;

public class EmailService : IEmailService
{
    public Task SendAsync(string to, string subject, string htmlBody)
    {
        // TODO: SendGrid implementation later
        return Task.CompletedTask;
    }
}
