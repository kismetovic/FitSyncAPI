using AutoMapper;
using FITSync.Contracts.Notifications;
using FITSync.Domain.Definitions;
using FITSync.Domain.Entities;
using FITSync.Infrastructure.Notifications;
using FITSync.Infrastructure.Repositories.Interfaces;
using FITSync.Infrastructure.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace FITSync.Infrastructure.Services;

/// <inheritdoc />
public class NotificationDispatcher : INotificationDispatcher
{
    private readonly INotificationRepository _notificationRepository;
    private readonly IUserRepository _userRepository;
    private readonly INotificationPublisher _publisher;
    private readonly IRabbitMQProducer _producer;
    private readonly IMapper _mapper;
    private readonly ILogger<NotificationDispatcher> _logger;

    public NotificationDispatcher(
        INotificationRepository notificationRepository,
        IUserRepository userRepository,
        INotificationPublisher publisher,
        IRabbitMQProducer producer,
        IMapper mapper,
        ILogger<NotificationDispatcher> logger)
    {
        _notificationRepository = notificationRepository;
        _userRepository = userRepository;
        _publisher = publisher;
        _producer = producer;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task DispatchAsync(
        int userId,
        NotificationTemplates.Message message,
        string? email,
        bool sendEmail = true,
        CancellationToken cancellationToken = default)
    {
        var response = await PersistAndPushAsync(userId, message, cancellationToken);

        if (!sendEmail || string.IsNullOrWhiteSpace(email))
            return;

        // The API only publishes. FITSync.Worker consumes the queue and does the sending.
        TryEnqueueEmail(email, message, cancellationToken);

        _ = response;
    }

    public Task DispatchInAppAsync(int userId, NotificationTemplates.Message message, CancellationToken cancellationToken = default)
        => PersistAndPushAsync(userId, message, cancellationToken);

    public async Task DispatchToAdministratorsAsync(
        NotificationTemplates.Message message,
        bool sendEmail = false,
        CancellationToken cancellationToken = default)
    {
        var (admins, _) = await _userRepository.SearchAsync(
            null, RoleDefinition.Administrator, true, 0, 100, cancellationToken);

        foreach (var admin in admins)
        {
            var response = await PersistAndPushAsync(admin.Id, message, cancellationToken);
            await _publisher.PublishToAdministratorsAsync(response, cancellationToken);

            if (sendEmail && !string.IsNullOrWhiteSpace(admin.Email))
                TryEnqueueEmail(admin.Email, message, cancellationToken);
        }
    }

    public Task DispatchToReservationOwnerAsync(
        Reservation reservation,
        NotificationTemplates.Message message,
        bool sendEmail = true,
        CancellationToken cancellationToken = default)
        => DispatchAsync(reservation.UserId, message, reservation.User?.Email, sendEmail, cancellationToken);

    private async Task<NotificationResponse> PersistAndPushAsync(
        int userId,
        NotificationTemplates.Message message,
        CancellationToken cancellationToken)
    {
        var entity = await _notificationRepository.InsertAsync(new Notification
        {
            UserId = userId,
            Title = message.Title,
            Message = message.Body,
            IsRead = false
        });

        var response = _mapper.Map<NotificationResponse>(entity);

        // A push failure must never roll back a business operation that already succeeded.
        try
        {
            await _publisher.PublishToUserAsync(userId, response, cancellationToken);
            var unread = await _notificationRepository.GetUnreadByUserIdAsync(userId, cancellationToken);
            await _publisher.PublishUnreadCountAsync(userId, unread.Count, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Real-time push failed for user {UserId}; the notification is still stored.", userId);
        }

        return response;
    }

    private void TryEnqueueEmail(string email, NotificationTemplates.Message message, CancellationToken cancellationToken)
    {
        try
        {
            _producer.PublishToEmailQueueAsync(new Messaging.EmailMessage
            {
                To = email,
                Subject = message.EmailSubject,
                Body = message.EmailHtml,
                IsHtml = true
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not enqueue email to {Email}; the in-app notification was still created.", email);
        }
    }
}
