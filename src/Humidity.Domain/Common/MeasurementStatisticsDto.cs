namespace Humidity.Domain.Common;

/// <summary>
/// Статистика по замерам влажности для конкретной машины.
/// </summary>
public class MeasurementStatisticsDto
{
    /// <summary>
    /// Количество замеров.
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Среднее значение влажности.
    /// </summary>
    public double? Average { get; set; }

    /// <summary>
    /// Минимальное значение влажности.
    /// </summary>
    public double? Min { get; set; }

    /// <summary>
    /// Максимальное значение влажности.
    /// </summary>
    public double? Max { get; set; }

    /// <summary>
    /// Дата и время последнего замера.
    /// </summary>
    public DateTimeOffset? LastMeasurementTimestamp { get; set; }

    /// <summary>
    /// Количество ручных замеров.
    /// </summary>
    public int ManualCount { get; set; }

    /// <summary>
    /// Количество автоматических замеров.
    /// </summary>
    public int AutoCount { get; set; }
}