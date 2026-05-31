using System.Text;
using System.Text.Json;
using FITSync.Infrastructure.Configuration;
using FITSync.Infrastructure.Messaging;
using FITSync.Infrastructure.Services.Interfaces;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace FITSync.Infrastructure.Services.ExternalServices;

public class RabbitMQProducer : IRabbitMQProducer, IDisposable
{
    private readonly RabbitMQSettings _settings;
    private IConnection? _connection;
    private IModel? _channel;
    private readonly object _lock = new();

    public RabbitMQProducer(IOptions<RabbitMQSettings> options)
    {
        _settings = options.Value;
    }

    private IModel GetChannel()
    {
        if (_channel != null && _channel.IsOpen)
            return _channel;
        lock (_lock)
        {
            if (_channel != null && _channel.IsOpen)
                return _channel;
            _connection?.Close();
            _connection?.Dispose();
            _channel?.Close();
            _channel?.Dispose();
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
            return _channel;
        }
    }

    public void PublishToEmailQueue(EmailMessage message)
    {
        var channel = GetChannel();
        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);
        channel.BasicPublish("", _settings.EmailQueueName, body: body);
    }

    public Task PublishToEmailQueueAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        PublishToEmailQueue(message);
        return Task.CompletedTask;
    }

    public void Dispose() => GC.SuppressFinalize(this);
}
