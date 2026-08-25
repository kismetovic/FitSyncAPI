using FITSync.Contracts.Notifications;
using FITSync.Infrastructure.Services.Interfaces;
using FITSync.WebAPI.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace FITSync.WebAPI.RealTime
{
    /// <summary>
    /// SignalR implementation of the push abstraction the services depend on. Infrastructure
    /// stays free of any SignalR reference; only the API host knows a hub exists.
    /// </summary>
    public class SignalRNotificationPublisher : INotificationPublisher
    {
        /// <summary>Client method names, kept here so the Flutter clients have one contract to match.</summary>
        public const string NotificationReceived = "NotificationReceived";
        public const string UnreadCountChanged = "UnreadCountChanged";

        private readonly IHubContext<NotificationsHub> _hubContext;

        public SignalRNotificationPublisher(IHubContext<NotificationsHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public Task PublishToUserAsync(int userId, NotificationResponse notification, CancellationToken cancellationToken = default)
            => _hubContext.Clients
                .Group(NotificationsHub.UserGroup(userId))
                .SendAsync(NotificationReceived, notification, cancellationToken);

        public Task PublishUnreadCountAsync(int userId, int unreadCount, CancellationToken cancellationToken = default)
            => _hubContext.Clients
                .Group(NotificationsHub.UserGroup(userId))
                .SendAsync(UnreadCountChanged, unreadCount, cancellationToken);

        public Task PublishToAdministratorsAsync(NotificationResponse notification, CancellationToken cancellationToken = default)
            => _hubContext.Clients
                .Group(NotificationsHub.AdministratorsGroup)
                .SendAsync(NotificationReceived, notification, cancellationToken);
    }
}
