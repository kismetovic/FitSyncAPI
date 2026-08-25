using FITSync.Contracts.Notifications;
using FITSync.Infrastructure.Services.Interfaces;

namespace FITSync.Infrastructure.Notifications;

/// <summary>
/// Fallback used by hosts that have no SignalR hub (the worker process, tests).
/// Keeps the dispatcher usable without every host having to wire up a hub.
/// </summary>
public class NoOpNotificationPublisher : INotificationPublisher
{
    public Task PublishToUserAsync(int userId, NotificationResponse notification, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task PublishUnreadCountAsync(int userId, int unreadCount, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task PublishToAdministratorsAsync(NotificationResponse notification, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
