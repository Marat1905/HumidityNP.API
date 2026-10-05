namespace Humidity.Domain.Common;

/// <summary>
/// Одна сессия работы со штабелем.
///
/// Формируется серверным SQL-запросом с оконными функциями:
///  - сессии разбиваются по разрыву во времени (по умолчанию >= 24 часов между замерами);
///  - короткие «осколки» (мало замеров/машин) примыкают к предыдущей сессии,
///    если разрыв между ними меньше 72 часов;
///  - поставщики группируются по ИНН, при его отсутствии — по нормализованному имени;
///  - каноническое имя поставщика — самое короткое в группе
///    (обычно «Тандер», а не «Тандер(Новосибирск)»);
///  - «основной поставщик» сессии — тот, у которого больше всего машин.
/// </summary>
public class StackSessionDto
{
    /// <summary>
    /// Номер штабеля (нормализованный: пробелы схлопнуты, латинские «а/А» приведены к латинице).
    /// </summary>
    public string StackNumber { get; set; } = string.Empty;

    /// <summary>
    /// Идентификатор сессии внутри штабеля (нумерация с 1, сквозная по времени).
    /// </summary>
    public int SessionId { get; set; }

    /// <summary>
    /// Начало сессии (локальное время Екатеринбурга, как в исходном SQL-запросе).
    /// </summary>
    public DateTime SessionStart { get; set; }

    /// <summary>
    /// Конец сессии (локальное время Екатеринбурга).
    /// </summary>
    public DateTime SessionEnd { get; set; }

    /// <summary>
    /// Длительность сессии в формате «чч:мм:сс» (чч может быть больше 24).
    /// Формируется сразу в SQL, чтобы не терять точность на клиенте.
    /// </summary>
    public string Duration { get; set; } = string.Empty;

    /// <summary>
    /// Количество замеров в сессии.
    /// </summary>
    public int MeasurementsCount { get; set; }

    /// <summary>
    /// Количество уникальных машин в сессии.
    /// </summary>
    public int VehiclesCount { get; set; }

    /// <summary>
    /// Количество уникальных поставщиков в сессии (по ключу cp_key: ИНН или имя).
    /// </summary>
    public int CounterpartyCount { get; set; }

    /// <summary>
    /// Основной поставщик сессии (у которого больше всего машин).
    /// Пустая строка, если поставщик не определён.
    /// </summary>
    public string PrimaryCounterparty { get; set; } = string.Empty;

    /// <summary>
    /// Полный список поставщиков сессии, отсортированный по убыванию числа машин
    /// и по алфавиту. Разделитель — «; ».
    /// </summary>
    public string Counterparties { get; set; } = string.Empty;

    /// <summary>
    /// Средняя влажность по всем замерам сессии.
    /// <c>null</c>, если замеров нет — так «нет данных» не путается с «средняя 0%».
    /// </summary>
    public double? AverageHumidity { get; set; }

    /// <summary>
    /// Минимальная влажность среди замеров сессии.
    /// <c>null</c>, если замеров нет.
    /// </summary>
    public double? MinHumidity { get; set; }

    /// <summary>
    /// Максимальная влажность среди замеров сессии.
    /// <c>null</c>, если замеров нет.
    /// </summary>
    public double? MaxHumidity { get; set; }
}