using FITSync.Infrastructure.Messaging;
using FITSync.Infrastructure.Notifications;
using FITSync.Infrastructure.Repositories.Interfaces;
using FITSync.Infrastructure.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace FITSync.Infrastructure.Services;

/// <summary>
/// Publishes email work onto RabbitMQ. Nothing here sends mail: FITSync.Worker consumes
/// the queue in its own container and does the SMTP call.
/// </summary>
public class EmailNotificationService : IEmailNotificationService
{
    private readonly IRabbitMQProducer _producer;
    private readonly IReservationRepository _reservationRepository;
    private readonly IUserRepository _userRepository;
    private readonly INotificationDispatcher _dispatcher;
    private readonly ILogger<EmailNotificationService> _logger;

    public EmailNotificationService(
        IRabbitMQProducer producer,
        IReservationRepository reservationRepository,
        IUserRepository userRepository,
        INotificationDispatcher dispatcher,
        ILogger<EmailNotificationService> logger)
    {
        _producer = producer;
        _reservationRepository = reservationRepository;
        _userRepository = userRepository;
        _dispatcher = dispatcher;
        _logger = logger;
    }

    public Task SendWelcomeEmailAsync(int userId, string toEmail, string userName, CancellationToken cancellationToken = default)
        => _dispatcher.DispatchAsync(userId, NotificationTemplates.Welcome(userName), toEmail, true, cancellationToken);

    /// <summary>
    /// Reminder for everything the user still owes. The unpaid set now comes from one
    /// query rather than a payment lookup per reservation.
    /// </summary>
    public async Task SendPaymentReminderToUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user?.Email == null) return;

        var unpaid = await _reservationRepository.GetUnpaidByUserIdAsync(userId, cancellationToken);
        if (unpaid.Count == 0) return;

        var details = unpaid
            .Select(r => $"#{r.Id} - {r.Training?.Name ?? "Trening"} ({r.ReservationDate:dd.MM.yyyy HH:mm}), {r.TotalPrice:0.00} BAM")
            .ToList();

        await _dispatcher.DispatchAsync(
            userId, NotificationTemplates.PaymentReminder(details), user.Email, true, cancellationToken);
    }

    public Task SendPasswordResetEmailAsync(string toEmail, string resetLink, CancellationToken cancellationToken = default)
    {
        var body = $@"
            <h2>Resetovanje lozinke</h2>
            <p>Kliknite na link ispod da postavite novu lozinku:</p>
            <p><a href=""{resetLink}"">{resetLink}</a></p>
            <p>Ako niste Vi zatražili resetovanje, slobodno ignorišite ovu poruku.</p>
            <p>Srdačan pozdrav,<br/>FitSync tim</p>";

        return EnqueueEmailAsync(toEmail, "Resetovanje lozinke - FitSync", body, true, cancellationToken);
    }

    public Task EnqueueEmailAsync(string to, string subject, string body, bool isHtml = true, CancellationToken cancellationToken = default)
    {
        try
        {
            return _producer.PublishToEmailQueueAsync(
                new EmailMessage { To = to, Subject = subject, Body = body, IsHtml = isHtml }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not enqueue an email to {Email}.", to);
            return Task.CompletedTask;
        }
    }
}
