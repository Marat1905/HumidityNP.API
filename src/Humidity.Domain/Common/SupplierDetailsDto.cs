namespace Humidity.Domain.Common;

/// <summary>
/// Детальная информация по поставщику (для раскрывающегося блока).
/// </summary>
public class SupplierDetailsDto
{
    /// <summary>
    /// ИНН поставщика.
    /// </summary>
    public string Inn { get; set; } = string.Empty;

    /// <summary>
    /// Актуальное наименование.
    /// </summary>
    public string Counterparty { get; set; } = string.Empty;

    /// <summary>
    /// Список машин поставщика с агрегированными данными по каждой.
    /// </summary>
    public List<SupplierVehicleSummaryDto> Vehicles { get; set; } = new();

    /// <summary>
    /// Общая статистика по всем машинам.
    /// </summary>
    public MeasurementStatisticsDto OverallStatistics { get; set; } = new();
}

/// <summary>
/// Сводка по одной машине поставщика.
/// </summary>
public class SupplierVehicleSummaryDto
{
    public Guid VehicleId { get; set; }
    public string Number { get; set; } = string.Empty;
    public string VehiclePlate { get; set; } = string.Empty;
    public DateTimeOffset EntryDate { get; set; }
    public DateTimeOffset? ExitDate { get; set; }
    public int MeasurementsCount { get; set; }
    public double? AverageHumidity { get; set; }
    public double? MinHumidity { get; set; }
    public double? MaxHumidity { get; set; }
    public int AutoCount { get; set; }
    public int ManualCount { get; set; }
    public DateTimeOffset? LastMeasurementTimestamp { get; set; }
}