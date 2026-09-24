namespace Humidity.Domain.Common;

/// <summary>
/// DTO с информацией о разгрузке машины и средней влажности.
/// Используется для интеграции с 1С.
/// </summary>
public class OneCVehicleUnloadDto
{
    /// <summary>
    /// Уникальный идентификатор записи из 1С (ГУИД).
    /// </summary>
    public string? OneCGuid { get; set; }

    /// <summary>
    /// Количество тюков, выгруженных из машины.
    /// </summary>
    public int? BaleCount { get; set; }

    /// <summary>
    /// Количество порванных тюков.
    /// </summary>
    public int? DamagedBaleCount { get; set; }

    /// <summary>
    /// Вес выгруженного груза в килограммах.
    /// </summary>
    public double? WeightKg { get; set; }

    /// <summary>
    /// Номер штабеля, куда выгружена машина.
    /// </summary>
    public string? StackNumber { get; set; }

    /// <summary>
    /// Средняя влажность по всем замерам машины.
    /// null, если замеров нет.
    /// </summary>
    public double? AverageHumidity { get; set; }
}