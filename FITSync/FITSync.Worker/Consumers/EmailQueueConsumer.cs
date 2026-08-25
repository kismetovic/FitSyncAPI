using System.Text;
using System.Text.Json;
using FITSync.Infrastructure.Configuration;
using FITSync.Infrastructure.Messaging;
using FITSync.Infrastructure.Services.Interfaces;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace FITSync.Worker.Consumers;

/// <summary>
/// Consumes the email queue and sends the messages. This runs in its own container
/// (fitsync-worker), completely separate from the API process: the API only publishes
/// onto RabbitMQ and never sends an email itself.
/// </summary>
public class EmailQueueConsumer : BackgroundService
{
    private readonly RabbitMQSettings _settings;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EmailQueueConsumer> _logger;

    private IConnection? _connection;
    private IModel? _channel;

    public EmailQueueConsumer(
        IOptions<RabbitMQSettings> options,
        IServiceProvider serviceProvider,
        ILogger<EmailQueueConsumer> logger)
    {
        _settings = options.Value;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ConnectWithRetryAsync(stoppingToken);

        // Keep the service alive; the consumer callback drives the actual work.
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_connection is not { IsOpen: true })
            {
                _logger.LogWarning("RabbitMQ connection dropped. Reconnecting...");
                await ConnectWithRetryAsync(stoppingToken);
            }
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }

    private async Task ConnectWithRetryAsync(CancellationToken stoppingToken)
    {
        const int maxRetries = 30;

        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            if (stoppingToken.IsCancellationRequested) return;

            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = _settings.HostName,
                    Port = _settings.Port,
                    UserName = _settings.UserName,
                    Password = _settings.Password,
                    DispatchConsumersAsync = true,
                    AutomaticRecoveryEnabled = true
                };

                _connection = factory.CreateConnection("fitsync-worker");
                _channel = _connection.CreateModel();
                _channel.QueueDeclare(_settings.EmailQueueName, durable: true, exclusive: false, autoDelete: false);

                // One message in flight at a time keeps SMTP usage predictable and makes
                // redelivery behaviour easy to reason about.
                _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

                var consumer = new AsyncEventingBasicConsumer(_channel);
                consumer.Received += OnMessageReceivedAsync;

                _channel.BasicConsume(_settings.EmailQueueName, autoAck: false, consumer);

                _logger.LogInformation(
                    "Worker connected to RabbitMQ at {Host}:{Port} and is consuming queue '{Queue}'.",
                    _settings.HostName, _settings.Port, _settings.EmailQueueName);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("RabbitMQ not ready (attempt {Attempt}/{Max}): {Message}", attempt, maxRetries, ex.Message);
                if (attempt == maxRetries)
                {
                    _logger.LogError("Could not connect to RabbitMQ after {Max} attempts. Email delivery is paused.", maxRetries);
                    return;
                }
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs args)
    {
        var channel = _channel;
        if (channel == null) return;

        EmailMessage? message = null;
        try
        {
            var json = Encoding.UTF8.GetString(args.Body.ToArray());
            message = JsonSerializer.Deserialize<EmailMessage>(json);
        }
        catch (Exception ex)
        {
            // A message we cannot even parse will never succeed: drop it rather than
            // requeueing it forever.
            _logger.LogError(ex, "Discarding an email message that could not be deserialized.");
            channel.BasicNack(args.DeliveryTag, multiple: false, requeue: false);
            return;
        }

        if (message == null || string.IsNullOrWhiteSpace(message.To))
        {
            _logger.LogWarning("Discarding an email message with no recipient.");
            channel.BasicNack(args.DeliveryTag, multiple: false, requeue: false);
            return;
        }

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
            await emailSender.SendAsync(message.To, message.Subject, message.Body, message.IsHtml);

            channel.BasicAck(args.DeliveryTag, multiple: false);
            _logger.LogInformation("Email sent to {Recipient}: {Subject}", message.To, message.Subject);
        }
        catch (Exception ex)
        {
            // Transient SMTP problems are worth one retry; a message already redelivered
            // once is dropped so a permanently broken address cannot block the queue.
            var alreadyRetried = args.Redelivered;
            _logger.LogError(ex, "Failed to send email to {Recipient}. Requeue: {Requeue}", message.To, !alreadyRetried);
            channel.BasicNack(args.DeliveryTag, multiple: false, requeue: !alreadyRetried);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Worker is shutting down.");
        await base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        try
        {
            _channel?.Close();
            _channel?.Dispose();
            _connection?.Close();
            _connection?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Ignoring an error while closing the RabbitMQ connection.");
        }

        base.Dispose();
        GC.SuppressFinalize(this);
    }
}
