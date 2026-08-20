using Serilog;
using Serilog.Events;

namespace WikiScrapper.Logging;

/// <summary>
/// Console-friendly HTTP request logging: a timestamped separator, then a
/// timestamped completion line, with status-based log levels for color.
/// </summary>
public static class RequestLoggingExtensions
{
    /// <summary>
    /// Writes a separator at the start of each meaningful request, then Serilog's
    /// request-completion line (with its own timestamp) when the request ends.
    /// </summary>
    public static WebApplication UseTimestampedRequestLogging(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            if (!IsNoisy(context.Request.Path))
            {
                Log.Information("======== {RequestMethod} {RequestPath} ========",
                    context.Request.Method,
                    context.Request.Path);
            }

            await next();
        });

        app.UseSerilogRequestLogging(options =>
        {
            options.IncludeQueryInRequestPath = true;
            options.MessageTemplate =
                "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0} ms";
            options.GetLevel = (httpContext, _, exception) =>
            {
                if (IsNoisy(httpContext.Request.Path) && exception is null && httpContext.Response.StatusCode < 400)
                {
                    return LogEventLevel.Verbose;
                }

                if (exception is not null || httpContext.Response.StatusCode >= 500)
                {
                    return LogEventLevel.Error;
                }

                if (httpContext.Response.StatusCode >= 400)
                {
                    return LogEventLevel.Warning;
                }

                return LogEventLevel.Information;
            };
        });

        return app;
    }

    private static bool IsNoisy(PathString path) =>
        path.StartsWithSegments("/api/sync/status")
        || path.StartsWithSegments("/swagger")
        || path.StartsWithSegments("/css")
        || path.StartsWithSegments("/js")
        || path.StartsWithSegments("/lib")
        || path.StartsWithSegments("/favicon.ico");
}
