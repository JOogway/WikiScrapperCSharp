using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;

namespace WikiScrapper.Middleware;

/// <summary>
/// Catches unhandled exceptions, logs them via Serilog, and returns
/// an RFC 7807 problem-details payload for API requests or re-throws
/// for MVC requests (handled by the standard error page).
/// </summary>
public class GlobalExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionHandlingMiddleware> logger)
{
    /// <summary>Invokes the middleware.</summary>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception for {Method} {Path}",
                context.Request.Method, context.Request.Path);

            if (!context.Request.Path.StartsWithSegments("/api") || context.Response.HasStarted)
            {
                throw; // Let the MVC error page handle non-API requests.
            }

            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred.",
                Detail = "The error has been logged. Please try again or check the logs.",
                Instance = context.Request.Path,
            };

            context.Response.StatusCode = problem.Status.Value;
            context.Response.ContentType = MediaTypeNames.Application.ProblemJson;
            await context.Response.WriteAsJsonAsync(problem);
        }
    }
}
