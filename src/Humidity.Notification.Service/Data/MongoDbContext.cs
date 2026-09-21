using Humidity.Notification.Service.Models;
using Humidity.Notification.Service.Options;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace Humidity.Notification.Service.Data;

/// <summary>
/// Простой контекст MongoDB. Содержит ссылку на коллекцию журнала
/// уведомлений и создаёт индексы при первом обращении.
/// </summary>
public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    /// <summary>
    /// Статический конструктор — гарантирует, что сериализатор Guid
    /// зарегистрируется до первого обращения к коллекции.
    /// </summary>
    static MongoDbContext()
    {
        // Регистрируем GuidRepresentation.Standard глобально.
        BsonSerializer.TryRegisterSerializer(
            new GuidSerializer(GuidRepresentation.Standard));
    }

    public MongoDbContext(IOptions<MongoDbOptions> options)
    {
        var opts = options.Value;
        var client = new MongoClient(opts.ConnectionString);
        _database = client.GetDatabase(opts.DatabaseName);

        Notifications = _database.GetCollection<NotificationLog>(opts.NotificationsCollection);

        // Уникальный индекс по EventId — защита от повторной обработки
        // одного и того же события RabbitMQ (идемпотентность consumer-а).
        var indexKeys = Builders<NotificationLog>.IndexKeys.Ascending(x => x.EventId);
        var indexOptions = new CreateIndexOptions { Unique = true, Name = "ux_eventId" };
        Notifications.Indexes.CreateOne(new CreateIndexModel<NotificationLog>(indexKeys, indexOptions));

        // Индекс по sentAt — ускоряет выборку журнала за период.
        var sentAtIndex = Builders<NotificationLog>.IndexKeys.Descending(x => x.SentAt);
        Notifications.Indexes.CreateOne(new CreateIndexModel<NotificationLog>(sentAtIndex));
    }

    /// <summary>
    /// Коллекция журнала уведомлений.
    /// </summary>
    public IMongoCollection<NotificationLog> Notifications { get; }
}