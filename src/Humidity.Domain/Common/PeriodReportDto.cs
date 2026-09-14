namespace Humidity.Domain.Common;

/// <summary>
/// Одна строка отчёта за период: агрегированные данные по одной машине.
/// Формируется на сервере одним SQL-запросом с группировкой по VehicleId,
/// поэтому на клиент уходит уже готовая страница с посчитанными агрегатами.
/// </summary>
public class PeriodReportItemDto
{
    /// <summary>
    /// Идентификатор машины.
    /// </summary>
    public Guid VehicleId { get; set; }

    /// <summary>
    /// Номер пропуска машины.
    /// </summary>
    public string Number { get; set; } = string.Empty;

    /// <summary>
    /// Государственный номер автомобиля.
    /// </summary>
    public string VehiclePlate { get; set; } = string.Empty;

    /// <summary>
    /// Поставщик (наименование).
    /// </summary>
    public string Counterparty { get; set; } = string.Empty;

    /// <summary>
    /// Дата въезда машины на площадку.
    /// Может быть null, если в БД поле не заполнено.
    /// </summary>
    public DateTimeOffset? EntryDate { get; set; }

    /// <summary>
    /// Дата выезда машины с площадки.
    /// Может быть null, если машина ещё на площадке.
    /// </summary>
    public DateTimeOffset? ExitDate { get; set; }

    /// <summary>
    /// Количество замеров, выполненных для данной машины за период.
    /// </summary>
    public int MeasurementsCount { get; set; }

    /// <summary>
    /// Средняя влажность по замерам машины за период.
    /// </summary>
    public double? AverageHumidity { get; set; }

    /// <summary>
    /// Минимальная влажность по замерам машины за период.
    /// </summary>
    public double? MinHumidity { get; set; }

    /// <summary>
    /// Максимальная влажность по замерам машины за период.
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
    /// Дата и время последнего замера для машины за период.
    /// </summary>
    public DateTimeOffset? LastMeasurementTimestamp { get; set; }
}

/// <summary>
/// Общая статистика по всем машинам за период.
/// Считается на сервере по полному набору данных (без учёта пагинации),
/// поэтому не меняется при переключении страниц.
/// </summary>
public class PeriodReportSummaryDto
{
    /// <summary>
    /// Количество уникальных машин с замерами за период.
    /// </summary>
    public int VehicleCount { get; set; }

    /// <summary>
    /// Общее количество замеров за период.
    /// </summary>
    public int TotalMeasurements { get; set; }

    /// <summary>
    /// Взвешенная средняя влажность по всем замерам за период.
    /// </summary>
    public double? OverallAverageHumidity { get; set; }

    /// <summary>
    /// Минимальная влажность среди всех замеров за период.
    /// </summary>
    public double? OverallMinHumidity { get; set; }

    /// <summary>
    /// Максимальная влажность среди всех замеров за период.
    /// </summary>
    public double? OverallMaxHumidity { get; set; }

    /// <summary>
    /// Общее количество автоматических замеров.
    /// </summary>
    public int TotalAutoCount { get; set; }

    /// <summary>
    /// Общее количество ручных замеров.
    /// </summary>
    public int TotalManualCount { get; set; }
}

/// <summary>
/// Полный ответ отчёта за период: постраничный список машин и общая статистика.
/// </summary>
public class PeriodReportResponseDto
{
    /// <summary>
    /// Постраничный список машин с агрегированными данными.
    /// </summary>
    public PagedResult<PeriodReportItemDto> Vehicles { get; set; } = new();

    /// <summary>
    /// Общая статистика по всем машинам за период (не зависит от страницы).
    /// </summary>
    public PeriodReportSummaryDto Summary { get; set; } = new();
}