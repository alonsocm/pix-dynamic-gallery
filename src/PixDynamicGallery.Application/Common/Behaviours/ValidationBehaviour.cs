using FluentValidation;
using MediatR;
using ValidationException = PixDynamicGallery.Application.Common.Exceptions.ValidationException;

namespace PixDynamicGallery.Application.Common.Behaviours;

/// <summary>
/// MediatR pipeline behaviour that runs every registered <see cref="IValidator{T}"/> for the
/// incoming request before the handler executes, short-circuiting with a
/// <see cref="ValidationException"/> when any fail. Keeps handlers free of manual validation code.
/// </summary>
public class ValidationBehaviour<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        var failures = (await Task.WhenAll(
                validators.Select(v => v.ValidateAsync(context, cancellationToken))))
            .SelectMany(result => result.Errors)
            .Where(failure => failure is not null)
            .ToList();

        if (failures.Count != 0)
        {
            throw new ValidationException(failures);
        }

        return await next();
    }
}
