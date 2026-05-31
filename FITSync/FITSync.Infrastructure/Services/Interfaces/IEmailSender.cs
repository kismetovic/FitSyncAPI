namespace FITSync.Infrastructure.Services.Interfaces;

public interface IEmailSender
{
    Task SendAsync(string to, string subject, string body, bool isHtml = true, CancellationToken cancellationToken = default);
}
