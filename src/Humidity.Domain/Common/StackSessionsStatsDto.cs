namespace Humidity.Domain.Common;

/// <summary>
/// Сводная статистика по штабелям за период.
///
/// Нужна, чтобы на клиенте можно было честно показать пользователю:
/// сколько замеров попало в отчёт по штабелям, а сколько — нет
/// (у машин без указанного StackNumber).
///
/// Это устраняет расхождение между «Отчётом за период» (где фильтра по штабелю нет)
/// и «Отчётом по штабелям» (где есть жёсткий фильтр StackNumber IS NOT NULL AND TRIM(...) <> '').
/// </summary>
public class StackSessionsStatsDto
{
    /// <summary>
    /// Общее количество замеров за период (без фильтра по штабелю).
    /// Это то же число, что показывает «Отчёт за период».
    /// </summary>
    public int TotalMeasurements { get; set; }

    /// <summary>
    /// Количество замеров, попавших в отчёт по штабелям (StackNumber заполнен).
    /// </summary>
    public int MeasurementsWithStack { get; set; }

    /// <summary>
    /// Количество замеров, НЕ попавших в отчёт по штабелям (StackNumber пустой или NULL).
    /// Именно из-за этих замеров цифры в двух отчётах не совпадают.
    /// </summary>
    public int MeasurementsWithoutStack { get; set; }

    /// <summary>
    /// Общее количество уникальных машин с замерами за период.
    /// </summary>
    public int TotalVehicles { get; set; }

    /// <summary>
    /// Количество уникальных машин с заполненным StackNumber.
    /// </summary>
    public int VehiclesWithStack { get; set; }

    /// <summary>
    /// Количество уникальных машин без StackNumber.
    /// </summary>
    public int VehiclesWithoutStack { get; set; }

    /// <summary>
    /// Количество уникальных штабелей (по нормализованному номеру).
    /// </summary>
    public int UniqueStacks { get; set; }
}