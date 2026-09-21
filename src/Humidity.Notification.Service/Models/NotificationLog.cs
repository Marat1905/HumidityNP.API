using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Humidity.Notification.Service.Models;

/// <summary>
/// Запись журнала отправленных уведомлений.
/// </summary>
public class NotificationLog
{
    /// <summary>
    /// Идентификатор документа MongoDB (ObjectId).
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    /// <summary>
    /// Идентификатор события RabbitMQ, к которому относится запись.
    /// Используется для идемпотентности: одно событие — одна запись.
    /// </summary>
    [BsonElement("eventId")]
    public Guid EventId { get; set; }

    /// <summary>
    /// Тип смены: "day" или "night".
    /// </summary>
    [BsonElement("shiftType")]
    public string ShiftType { get; set; } = string.Empty;

    /// <summary>
    /// Начало смены (UTC).
    /// </summary>
    [BsonElement("shiftStart")]
    public DateTime ShiftStart { get; set; }

    /// <summary>
    /// Конец смены (UTC).
    /// </summary>
    [BsonElement("shiftEnd")]
    public DateTime ShiftEnd { get; set; }

    /// <summary>
    /// Момент фактической отправки письма.
    /// </summary>
    [BsonElement("sentAt")]
    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Список получателей письма.
    /// </summary>
    [BsonElement("recipients")]
    public List<string> Recipients { get; set; } = new();

    /// <summary>
    /// Тема письма.
    /// </summary>
    [BsonElement("subject")]
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Статус: "Success" или "Failure".
    /// </summary>
    [BsonElement("status")]
    public string Status { get; set; } = "Success";

    /// <summary>
    /// Текст ошибки при неуспешной отправке.
    /// </summary>
    [BsonElement("errorMessage")]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Количество машин в смене.
    /// </summary>
    [BsonElement("vehiclesCount")]
    public int VehiclesCount { get; set; }

    /// <summary>
    /// Общее количество замеров.
    /// </summary>
    [BsonElement("totalMeasurements")]
    public int TotalMeasurements { get; set; }

    /// <summary>
    /// Средняя влажность за смену.
    /// </summary>
    [BsonElement("overallAverageHumidity")]
    public double OverallAverageHumidity { get; set; }

    /// <summary>
    /// Минимальная влажность за смену.
    /// </summary>
    [BsonElement("overallMinHumidity")]
    public double OverallMinHumidity { get; set; }

    /// <summary>
    /// Максимальная влажность за смену.
    /// </summary>
    [BsonElement("overallMaxHumidity")]
    public double OverallMaxHumidity { get; set; }

    /// <summary>
    /// Краткая информация по машинам (id, номер, средняя влажность),
    /// </summary>
    [BsonElement("vehicles")]
    public List<NotificationVehicleSnapshot> Vehicles { get; set; } = new();
}

/// <summary>
/// Снимок агрегатов по одной машине, сохраняемый вместе с уведомлением.
/// </summary>
public class NotificationVehicleSnapshot
{
    [BsonElement("vehicleId")]
    public Guid VehicleId { get; set; }

    [BsonElement("number")]
    public string Number { get; set; } = string.Empty;

    [BsonElement("vehiclePlate")]
    public string VehiclePlate { get; set; } = string.Empty;

    [BsonElement("counterparty")]
    public string Counterparty { get; set; } = string.Empty;

    [BsonElement("measurementsCount")]
    public int MeasurementsCount { get; set; }

    [BsonElement("averageHumidity")]
    public double AverageHumidity { get; set; }

    [BsonElement("minHumidity")]
    public double MinHumidity { get; set; }

    [BsonElement("maxHumidity")]
    public double MaxHumidity { get; set; }
}