using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace PixDynamicGallery.Api.Auth;

/// <summary>
/// Gate for admin-only endpoints (event list/toggle, bulk photo delete). Compares the
/// <c>X-Admin-Password</c> request header against <see cref="AdminOptions.Password"/>.
///
/// Deliberately NOT real security — a single shared password is UI-level deterrence for a
/// solo-operator tool, not protection against a determined attacker. If
/// <see cref="AdminOptions.Password"/> is unset/empty (the default — local dev/docker-compose
/// needs zero setup), the check is skipped entirely: every admin endpoint behaves exactly like
/// today's fully-open ones.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class AdminAuthAttribute : Attribute, IAsyncActionFilter
{
    private const string HeaderName = "X-Admin-Password";

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var options = context.HttpContext.RequestServices.GetRequiredService<IOptions<AdminOptions>>().Value;

        if (!string.IsNullOrEmpty(options.Password))
        {
            var provided = context.HttpContext.Request.Headers[HeaderName].ToString();

            if (!string.Equals(provided, options.Password, StringComparison.Ordinal))
            {
                context.Result = new ObjectResult(new ProblemDetails
                {
                    Title = "Missing or incorrect admin password.",
                    Status = StatusCodes.Status401Unauthorized,
                })
                {
                    StatusCode = StatusCodes.Status401Unauthorized,
                };
                return;
            }
        }

        await next();
    }
}
