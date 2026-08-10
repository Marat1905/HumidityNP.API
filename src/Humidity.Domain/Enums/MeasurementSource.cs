using System.Text.Json.Serialization;

namespace Humidity.Domain.Enums;

/// <summary>
/// Источник данных замера влажности.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MeasurementSource
{
    /// <summary>
    /// Автоматический замер с датчика (BLE).
    /// </summary>
    Auto = 0,

    /// <summary>
    /// Ручной ввод данных.
    /// </summary>
    Manual = 1
}