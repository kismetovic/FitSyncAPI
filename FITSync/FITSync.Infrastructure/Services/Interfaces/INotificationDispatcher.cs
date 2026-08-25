using FITSync.Domain.Entities;
using FITSync.Infrastructure.Notifications;

namespace FITSync.Infrastructure.Services.Interfaces;

/// <summary>
/// The single path every user-facing message takes: persist the in-app notification,
/// push it over SignalR, and enqueue the email on RabbitMQ. Email is an addition to the
/// in-app notification, never a replacement for it.
/// </summary>
public interface INotificationDispatcher
{
    Task DispatchAsync(int userId, NotificationTemplates.Message message, string? email, bool sendEmail = true, CancellationToken cancellationToken = default);

    /// <summary>In-app + push only, no email. Used for high-frequency staff notices.</summary>
    Task DispatchInAppAsync(int userId, NotificationTemplates.Message message, CancellationToken cancellationToken = default);

    /// <summary>Notifies every administrator, e.g. when a client requests an out-of-hours slot.</summary>
    Task DispatchToAdministratorsAsync(NotificationTemplates.Message message, bool sendEmail = false, CancellationToken cancellationToken = default);

    Task DispatchToReservationOwnerAsync(Reservation reservation, NotificationTemplates.Message message, bool sendEmail = true, CancellationToken cancellationToken = default);
}
