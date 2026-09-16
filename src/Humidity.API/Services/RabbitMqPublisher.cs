using Humidity.Application.Interfaces;
using Humidity.Contracts.Events;
using Microsoft.AspNetCore.Connections;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using IModel = RabbitMQ.Client.IModel;

namespace Humidity.API.Services;


/// <summary>
/// Реализация на официальном клиенте RabbitMQ.Client.
/// Один долгоживущий IConnection и один IModel на приложение (потокобезопасны,
/// за исключением одновременной публикации — на всякий случай используем lock).
/// </summary>
public class RabbitMqPublisher : IRabbitMqPublisher, IDisposable
{
    private readonly RabbitMqOptions _options;
    private readonly ILogger<RabbitMqPublisher> _logger;
    private readonly object _lock = new();
    private IConnection? _connection;
    private IModel? _channel;

    public RabbitMqPublisher(
        IOptions<RabbitMqOptions> options,
        ILogger<RabbitMqPublisher> logger)
    {
        _options = options.Value;
        _logger = logger;
        TryConnect();
    }

    /// <summary>
    /// Устанавливает соединение с RabbitMQ. При ошибке пишет warning,
    /// чтобы приложение продолжало работать (уведомления — не критичный путь).
    /// </summary>
    private void TryConnect()
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _options.Host,
                Port = _options.Port,
                UserName = _options.Username,
                Password = _options.Password,
                VirtualHost = _options.VirtualHost,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
            };

            _connection = factory.CreateConnection("Humidity.API.Publisher");
            _channel = _connection.CreateModel();

            // Основной exchange
            _channel.ExchangeDeclare(
                exchange: RabbitMqTopology.EventsExchange,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false);

            // DLX + DLQ, чтобы consumer мог безопасно отправлять «битые» сообщения.
            _channel.ExchangeDeclare(
                exchange: RabbitMqTopology.DeadLetterExchange,
                type: ExchangeType.Fanout,
                durable: true,
                autoDelete: false);

            _channel.QueueDeclare(
                queue: RabbitMqTopology.DeadLetterQueue,
                durable: true,
                exclusive: false,
                autoDelete: false);

            _channel.QueueBind(
                queue: RabbitMqTopology.DeadLetterQueue,
                exchange: RabbitMqTopology.DeadLetterExchange,
                routingKey: "");

            // Очередь уведомлений о смене (объявляется и на стороне издателя,
            // чтобы consumer мог стартовать после издателя без race condition).
            var queueArgs = new Dictionary<string, object>
            {
                { "x-dead-letter-exchange", RabbitMqTopology.DeadLetterExchange }
            };

            _channel.QueueDeclare(
                queue: RabbitMqTopology.ShiftEndedQueue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: queueArgs);

            _channel.QueueBind(
                queue: RabbitMqTopology.ShiftEndedQueue,
                exchange: RabbitMqTopology.EventsExchange,
                routingKey: RabbitMqTopology.ShiftEndedRoutingKey);

            _logger.LogInformation("RabbitMqPublisher: подключение установлено.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "RabbitMqPublisher: не удалось подключиться к RabbitMQ. " +
                "События не будут публиковаться до следующего перезапуска.");
        }
    }

    public Task PublishAsync<T>(T @event, string routingKey, CancellationToken ct = default)
        where T : class
    {
        if (_channel == null || !_channel.IsOpen)
        {
            _logger.LogWarning("RabbitMqPublisher: канал закрыт, событие {Type} потеряно.",
                typeof(T).Name);
            return Task.CompletedTask;
        }

        var json = JsonSerializer.Serialize(@event);
        var body = Encoding.UTF8.GetBytes(json);

        var props = _channel.CreateBasicProperties();
        props.ContentType = "application/json";
        props.DeliveryMode = 2; // persistent
        props.MessageId = Guid.NewGuid().ToString();

        lock (_lock)
        {
            _channel.BasicPublish(
                exchange: RabbitMqTopology.EventsExchange,
                routingKey: routingKey,
                basicProperties: props,
                body: body);
        }

        _logger.LogInformation(
            "RabbitMqPublisher: опубликовано {Type} с routingKey={RoutingKey}",
            typeof(T).Name, routingKey);

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        try
        {
            _channel?.Close();
            _connection?.Close();
        }
        catch
        {
            // Игнорируем ошибки при закрытии.
        }
    }
}

/// <summary>
/// Настройки RabbitMQ для основного API.
/// </summary>
public class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string Username { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
}