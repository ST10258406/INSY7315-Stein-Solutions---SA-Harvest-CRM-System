using FluentValidation.Results;

namespace CRM.Application.Common.Exceptions;

/// <summary>
/// Thrown by ValidationBehaviour when one or more FluentValidation validators
/// fail for an incoming request. Caught by ExceptionHandlingMiddleware -> 400.
/// </summary>
public class ValidationException : Exception
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException()
        : base("One or more validation failures have occurred.")
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(IEnumerable<ValidationFailure> failures) : this()
    {
        Errors = failures
            .GroupBy(f => f.PropertyName, f => f.ErrorMessage)
            .ToDictionary(g => g.Key, g => g.ToArray());
    }
}
