using FITSync.Contracts.Notifications;

namespace FITSync.Infrastructure.Services.Interfaces;

/// <summary>
/// Real-time push channel. Implemented over SignalR in the WebAPI layer; Infrastructure
/// only depends on this abstraction so services never reference a hub type.
/// </summary>
public interface INotificationPublisher
{
    Task PublishToUserAsync(int userId, NotificationResponse notification, CancellationToken cancellationToken = default);
    Task PublishUnreadCountAsync(int userId, int unreadCount, CancellationToken cancellationToken = default);
    Task PublishToAdministratorsAsync(NotificationResponse notification, CancellationToken cancellationToken = default);
}
