namespace Humidity.Notification.Service.Options;

/// <summary>
/// Настройки подключения к RabbitMQ.
/// </summary>
public class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string Username { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";

    /// <summary>
    /// Количество повторных попыток обработки сообщения.
    /// После исчерпания сообщение уходит в DLQ.
    /// </summary>
    public int MaxRetries { get; set; } = 3;
}