using MediatR;
using SuperAdminDashboard.Domain.Events;
using SuperAdminDashboard.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace SuperAdminDashboard.Application.EventHandlers;

/// <summary>
/// Handles CustomerLoggedInEvent - broadcasts to admin dashboard
/// </summary>
public sealed class CustomerLoggedInEventHandler 
    : INotificationHandler<CustomerLoggedInEvent>
{
    private readonly IRealTimeNotifier _notifier;
    private readonly ILogger<CustomerLoggedInEventHandler> _logger;

    public CustomerLoggedInEventHandler(
        IRealTimeNotifier notifier,
        ILogger<CustomerLoggedInEventHandler> logger)
    {
        _notifier = notifier;
        _logger = logger;
    }

    public async Task Handle(
        CustomerLoggedInEvent notification, 
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Customer {CustomerId} logged in from {IpAddress}",
            notification.CustomerId,
            notification.IpAddress);

        await _notifier.NotifyAllAsync(
            "CustomerLoggedIn",
            new
            {
                notification.CustomerId,
                notification.CustomerEmail,
                notification.IpAddress,
                notification.OccurredAt,
                notification.TenantId
            },
            cancellationToken);
    }
}

/// <summary>
/// Handles CustomerPurchasedEvent - broadcasts to admin dashboard
/// </summary>
public sealed class CustomerPurchasedEventHandler 
    : INotificationHandler<CustomerPurchasedEvent>
{
    private readonly IRealTimeNotifier _notifier;
    private readonly ILogger<CustomerPurchasedEventHandler> _logger;

    public CustomerPurchasedEventHandler(
        IRealTimeNotifier notifier,
        ILogger<CustomerPurchasedEventHandler> logger)
    {
        _notifier = notifier;
        _logger = logger;
    }

    public async Task Handle(
        CustomerPurchasedEvent notification, 
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Customer {CustomerId} purchased order {OrderId} for {Amount} {Currency}",
            notification.CustomerId,
            notification.OrderId,
            notification.Amount,
            notification.Currency);

        // Broadcast to specific tenant's dashboard
        if (notification.TenantId.HasValue)
        {
            await _notifier.NotifyTenantAsync(
                notification.TenantId.Value,
                "CustomerPurchased",
                new
                {
                    notification.CustomerId,
                    notification.OrderId,
                    notification.Amount,
                    notification.Currency,
                    notification.OccurredAt
                },
                cancellationToken);
        }

        // Also broadcast to super admin dashboard
        await _notifier.NotifyAllAsync(
            "CustomerPurchased",
            new
            {
                notification.CustomerId,
                notification.OrderId,
                notification.Amount,
                notification.Currency,
                notification.TenantId,
                notification.OccurredAt
            },
            cancellationToken);
    }
}

/// <summary>
/// Handles SystemErrorEvent - broadcasts alerts to admin dashboard
/// </summary>
public sealed class SystemErrorEventHandler 
    : INotificationHandler<SystemErrorEvent>
{
    private readonly IRealTimeNotifier _notifier;
    private readonly ILogger<SystemErrorEventHandler> _logger;

    public SystemErrorEventHandler(
        IRealTimeNotifier notifier,
        ILogger<SystemErrorEventHandler> logger)
    {
        _notifier = notifier;
        _logger = logger;
    }

    public async Task Handle(
        SystemErrorEvent notification, 
        CancellationToken cancellationToken)
    {
        _logger.LogError(
            "System error: {ErrorCode} - {Message} from {Source}",
            notification.ErrorCode,
            notification.Message,
            notification.Source);

        await _notifier.NotifyAllAsync(
            "SystemError",
            new
            {
                notification.ErrorCode,
                notification.Message,
                notification.Source,
                notification.OccurredAt,
                notification.TenantId
            },
            cancellationToken);
    }
}

/// <summary>
/// Handles TenantStatusChangedEvent - broadcasts to admin dashboard
/// </summary>
public sealed class TenantStatusChangedEventHandler 
    : INotificationHandler<TenantStatusChangedEvent>
{
    private readonly IRealTimeNotifier _notifier;
    private readonly ILogger<TenantStatusChangedEventHandler> _logger;

    public TenantStatusChangedEventHandler(
        IRealTimeNotifier notifier,
        ILogger<TenantStatusChangedEventHandler> logger)
    {
        _notifier = notifier;
        _logger = logger;
    }

    public async Task Handle(
        TenantStatusChangedEvent notification, 
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Tenant '{TenantName}' status changed from {OldStatus} to {NewStatus}",
            notification.TenantName,
            notification.OldStatus,
            notification.NewStatus);

        await _notifier.NotifyAllAsync(
            "TenantStatusChanged",
            new
            {
                notification.TenantId,
                notification.TenantName,
                notification.OldStatus,
                notification.NewStatus,
                notification.OccurredAt
            },
            cancellationToken);
    }
}
