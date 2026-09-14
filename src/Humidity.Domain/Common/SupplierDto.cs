namespace Humidity.Domain.Common;

/// <summary>
/// Краткая информация о поставщике для списка и топа.
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
    /// Общее количество машин, связанных с этим поставщиком за выбранный период.
    /// </summary>
    public int VehiclesCount { get; set; }

    /// <summary>
    /// Количество машин, по которым проводились замеры за выбранный период.
    /// </summary>
    public int MeasuredVehiclesCount { get; set; }

    /// <summary>
    /// Общее количество замеров за период.
    /// </summary>
    public int TotalMeasurements { get; set; }

    /// <summary>
    /// Наивная средняя влажность по всем замерам за период: sum / count.
    /// Это значение показывает «сырую» среднюю без учёта объёма данных.
    /// </summary>
    public double? AverageHumidity { get; set; }

    /// <summary>
    /// Байесовски скорректированная средняя влажность.
    /// Формула: (C * m + sum) / (C + n), где
    ///   C — вес prior (PriorWeight);
    ///   m — глобальная средняя влажность (GlobalAverageHumidity);
    ///   sum — сумма влажностей у данного поставщика за период;
    ///   n — количество замеров у данного поставщика за период.
    ///
    /// Используется ТОЛЬКО в топ-поставщиках. Для обычного списка поставщиков
    /// поле остаётся null (там применяется наивная средняя AverageHumidity).
    /// </summary>
    public double? AdjustedAverageHumidity { get; set; }

    /// <summary>
    /// Вес prior (C), использованный при байесовской коррекции.
    /// Ноль означает, что коррекция не применялась, и AdjustedAverageHumidity == AverageHumidity.
    /// </summary>
    public double PriorWeight { get; set; }

    /// <summary>
    /// Глобальная средняя влажность по всем замерам за период (prior mean m).
    /// Используется при байесовской коррекции.
    /// </summary>
    public double? GlobalAverageHumidity { get; set; }

    /// <summary>
    /// Минимальная влажность за период.
    /// </summary>
    public double? MinHumidity { get; set; }

    /// <summary>
    /// Максимальная влажность за период.
    /// </summary>
    public double? MaxHumidity { get; set; }
}