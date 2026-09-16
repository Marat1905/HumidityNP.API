using Humidity.Contracts.Events;

namespace Humidity.Application.Interfaces;

/// <summary>
/// Абстракция над механизмом real-time уведомлений клиентов (SignalR).
/// Находится в слое Application, чтобы бизнес-сервисы не зависели
/// от ASP.NET Core и SignalR напрямую. Реализация — в Humidity.API.
/// </summary>
public interface IHumidityRealtimeNotifier
{
    /// <summary>
    /// Уведомить клиентов о создании новой машины.
    /// </summary>
    Task NotifyVehicleCreatedAsync(VehicleCreatedEvent evt, CancellationToken ct = default);

    /// <summary>
    /// Уведомить клиентов о создании нового замера.
    /// </summary>
    Task NotifyMeasurementCreatedAsync(MeasurementCreatedEvent evt, CancellationToken ct = default);

    /// <summary>
    /// Уведомить клиентов об обновлении существующей машины.
    /// </summary>
    Task NotifyVehicleUpdatedAsync(Guid vehicleId, CancellationToken ct = default);

    /// <summary>
    /// Уведомить клиентов об удалении замера.
    /// </summary>
    Task NotifyMeasurementDeletedAsync(Guid measurementId, Guid vehicleId, CancellationToken ct = default);

    /// <summary>
    /// Уведомить клиентов о завершении смены.
    /// </summary>
    Task NotifyShiftEndedAsync(Guid eventId, string shiftType, DateTimeOffset shiftEnd, CancellationToken ct = default);
}