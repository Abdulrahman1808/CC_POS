using System.Net;
using System.Text.Json;
using SuperAdminDashboard.Domain.Exceptions;

namespace SuperAdminDashboard.API.Middleware;

/// <summary>
/// Global exception handling middleware
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, errorCode, message, errors) = exception switch
        {
            ValidationException ve => (
                HttpStatusCode.BadRequest,
                ve.Code,
                ve.Message,
                ve.Errors
            ),
            UnauthorizedException ue => (
                HttpStatusCode.Unauthorized,
                ue.Code,
                ue.Message,
                (IDictionary<string, string[]>?)null
            ),
            ForbiddenException fe => (
                HttpStatusCode.Forbidden,
                fe.Code,
                fe.Message,
                (IDictionary<string, string[]>?)null
            ),
            NotFoundException nf => (
                HttpStatusCode.NotFound,
                nf.Code,
                nf.Message,
                (IDictionary<string, string[]>?)null
            ),
            ConflictException ce => (
                HttpStatusCode.Conflict,
                ce.Code,
                ce.Message,
                (IDictionary<string, string[]>?)null
            ),
            AppException ae => (
                (HttpStatusCode)ae.StatusCode,
                ae.Code,
                ae.Message,
                (IDictionary<string, string[]>?)null
            ),
            _ => (
                HttpStatusCode.InternalServerError,
                "INTERNAL_ERROR",
                _env.IsDevelopment() ? exception.Message : "An unexpected error occurred",
                (IDictionary<string, string[]>?)null
            )
        };

        // Log the exception
        if (statusCode == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);
        }
        else
        {
            _logger.LogWarning("Request failed: {ErrorCode} - {Message}", errorCode, message);
        }

        // Build response
        var response = new
        {
            success = false,
            error = new
            {
                code = errorCode,
                message,
                errors,
                traceId = context.TraceIdentifier,
                timestamp = DateTime.UtcNow
            }
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var options = new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
        };
        
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
    }
}
