using FITSync.Infrastructure.Messaging;

namespace FITSync.Infrastructure.Services.Interfaces;

public interface IRabbitMQProducer
{
    void PublishToEmailQueue(EmailMessage message);
    Task PublishToEmailQueueAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
