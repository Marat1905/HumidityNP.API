namespace Humidity.Domain.Common;

/// <summary>
/// Детальная информация по поставщику (для раскрывающегося блока).
/// Содержит постраничный список машин поставщика с агрегированными данными
/// и общую статистику по всем машинам за период (не только по текущей странице).
/// Сортировка и пагинация выполняются на стороне сервера.
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
    /// Постраничный список машин поставщика с агрегированными данными по каждой.
    /// Пагинация и сортировка выполняются на стороне сервера.
    /// Поле <see cref="PagedResult{T}.TotalCount"/> содержит общее количество машин
    /// у поставщика за период (без учёта пагинации).
    /// </summary>
    public PagedResult<SupplierVehicleSummaryDto> Vehicles { get; set; } = new();

    /// <summary>
    /// Общая статистика по всем машинам поставщика за период
    /// (вычисляется по полному набору данных, а не только по текущей странице).
    /// </summary>
    public MeasurementStatisticsDto OverallStatistics { get; set; } = new();
}

/// <summary>
/// Сводка по одной машине поставщика.
/// </summary>
public class SupplierVehicleSummaryDto
{
    /// <summary>
    /// Идентификатор машины.
    /// </summary>
    public Guid VehicleId { get; set; }

    /// <summary>
    /// Номер пропуска.
    /// </summary>
    public string Number { get; set; } = string.Empty;

    /// <summary>
    /// Государственный номер автомобиля.
    /// </summary>
    public string VehiclePlate { get; set; } = string.Empty;

    /// <summary>
    /// Дата въезда на площадку.
    /// </summary>
    public DateTimeOffset EntryDate { get; set; }

    /// <summary>
    /// Дата выезда с площадки (может быть null).
    /// </summary>
    public DateTimeOffset? ExitDate { get; set; }

    /// <summary>
    /// Количество замеров, выполненных для данной машины за период.
    /// </summary>
    public int MeasurementsCount { get; set; }

    /// <summary>
    /// Средняя влажность по замерам машины (null, если замеров не было).
    /// </summary>
    public double? AverageHumidity { get; set; }

    /// <summary>
    /// Минимальная влажность (null, если замеров не было).
    /// </summary>
    public double? MinHumidity { get; set; }

    /// <summary>
    /// Максимальная влажность (null, если замеров не было).
    /// </summary>
    public double? MaxHumidity { get; set; }

    /// <summary>
    /// Количество автоматических замеров.
    /// </summary>
    public int AutoCount { get; set; }

    /// <summary>
    /// Количество ручных замеров.
    /// </summary>
    public int ManualCount { get; set; }

    /// <summary>
    /// Дата и время последнего замера (null, если замеров не было).
    /// </summary>
    public DateTimeOffset? LastMeasurementTimestamp { get; set; }
}