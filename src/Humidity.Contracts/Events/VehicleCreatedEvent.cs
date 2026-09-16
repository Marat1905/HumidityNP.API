namespace Humidity.Contracts.Events;

/// <summary>
/// Событие «создана новая машина». Публикуется в RabbitMQ,
/// потребителем может быть любой сервис. В нашем случае событие
/// также дублируется в SignalR-хаб для UI.
/// </summary>
public class VehicleCreatedEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public Guid VehicleId { get; set; }
    public string Number { get; set; } = string.Empty;
    public string VehiclePlate { get; set; } = string.Empty;
    public string Counterparty { get; set; } = string.Empty;
    public DateTimeOffset EntryDate { get; set; }
    public DateTimeOffset PublishedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Событие «создан новый замер». Публикуется в RabbitMQ
/// и дублируется в SignalR-хаб, чтобы UI моментально обновлялся.
/// </summary>
public class MeasurementCreatedEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public Guid MeasurementId { get; set; }
    public Guid VehicleId { get; set; }
    public string VehicleNumber { get; set; } = string.Empty;
    public string VehiclePlate { get; set; } = string.Empty;
    public double HumidityValue { get; set; }
    public double TemperatureC { get; set; }
    public string Source { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
    public DateTimeOffset PublishedAt { get; set; } = DateTimeOffset.UtcNow;
}