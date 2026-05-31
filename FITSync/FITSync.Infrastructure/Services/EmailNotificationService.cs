using FITSync.Domain.Entities;
using FITSync.Domain.Enums;
using FITSync.Infrastructure.Messaging;
using FITSync.Infrastructure.Repositories.Interfaces;
using FITSync.Infrastructure.Services.Interfaces;

namespace FITSync.Infrastructure.Services;

public class EmailNotificationService : IEmailNotificationService
{
    private readonly IRabbitMQProducer _producer;
    private readonly IUserRepository _userRepository;
    private readonly IReservationRepository _reservationRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly INotificationRepository _notificationRepository;

    public EmailNotificationService(
        IRabbitMQProducer producer,
        IUserRepository userRepository,
        IReservationRepository reservationRepository,
        IPaymentRepository paymentRepository,
        INotificationRepository notificationRepository)
    {
        _producer = producer;
        _userRepository = userRepository;
        _reservationRepository = reservationRepository;
        _paymentRepository = paymentRepository;
        _notificationRepository = notificationRepository;
    }

    public Task SendWelcomeEmailAsync(string toEmail, string userName, CancellationToken cancellationToken = default)
    {
        var subject = "Welcome to FitSync";
        var body = $@"
            <h2>Welcome, {userName}!</h2>
            <p>Thank you for registering with FitSync. You can now browse trainings and make reservations.</p>
            <p>Best regards,<br/>The FitSync Team</p>";
        EnqueueEmailAsync(toEmail, subject, body, true, cancellationToken);
        return Task.CompletedTask;
    }

    public Task SendReservationConfirmationAsync(string toEmail, string userName, DateTime reservationDate, string trainingName, CancellationToken cancellationToken = default)
    {
        var subject = "Reservation confirmed - FitSync";
        var body = $@"
            <h2>Reservation confirmed</h2>
            <p>Hi {userName},</p>
            <p>Your reservation has been confirmed.</p>
            <ul>
                <li><strong>Training:</strong> {trainingName}</li>
                <li><strong>Date:</strong> {reservationDate:dd.MM.yyyy HH:mm}</li>
            </ul>
            <p>Best regards,<br/>The FitSync Team</p>";
        EnqueueEmailAsync(toEmail, subject, body, true, cancellationToken);
        return Task.CompletedTask;
    }

    public async Task SendPaymentReminderToUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user?.Email == null) return;
        var reservations = await _reservationRepository.GetByUserIdAsync(userId, cancellationToken);
        var unpaid = new List<string>();
        foreach (var r in reservations)
        {
            var payment = await _paymentRepository.GetByReservationIdAsync(r.Id, cancellationToken);
            if (payment == null && r.Status != ReservationStatus.Cancelled)
                unpaid.Add($"Reservation #{r.Id} - {r.Training?.Name ?? "Training"} on {r.ReservationDate:dd.MM.yyyy}");
        }
        if (unpaid.Count > 0)
        {
            await SendPaymentReminderAsync(user.Email, user.Name ?? user.UserName ?? user.Email, string.Join("; ", unpaid), cancellationToken);

            await _notificationRepository.InsertAsync(new Notification
            {
                UserId = userId,
                Title = "Podsjetnik za uplatu",
                Message = $"Imate {unpaid.Count} nepla{(unpaid.Count == 1 ? "ćenu rezervaciju" : "ćene rezervacije")}: {string.Join(", ", unpaid)}",
                IsRead = false
            });
        }
    }

    public Task SendPaymentReminderAsync(string toEmail, string userName, string reservationDetails, CancellationToken cancellationToken = default)
    {
        var subject = "Payment reminder - FitSync";
        var body = $@"
            <h2>Payment reminder</h2>
            <p>Hi {userName},</p>
            <p>This is a reminder that payment is pending for the following reservation:</p>
            <p>{reservationDetails}</p>
            <p>Please complete your payment at your earliest convenience.</p>
            <p>Best regards,<br/>The FitSync Team</p>";
        EnqueueEmailAsync(toEmail, subject, body, true, cancellationToken);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetEmailAsync(string toEmail, string resetLink, CancellationToken cancellationToken = default)
    {
        var subject = "Reset your password - FitSync";
        var body = $@"<h2>Password reset</h2><p>Click the link below to reset your password:</p><p><a href=""{resetLink}"">{resetLink}</a></p><p>If you did not request this, please ignore this email.</p><p>Best regards,<br/>The FitSync Team</p>";
        EnqueueEmailAsync(toEmail, subject, body, true, cancellationToken);
        return Task.CompletedTask;
    }

    public Task EnqueueEmailAsync(string to, string subject, string body, bool isHtml = true, CancellationToken cancellationToken = default)
    {
        _producer.PublishToEmailQueueAsync(new EmailMessage { To = to, Subject = subject, Body = body, IsHtml = isHtml }, cancellationToken);
        return Task.CompletedTask;
    }
}
