namespace Humidity.Notification.Service.Options;

/// <summary>
/// Настройки подключения к MongoDB, читаются из секции MongoDb.
/// </summary>
public class MongoDbOptions
{
    public const string SectionName = "MongoDb";

    /// <summary>
    /// Строка подключения к MongoDB.
    /// </summary>
    public string ConnectionString { get; set; } = "mongodb://localhost:27017";

    /// <summary>
    /// Имя базы данных.
    /// </summary>
    public string DatabaseName { get; set; } = "humidity_notifications";

    /// <summary>
    /// Имя коллекции с журналом уведомлений.
    /// </summary>
    public string NotificationsCollection { get; set; } = "notification_logs";
}