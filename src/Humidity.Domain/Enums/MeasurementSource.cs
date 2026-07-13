namespace Humidity.Domain.Enums;

/// <summary>
/// Источник данных замера влажности.
/// </summary>
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