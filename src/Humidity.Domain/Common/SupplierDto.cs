namespace Humidity.Domain.Common;

/// <summary>
/// Краткая информация о поставщике для списка.
/// </summary>
public class SupplierDto
{
    /// <summary>
    /// ИНН поставщика (уникальный ключ).
    /// </summary>
    public string Inn { get; set; } = string.Empty;

    /// <summary>
    /// Актуальное наименование поставщика (последнее по времени).
    /// </summary>
    public string Counterparty { get; set; } = string.Empty;

    /// <summary>
    /// Количество машин, связанных с этим поставщиком за выбранный период.
    /// </summary>
    public int VehiclesCount { get; set; }

    /// <summary>
    /// Общее количество замеров за период.
    /// </summary>
    public int TotalMeasurements { get; set; }

    /// <summary>
    /// Средняя влажность по всем замерам за период.
    /// </summary>
    public double? AverageHumidity { get; set; }

    /// <summary>
    /// Минимальная влажность за период.
    /// </summary>
    public double? MinHumidity { get; set; }

    /// <summary>
    /// Максимальная влажность за период.
    /// </summary>
    public double? MaxHumidity { get; set; }
}