using System.Diagnostics;

namespace SuperAdminDashboard.API.Middleware;

/// <summary>
/// Request logging middleware with timing
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestId = context.TraceIdentifier;
        
        // Add request ID to response headers
        context.Response.OnStarting(() =>
        {
            context.Response.Headers["X-Request-Id"] = requestId;
            return Task.CompletedTask;
        });

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            
            var logLevel = context.Response.StatusCode >= 500 
                ? LogLevel.Error 
                : context.Response.StatusCode >= 400 
                    ? LogLevel.Warning 
                    : LogLevel.Information;

            _logger.Log(
                logLevel,
                "{Method} {Path} responded {StatusCode} in {ElapsedMs}ms [RequestId: {RequestId}]",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds,
                requestId);
        }
    }
}
