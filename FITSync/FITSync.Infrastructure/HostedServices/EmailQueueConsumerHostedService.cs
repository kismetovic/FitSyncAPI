using System.Text;
using System.Text.Json;
using FITSync.Infrastructure.Configuration;
using FITSync.Infrastructure.Messaging;
using FITSync.Infrastructure.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace FITSync.Infrastructure.HostedServices;

public class EmailQueueConsumerHostedService : BackgroundService
{
    private readonly RabbitMQSettings _settings;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EmailQueueConsumerHostedService> _logger;
    private IConnection? _connection;
    private IModel? _channel;

    public EmailQueueConsumerHostedService(
        IOptions<RabbitMQSettings> options,
        IServiceProvider serviceProvider,
        ILogger<EmailQueueConsumerHostedService> logger)
    {
        _settings = options.Value;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var maxRetries = 15;
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            if (stoppingToken.IsCancellationRequested) return;
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = _settings.HostName,
                    Port = _settings.Port,
                    UserName = _settings.UserName,
                    Password = _settings.Password
                };
                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();
                _channel.QueueDeclare(_settings.EmailQueueName, durable: true, exclusive: false, autoDelete: false);

                var consumer = new EventingBasicConsumer(_channel);
                consumer.Received += async (_, ea) =>
                {
                    try
                    {
                        var body = ea.Body.ToArray();
                        var json = Encoding.UTF8.GetString(body);
                        var message = JsonSerializer.Deserialize<EmailMessage>(json);
                        if (message != null)
                        {
                            using var scope = _serviceProvider.CreateScope();
                            var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
                            await emailSender.SendAsync(message.To, message.Subject, message.Body, message.IsHtml, stoppingToken);
                            _channel.BasicAck(ea.DeliveryTag, false);
                        }
                        else
                        {
                            _channel.BasicNack(ea.DeliveryTag, false, false);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to process email message.");
                        _channel?.BasicNack(ea.DeliveryTag, false, true);
                    }
                };
                _channel.BasicConsume(_settings.EmailQueueName, autoAck: false, consumer);
                _logger.LogInformation("Email queue consumer connected to RabbitMQ.");
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("RabbitMQ not ready (attempt {Attempt}/{Max}): {Message}", attempt, maxRetries, ex.Message);
                if (attempt < maxRetries)
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                else
                    _logger.LogError("Could not connect to RabbitMQ after {Max} attempts. Email notifications are disabled.", maxRetries);
            }
        }
    }

    public override void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();
        _connection?.Close();
        _connection?.Dispose();
        base.Dispose();
    }
}
