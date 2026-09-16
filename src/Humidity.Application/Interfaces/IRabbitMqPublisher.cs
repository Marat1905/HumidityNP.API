namespace Humidity.Application.Interfaces;

/// <summary>
/// Абстракция издателя событий в RabbitMQ.
/// Используется бизнес-сервисами слоя Application для публикации
/// доменных событий, не зная деталей реализации брокера.
/// </summary>
public interface IRabbitMqPublisher
{
    /// <summary>
    /// Опубликовать событие в основной exchange.
    /// </summary>
    /// <typeparam name="T">Тип события.</typeparam>
    /// <param name="event">Экземпляр события.</param>
    /// <param name="routingKey">Routing key для маршрутизации.</param>
    /// <param name="ct">Токен отмены.</param>
    Task PublishAsync<T>(T @event, string routingKey, CancellationToken ct = default)
        where T : class;
}