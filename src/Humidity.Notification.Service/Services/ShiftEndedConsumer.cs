using Humidity.Contracts.Events;
using Humidity.Contracts.Protos;
using Humidity.Contracts.Serialization;
using Humidity.Notification.Service.Data;
using Humidity.Notification.Service.Models;
using Humidity.Notification.Service.Options;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace Humidity.Notification.Service.Services;

/// <summary>
/// BackgroundService, который слушает очередь RabbitMQ
/// </summary>
public class ShiftEndedConsumer : BackgroundService
{
    private readonly ILogger<ShiftEndedConsumer> _logger;
    private readonly RabbitMqOptions _rabbitOptions;
    private readonly RecipientsOptions _recipients;
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeZoneInfo _timeZone;

    private IConnection? _connection;
    private IModel? _channel;

    private volatile bool _stopping;

    // Счётчик активных обработчиков — для graceful shutdown.
    private int _activeHandlers;

    public ShiftEndedConsumer(
        ILogger<ShiftEndedConsumer> logger,
        IOptions<RabbitMqOptions> rabbitOptions,
        IOptions<RecipientsOptions> recipients,
        IConfiguration configuration,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _rabbitOptions = rabbitOptions.Value;
        _recipients = recipients.Value;
        _serviceProvider = serviceProvider;

        var tzId = configuration["TimeZoneId"] ?? "Ekaterinburg Standard Time";
        try
        {
            _timeZone = TimeZoneInfo.FindSystemTimeZoneById(tzId);
        }
        catch
        {
            _logger.LogWarning("Таймзона {Tz} не найдена, используется UTC.", tzId);
            _timeZone = TimeZoneInfo.Utc;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await WaitForRabbitMqAsync(stoppingToken);

        if (_connection == null || _channel == null)
        {
            _logger.LogError("Не удалось установить соединение с RabbitMQ. Consumer не запущен.");
            return;
        }

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += OnMessageReceivedAsync;

        _channel.BasicConsume(
            queue: RabbitMqTopology.ShiftEndedQueue,
            autoAck: false,
            consumer: consumer);

        _logger.LogInformation("ShiftEndedConsumer запущен и слушает очередь {Queue}.",
            RabbitMqTopology.ShiftEndedQueue);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Нормальное завершение.
        }
    }

