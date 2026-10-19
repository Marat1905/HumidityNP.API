namespace Humidity.Domain.Common;

/// <summary>
/// Информация о поставщике внутри сессии по штабелю.
/// Содержит количество машин, тюков, порванных тюков и общий вес.
/// Используется в <see cref="StackSessionDto.CounterpartiesDetails"/>.
/// </summary>
public class StackSessionCounterpartyDto
{
    /// <summary>
    /// Каноническое (самое короткое) имя поставщика в группе.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Количество машин этого поставщика в сессии.
    /// </summary>
    public int VehiclesCount { get; set; }

    /// <summary>
    /// Общее количество тюков по всем машинам этого поставщика в сессии.
    /// <c>null</c> — если ни у одной машины поставщика не указано количество тюков.
    /// Ноль означает «указано 0 тюков», <c>null</c> — «данных нет».
    /// </summary>
    public int? Bales { get; set; }

    /// <summary>
    /// Общее количество порванных тюков по всем машинам этого поставщика в сессии.
    /// <c>null</c> — если данных нет.
    /// </summary>
    public int? DamagedBales { get; set; }

    /// <summary>
    /// Общий вес груза (кг) по всем машинам этого поставщика в сессии.
    /// <c>null</c> — если данных нет.
    /// </summary>
    public double? WeightKg { get; set; }
}

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
    /// Общее количество тюков в сессии (сумма по всем уникальным машинам).
    /// <c>null</c> — если ни у одной машины в сессии не указано количество тюков.
    /// Ноль означает «суммарно 0 тюков», <c>null</c> — «данных нет».
    /// </summary>
    public int? TotalBales { get; set; }

    /// <summary>
    /// Общее количество порванных тюков в сессии.
    /// <c>null</c> — если данных нет.
    /// </summary>
    public int? TotalDamagedBales { get; set; }

    /// <summary>
    /// Общий вес груза (кг) в сессии.
    /// <c>null</c> — если данных нет.
    /// </summary>
    public double? TotalWeightKg { get; set; }

    /// <summary>
    /// Количество тюков у основного поставщика сессии.
    /// <c>null</c> — если данных нет или основной поставщик не определён.
    /// </summary>
    public int? PrimaryCounterpartyBales { get; set; }

    /// <summary>
    /// Детальная разбивка по поставщикам сессии с количеством тюков и весом.
    /// Каждый элемент соответствует одному уникальному поставщику (по cp_key).
    /// Пустой список, если поставщики не определены.
    /// </summary>
    public List<StackSessionCounterpartyDto> CounterpartiesDetails { get; set; } = new();

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