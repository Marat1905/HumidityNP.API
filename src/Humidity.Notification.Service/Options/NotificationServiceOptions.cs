namespace Humidity.Notification.Service.Options;

/// <summary>
/// Настройки подключения к основному API (по gRPC) для получения
/// агрегированной статистики смены.
/// </summary>
public class NotificationServiceOptions
{
    public const string SectionName = "HumidityApi";

    /// <summary>
    /// Адрес gRPC-сервиса Humidity.API, например: http://api:8081
    /// </summary>
    public string GrpcAddress { get; set; } = "http://localhost:5001";
}