    private async Task WaitForRabbitMqAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _rabbitOptions.Host,
            Port = _rabbitOptions.Port,
            UserName = _rabbitOptions.Username,
            Password = _rabbitOptions.Password,
            VirtualHost = _rabbitOptions.VirtualHost,
            DispatchConsumersAsync = true,
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
        };

        for (var attempt = 1; attempt <= 10 && !stoppingToken.IsCancellationRequested; attempt++)
        {
            try
            {
                _connection = factory.CreateConnection("Humidity.Notification.Service");
                _channel = _connection.CreateModel();
                DeclareTopology(_channel);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Попытка {Attempt} подключения к RabbitMQ не удалась. Ждём 5 секунд.",
                    attempt);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private void DeclareTopology(IModel channel)
    {
        channel.ExchangeDeclare(
            exchange: RabbitMqTopology.EventsExchange,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false);

        channel.ExchangeDeclare(
            exchange: RabbitMqTopology.DeadLetterExchange,
            type: ExchangeType.Fanout,
            durable: true,
            autoDelete: false);

        channel.QueueDeclare(
            queue: RabbitMqTopology.DeadLetterQueue,
            durable: true,
            exclusive: false,
            autoDelete: false);

        channel.QueueBind(
            queue: RabbitMqTopology.DeadLetterQueue,
            exchange: RabbitMqTopology.DeadLetterExchange,
            routingKey: "");

        var queueArgs = new Dictionary<string, object>
        {
            { "x-dead-letter-exchange", RabbitMqTopology.DeadLetterExchange }
        };

        channel.QueueDeclare(
            queue: RabbitMqTopology.ShiftEndedQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: queueArgs);

        channel.QueueBind(
            queue: RabbitMqTopology.ShiftEndedQueue,
            exchange: RabbitMqTopology.EventsExchange,
            routingKey: RabbitMqTopology.ShiftEndedRoutingKey);
    }

    private async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs ea)
    {
        if (_channel == null) return;

        // Если началась остановка — не начинаем обработку нового сообщения,
        // возвращаем его в очередь.
        if (_stopping)
        {
            _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
            return;
        }

        Interlocked.Increment(ref _activeHandlers);

        ShiftEndedEvent? evt = null;
        try
        {
            var body = ea.Body.ToArray();
            var json = Encoding.UTF8.GetString(body);
            evt = JsonSerializer.Deserialize<ShiftEndedEvent>(json, JsonDefaults.Options);

            if (evt == null)
            {
                _logger.LogError("Не удалось десериализовать событие. Raw: {Json}", json);
                _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
                return;
            }

            _logger.LogInformation(
                "Получено событие окончания смены {ShiftType} {Start}–{End}. EventId={EventId}",
                evt.ShiftType, evt.ShiftStart, evt.ShiftEnd, evt.EventId);

            using var scope = _serviceProvider.CreateScope();
            var mongo = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
            var emailSender = scope.ServiceProvider.GetRequiredService<EmailSender>();
            var htmlBuilder = scope.ServiceProvider.GetRequiredService<ShiftReportHtmlBuilder>();
            var grpcClient = scope.ServiceProvider.GetRequiredService<MeasurementGrpc.MeasurementGrpcClient>();

            // === ШАГ 1. Идемпотентность ===
            var alreadyProcessed = await mongo.Notifications
                .Find(x => x.EventId == evt.EventId)
                .AnyAsync();

            if (alreadyProcessed)
            {
                _logger.LogInformation("Событие {EventId} уже обработано, пропускаем.", evt.EventId);
                _channel.BasicAck(ea.DeliveryTag, multiple: false);
                return;
            }

            // === ШАГ 2. Получение агрегатов по gRPC ===
            var stats = await grpcClient.GetShiftStatisticsAsync(new GetShiftStatisticsRequest
            {
                FromDate = evt.ShiftStart.ToUniversalTime().ToString("O"),
                ToDate = evt.ShiftEnd.ToUniversalTime().ToString("O")
            });

            // === ШАГ 3. Формирование HTML-письма ===
            var subject = $"Отчёт по влажности за {(evt.ShiftType == "day" ? "дневную" : "ночную")} смену " +
                          $"({TimeZoneInfo.ConvertTime(evt.ShiftStart, _timeZone):dd.MM.yyyy})";
            var html = htmlBuilder.Build(evt, stats, _timeZone);

            // === ШАГ 4. Отправка письма ===
            var recipients = _recipients.GetRecipients().ToList();
            var log = new NotificationLog
            {
                EventId = evt.EventId,
                ShiftType = evt.ShiftType,
                ShiftStart = evt.ShiftStart.UtcDateTime,
                ShiftEnd = evt.ShiftEnd.UtcDateTime,
                SentAt = DateTime.UtcNow,
                Recipients = recipients,
                Subject = subject,
                Status = "Success",
                VehiclesCount = stats.Vehicles.Count,
                TotalMeasurements = stats.TotalMeasurements,
                OverallAverageHumidity = stats.OverallAverage,
                OverallMinHumidity = stats.OverallMin,
                OverallMaxHumidity = stats.OverallMax,
                Vehicles = stats.Vehicles.Select(v => new NotificationVehicleSnapshot
                {
                    VehicleId = Guid.TryParse(v.VehicleId, out var g) ? g : Guid.Empty,
                    Number = v.Number,
                    VehiclePlate = v.VehiclePlate,
                    Counterparty = v.Counterparty,
                    MeasurementsCount = v.MeasurementsCount,
                    AverageHumidity = v.AverageHumidity,
                    MinHumidity = v.MinHumidity,
                    MaxHumidity = v.MaxHumidity
                }).ToList()
            };

            try
            {
                await emailSender.SendAsync(recipients, subject, html);
            }
            catch (Exception sendEx)
            {
                log.Status = "Failure";
                log.ErrorMessage = sendEx.Message;
                _logger.LogError(sendEx, "Ошибка отправки письма. EventId={EventId}", evt.EventId);
            }

            // === ШАГ 5. Сохранение записи в MongoDB ===
            await mongo.Notifications.InsertOneAsync(log);

            // === ШАГ 6. Ack / Nack ===
            if (log.Status == "Success")
            {
                _channel.BasicAck(ea.DeliveryTag, multiple: false);
                _logger.LogInformation("Событие {EventId} успешно обработано.", evt.EventId);
            }
            else
            {
                _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Необработанная ошибка при обработке сообщения. EventId={EventId}",
                evt?.EventId);

            _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
        }
        finally
        {
            Interlocked.Decrement(ref _activeHandlers);
        }
    }

    /// <summary>
    /// Graceful shutdown:
    ///   1. Устанавливаем _stopping = true — обработчик перестанет принимать новые сообщения.
    ///   2. Ждём до 30 секунд, пока активные обработчики завершатся.
    ///   3. Закрываем канал и соединение.
    /// </summary>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("ShiftEndedConsumer останавливается...");
        _stopping = true;

        // Ждём завершения активных обработок (до 30 секунд).
        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (Volatile.Read(ref _activeHandlers) > 0 && DateTime.UtcNow < deadline)
        {
            _logger.LogInformation(
                "Ожидание завершения {Count} активных обработчиков...",
                _activeHandlers);
            await Task.Delay(500, cancellationToken);
        }

        try
        {
            _channel?.Close();
            _channel?.Dispose();
            _connection?.Close();
            _connection?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ошибка при закрытии соединения с RabbitMQ.");
        }

        await base.StopAsync(cancellationToken);
    }
}