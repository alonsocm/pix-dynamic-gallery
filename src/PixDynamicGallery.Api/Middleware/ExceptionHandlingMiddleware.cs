using System.Net;
using Microsoft.AspNetCore.Mvc;
using PixDynamicGallery.Application.Common.Exceptions;
using PixDynamicGallery.Domain.Exceptions;
using ValidationException = PixDynamicGallery.Application.Common.Exceptions.ValidationException;

namespace PixDynamicGallery.Api.Middleware;

/// <summary>
/// Single place that turns exceptions escaping the pipeline into RFC 7807 <see cref="ProblemDetails"/>
/// responses, so controllers/handlers never write try/catch-for-HTTP-status boilerplate — they just
/// throw the appropriate typed exception and this maps it.
/// </summary>
public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, problemDetails) = exception switch
        {
            ValidationException validationException => (
                HttpStatusCode.BadRequest,
                new ValidationProblemDetails(validationException.Errors)
                {
                    Title = "One or more validation errors occurred.",
                    Status = (int)HttpStatusCode.BadRequest,
                }),

            NotFoundException notFoundException => (
                HttpStatusCode.NotFound,
                new ProblemDetails
                {
                    Title = "Resource not found.",
                    Detail = notFoundException.Message,
                    Status = (int)HttpStatusCode.NotFound,
                }),

            DomainException domainException => (
                HttpStatusCode.BadRequest,
                new ProblemDetails
                {
                    Title = "Request violates a business rule.",
                    Detail = domainException.Message,
                    Status = (int)HttpStatusCode.BadRequest,
                }),

            _ => (
                HttpStatusCode.InternalServerError,
                new ProblemDetails
                {
                    Title = "An unexpected error occurred.",
                    Status = (int)HttpStatusCode.InternalServerError,
                }),
        };

        if (statusCode == HttpStatusCode.InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception processing {Method} {Path}", context.Request.Method, context.Request.Path);
        }

        problemDetails.Instance = context.Request.Path;
        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)statusCode;

        // The switch above infers ProblemDetails as the common type across its arms (since
        // ValidationProblemDetails derives from it), which erases the static type even though the
        // runtime object is still a ValidationProblemDetails. WriteAsJsonAsync<T>'s generic overload
        // would serialize based on that erased static type — silently dropping ValidationProblemDetails'
        // own "errors" property. Passing the runtime type explicitly forces System.Text.Json to
        // serialize the object's actual shape instead.
        await context.Response.WriteAsJsonAsync(problemDetails, problemDetails.GetType());
    }
}
