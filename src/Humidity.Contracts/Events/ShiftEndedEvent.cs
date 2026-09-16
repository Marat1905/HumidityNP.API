namespace Humidity.Contracts.Events;

/// <summary>
/// Событие «смена закончилась». Публикуется основным API в RabbitMQ
/// ровно один раз в момент пересечения границы смены (20:00 или 08:00
/// локального времени площадки). Consumer в Notification.Service
/// получает это событие, формирует письмо и рассылает его.
/// </summary>
public class ShiftEndedEvent
{
    /// <summary>
    /// Уникальный идентификатор события (для идемпотентности consumer-а).
    /// </summary>
    public Guid EventId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Тип смены: "day" (08:00–20:00) или "night" (20:00–08:00).
    /// </summary>
    public string ShiftType { get; set; } = string.Empty;

    /// <summary>
    /// Начало смены (UTC).
    /// </summary>
    public DateTimeOffset ShiftStart { get; set; }

    /// <summary>
    /// Конец смены (UTC).
    /// </summary>
    public DateTimeOffset ShiftEnd { get; set; }

    /// <summary>
    /// Идентификатор площадки (если в будущем будет мульти-склад).
    /// </summary>
    public string SiteId { get; set; } = "default";

    /// <summary>
    /// Момент публикации события (UTC).
    /// </summary>
    public DateTimeOffset PublishedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Константы с именами топологов RabbitMQ для обмена событиями.
/// Используются как издателем (Humidity.API), так и потребителем
/// (Notification.Service), поэтому вынесены в общий проект контрактов.
/// </summary>
public static class RabbitMqTopology
{
    /// <summary>
    /// Основной topic-exchange для доменных событий.
    /// </summary>
    public const string EventsExchange = "humidity.events";

    /// <summary>
    /// Очередь уведомлений об окончании смены.
    /// </summary>
    public const string ShiftEndedQueue = "humidity.notifications.shift-ended";

    /// <summary>
    /// Routing key события окончания смены.
    /// </summary>
    public const string ShiftEndedRoutingKey = "shift.ended";

    /// <summary>
    /// Очередь «мёртвых писем» для сообщений, которые consumer
    /// не смог обработать после нескольких попыток.
    /// </summary>
    public const string DeadLetterQueue = "humidity.notifications.dead-letter";

    /// <summary>
    /// Dead-letter exchange.
    /// </summary>
    public const string DeadLetterExchange = "humidity.events.dlx";
}