using FluentValidation.Results;

namespace PixDynamicGallery.Application.Common.Exceptions;

/// <summary>
/// Aggregates one or more <see cref="FluentValidation"/> failures. Raised by
/// <see cref="Behaviours.ValidationBehaviour{TRequest,TResponse}"/> and mapped to HTTP 400 with a
/// field-level error dictionary by the API's exception-handling middleware.
/// </summary>
public class ValidationException : Exception
{
    public ValidationException()
        : base("One or more validation failures occurred.")
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(IEnumerable<ValidationFailure> failures) : this()
    {
        Errors = failures
            .GroupBy(f => f.PropertyName, f => f.ErrorMessage)
            .ToDictionary(g => g.Key, g => g.ToArray());
    }

    public IDictionary<string, string[]> Errors { get; }
}
