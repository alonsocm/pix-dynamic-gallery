using MediatR;
using Microsoft.Extensions.Logging;

namespace PixDynamicGallery.Application.Common.Behaviours;

/// <summary>
/// Logs any exception that escapes a handler, tagged with the request type and its serialized
/// payload, before letting it bubble up to the API's global exception-handling middleware.
/// </summary>
public class UnhandledExceptionBehaviour<TRequest, TResponse>(ILogger<UnhandledExceptionBehaviour<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        try
        {
            return await next();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception for request {RequestName}: {@Request}", typeof(TRequest).Name, request);
            throw;
        }
    }
}
