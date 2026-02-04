using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace SuperAdminDashboard.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior for logging and performance monitoring
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse> 
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation("Handling {RequestName}", requestName);

        try
        {
            var response = await next();
            
            stopwatch.Stop();
            
            if (stopwatch.ElapsedMilliseconds > 500)
            {
                _logger.LogWarning(
                    "Long running request: {RequestName} ({ElapsedMs} ms)",
                    requestName,
                    stopwatch.ElapsedMilliseconds);
            }
            else
            {
                _logger.LogInformation(
                    "Handled {RequestName} in {ElapsedMs} ms",
                    requestName,
                    stopwatch.ElapsedMilliseconds);
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error handling {RequestName}: {Message}",
                requestName,
                ex.Message);
            throw;
        }
    }
}
