using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using PixDynamicGallery.Application.Common.Behaviours;

namespace PixDynamicGallery.Application;

/// <summary>
/// Composition root extension for the Application layer. Called once from the API's
/// <c>Program.cs</c> — keeps all MediatR/FluentValidation wiring colocated with the code it wires up.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddValidatorsFromAssembly(assembly);

        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(assembly);

            // Order matters: unhandled-exception logging wraps validation, so validation
            // failures (expected, client-caused) are still logged as errors by the outer behaviour
            // only if something further downstream also throws unexpectedly.
            config.AddOpenBehavior(typeof(UnhandledExceptionBehaviour<,>));
            config.AddOpenBehavior(typeof(ValidationBehaviour<,>));
        });

        return services;
    }
}
