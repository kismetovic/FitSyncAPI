namespace FITSync.Infrastructure.Services.Interfaces;

public interface IEmailNotificationService
{
    Task SendWelcomeEmailAsync(string toEmail, string userName, CancellationToken cancellationToken = default);
    Task SendReservationConfirmationAsync(string toEmail, string userName, DateTime reservationDate, string trainingName, CancellationToken cancellationToken = default);
    Task SendPaymentReminderAsync(string toEmail, string userName, string reservationDetails, CancellationToken cancellationToken = default);
    Task SendPaymentReminderToUserAsync(int userId, CancellationToken cancellationToken = default);
    Task SendPasswordResetEmailAsync(string toEmail, string resetLink, CancellationToken cancellationToken = default);
    Task EnqueueEmailAsync(string to, string subject, string body, bool isHtml = true, CancellationToken cancellationToken = default);
}
