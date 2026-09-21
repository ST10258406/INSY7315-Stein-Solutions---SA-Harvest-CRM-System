namespace CRM.Application.Common.Exceptions;

/// <summary>
/// Thrown by a handler that needs an email send to succeed synchronously — unlike
/// the default fire-and-forget IEmailService callers (password reset, onboarding
/// confirmation), which log a failed send and never throw.
/// </summary>
public class EmailDeliveryException : Exception
{
    public EmailDeliveryException(string message) : base(message) { }
}
