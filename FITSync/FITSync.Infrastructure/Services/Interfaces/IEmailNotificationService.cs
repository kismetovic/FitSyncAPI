namespace FITSync.Infrastructure.Services.Interfaces;

/// <summary>
/// Thin publisher over RabbitMQ. It only enqueues; FITSync.Worker is what actually
/// sends mail. Business messages tied to a user go through INotificationDispatcher
/// instead, so the in-app notification and the email always stay in step.
/// </summary>
public interface IEmailNotificationService
{
    Task SendWelcomeEmailAsync(int userId, string toEmail, string userName, CancellationToken cancellationToken = default);
    Task SendPaymentReminderToUserAsync(int userId, CancellationToken cancellationToken = default);
    Task SendPasswordResetEmailAsync(string toEmail, string resetLink, CancellationToken cancellationToken = default);
    Task EnqueueEmailAsync(string to, string subject, string body, bool isHtml = true, CancellationToken cancellationToken = default);
}
