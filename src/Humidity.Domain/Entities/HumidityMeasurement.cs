using Humidity.Domain.Enums;

namespace Humidity.Domain.Entities;

/// <summary>
/// Замер влажности, привязанный к машине.
/// Содержит данные о влажности, температуре, типе измерения и источнике данных.
/// </summary>
public class HumidityMeasurement : BaseEntity
{
    /// <summary>
    /// Идентификатор машины (связь с Vehicle.Id).
    /// </summary>
    public Guid VehicleId { get; set; }

    /// <summary>
    /// Навигационное свойство для связи с машиной.
    /// </summary>
    public virtual Vehicle Vehicle { get; set; } = null!;

    /// <summary>
    /// Числовое значение влажности (%).
    /// </summary>
    public double HumidityValue { get; set; }

    /// <summary>
    /// Температура в градусах Цельсия.
    /// </summary>
    public double TemperatureC { get; set; }

    /// <summary>
    /// Тип измерения (из BLE-протокола).
    /// </summary>
    public string MeasurementType { get; set; } = string.Empty;

    /// <summary>
    /// Материал, для которого выполнен замер.
    /// </summary>
    public string Material { get; set; } = string.Empty;

    /// <summary>
    /// Источник данных: Auto (датчик) или Manual (вручную).
    /// </summary>
    public MeasurementSource Source { get; set; }

    /// <summary>
    /// Дата и время замера (UTC).
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Знак для BLE-данных (Less/Greater/None).
    /// </summary>
    public string Sign { get; set; } = string.Empty;
}