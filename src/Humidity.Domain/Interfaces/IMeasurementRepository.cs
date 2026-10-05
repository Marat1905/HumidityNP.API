using Humidity.Domain.Common;
using Humidity.Domain.Entities;

namespace Humidity.Domain.Interfaces;

/// <summary>
/// Интерфейс репозитория для работы с замерами влажности.
/// Расширяет базовый IRepository дополнительными методами, специфичными для HumidityMeasurement.
/// </summary>
public interface IMeasurementRepository : IRepository<HumidityMeasurement>
{
    /// <summary>
    /// Получить все замеры для указанной машины, отсортированные по времени (новые первыми).
    /// </summary>
    /// <param name="vehicleId">Идентификатор машины.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Коллекция замеров.</returns>
    Task<IEnumerable<HumidityMeasurement>> GetByVehicleIdAsync(Guid vehicleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить страницу замеров для указанной машины.
    /// </summary>
    /// <param name="vehicleId">Идентификатор машины.</param>
    /// <param name="pageNumber">Номер страницы.</param>
    /// <param name="pageSize">Размер страницы.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Страница замеров.</returns>
    Task<PagedResult<HumidityMeasurement>> GetByVehicleIdPagedAsync(Guid vehicleId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить последний (самый свежий) замер для указанной машины.
    /// </summary>
    /// <param name="vehicleId">Идентификатор машины.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Последний замер или null, если замеров нет.</returns>
    Task<HumidityMeasurement?> GetLatestByVehicleIdAsync(Guid vehicleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить все замеры за указанную дату.
    /// </summary>
    /// <param name="date">Дата (время игнорируется, берётся весь день).</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Коллекция замеров за день.</returns>
    Task<IEnumerable<HumidityMeasurement>> GetByDateAsync(DateTimeOffset date, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить страницу замеров за указанную дату.
    /// </summary>
    /// <param name="date">Дата.</param>
    /// <param name="pageNumber">Номер страницы.</param>
    /// <param name="pageSize">Размер страницы.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Страница замеров за день.</returns>
    Task<PagedResult<HumidityMeasurement>> GetByDatePagedAsync(DateTimeOffset date, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить замеры в диапазоне дат (фильтр по Timestamp замера).
    /// </summary>
    /// <param name="from">Начало диапазона (включительно).</param>
    /// <param name="to">Конец диапазона (включительно).</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Коллекция замеров в диапазоне.</returns>
    Task<IEnumerable<HumidityMeasurement>> GetByDateRangeAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить словарь (VehicleId → количество замеров) для переданного списка идентификаторов машин.
    /// </summary>
    /// <param name="vehicleIds">Список идентификаторов машин.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Словарь, где ключ – VehicleId, значение – количество замеров.</returns>
    Task<Dictionary<Guid, int>> GetCountsByVehicleIdsAsync(IEnumerable<Guid> vehicleIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить статистику по замерам для указанной машины.
    /// </summary>
    /// <param name="vehicleId">Идентификатор машины.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Объект статистики.</returns>
    Task<MeasurementStatisticsDto> GetStatisticsByVehicleIdAsync(Guid vehicleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить страницу замеров в диапазоне дат (фильтр по Timestamp замера).
    /// Используется в отчёте за период как «сырой» список замеров.
    /// </summary>
    /// <param name="from">Начало диапазона (включительно).</param>
    /// <param name="to">Конец диапазона (включительно).</param>
    /// <param name="pageNumber">Номер страницы.</param>
    /// <param name="pageSize">Размер страницы.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Страница замеров.</returns>
    Task<PagedResult<HumidityMeasurement>> GetByDateRangePagedAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить страницу замеров для машин, у которых ВРЕМЯ ВЫЕЗДА (Vehicle.ExitDate)
    /// попадает в указанный диапазон. Ключевой метод для отчёта по сменам:
    /// все замеры машины относятся к той смене, в которую машина выехала.
    /// Сортировка выполняется по Vehicle.ExitDate (по умолчанию — по убыванию),
    /// при равенстве — по Timestamp замера (тоже по убыванию).
    /// </summary>
    /// <param name="from">Начало диапазона (включительно) для времени выезда машины.</param>
    /// <param name="to">Конец диапазона (включительно) для времени выезда машины.</param>
    /// <param name="pageNumber">Номер страницы (начиная с 1).</param>
    /// <param name="pageSize">Размер страницы.</param>
    /// <param name="sortDescending">true — сортировка по ExitDate по убыванию (новые сверху), false — по возрастанию.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Страница замеров с подгруженными данными о машине.</returns>
    Task<PagedResult<HumidityMeasurement>> GetByVehicleExitDateRangePagedAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        int pageNumber,
        int pageSize,
        bool sortDescending,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить агрегированный отчёт за период с сортировкой и пагинацией на стороне сервера.
    ///
    /// Ключевые особенности:
    ///  - сервер сам делает GROUP BY VehicleId и считает все агрегаты (COUNT, AVG, MIN, MAX,
    ///    количество авто/ручных замеров, время последнего замера);
    ///  - сортировка выполняется в SQL по выбранному полю;
    ///  - пагинация применяется в SQL — клиент получает только одну страницу;
    ///  - общая статистика по всем машинам (Summary) считается отдельным запросом
    ///    по полному набору данных и не зависит от текущей страницы.
    ///
    /// Это позволяет безопасно запрашивать отчёты за длительные периоды (год и больше)
    /// без выгрузки всех замеров на клиент.
    /// </summary>
    /// <param name="from">Начало периода (включительно) по Timestamp замера.</param>
    /// <param name="to">Конец периода (включительно) по Timestamp замера.</param>
    /// <param name="sortBy">
    /// Поле сортировки. Поддерживаемые значения (без учёта регистра):
    ///  - "exitDate"            — по дате выезда машины;
    ///  - "averageHumidity"     — по средней влажности;
    ///  - "lastMeasurement"     — по времени последнего замера.
    /// Любое другое значение трактуется как "exitDate".
    /// </param>
    /// <param name="sortDescending">true — по убыванию, false — по возрастанию.</param>
    /// <param name="pageNumber">Номер страницы (начиная с 1).</param>
    /// <param name="pageSize">Размер страницы (максимум 500).</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Постраничный список машин + общая статистика.</returns>
    Task<PeriodReportResponseDto> GetPeriodReportAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        string sortBy,
        bool sortDescending,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить сводку по поставщикам (группировка по ИНН) за период с пагинацией и поиском.
    ///
    /// ПОИСК:
    /// Параметр <paramref name="search"/> позволяет фильтровать поставщиков по частичному совпадению
    /// ИНН или наименования (Counterparty) — регистронезависимо (через ILIKE).
    /// Фильтр применяется к «сырым» машинам до группировки: поставщик попадает в выборку,
    /// если хотя бы одна его машина за период удовлетворяет условию поиска.
    /// Пустая строка или null — поиск не применяется.
    /// </summary>
    /// <param name="from">Начало периода (включительно).</param>
    /// <param name="to">Конец периода (включительно).</param>
    /// <param name="pageNumber">Номер страницы.</param>
    /// <param name="pageSize">Размер страницы.</param>
    /// <param name="search">Строка поиска по ИНН или наименованию поставщика (частичное совпадение, регистронезависимо).</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Постраничная сводка по поставщикам.</returns>
    Task<PagedResult<SupplierDto>> GetSuppliersSummaryAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        int pageNumber,
        int pageSize,
        string? search = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить детальную информацию по поставщику (ИНН) за период с постраничной выборкой машин.
    /// Сортировка выполняется по дате въезда машины на площадку.
    /// Пагинация и сортировка выполняются на стороне сервера.
    /// </summary>
    /// <param name="inn">ИНН поставщика.</param>
    /// <param name="from">Начало периода (включительно).</param>
    /// <param name="to">Конец периода (включительно).</param>
    /// <param name="pageNumber">Номер страницы (начиная с 1).</param>
    /// <param name="pageSize">Размер страницы.</param>
    /// <param name="sortDescending">true – сортировка по дате въезда по убыванию (новые сверху), false – по возрастанию.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>DTO с постраничным списком машин и общей статистикой по всем машинам.</returns>
    Task<SupplierDetailsDto> GetSupplierDetailsAsync(
        string inn,
        DateTimeOffset from,
        DateTimeOffset to,
        int pageNumber,
        int pageSize,
        bool sortDescending,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить полный (без пагинации) список машин поставщика за период с агрегированными данными,
    /// отсортированный по дате въезда. Используется для построения графика.
    /// Сортировка выполняется на стороне сервера.
    /// </summary>
    /// <param name="inn">ИНН поставщика.</param>
    /// <param name="from">Начало периода (включительно).</param>
    /// <param name="to">Конец периода (включительно).</param>
    /// <param name="sortDescending">true – сортировка по дате въезда по убыванию (новые сверху), false – по возрастанию.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Коллекция сводок по машинам (полная, без пагинации).</returns>
    Task<IEnumerable<SupplierVehicleSummaryDto>> GetSupplierVehiclesForChartAsync(
        string inn,
        DateTimeOffset from,
        DateTimeOffset to,
        bool sortDescending,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить топ-N поставщиков по средней влажности за период с байесовской коррекцией.
    ///
    /// БАЙЕСОВСКАЯ КОРРЕКЦИЯ:
    /// Наивная средняя влажности поставщика (sum / n) плохо работает при малом количестве
    /// замеров — поставщик с 3 замерами может случайно оказаться «лучшим», а с 100 —
    /// «средним». Чтобы избежать такой нестабильности, применяем credibility adjustment:
    ///
    ///     adjusted_i = (C * m + sum_i) / (C + n_i)
    ///
    /// где:
    ///   - sum_i — сумма влажностей у поставщика i за период;
    ///   - n_i   — количество замеров у поставщика i за период;
    ///   - m     — глобальная средняя влажность по всем замерам за период (prior mean);
    ///   - C     — вес prior (priorWeight), «сколько виртуальных замеров со средней m»
    ///             добавляется к каждому поставщику.
    ///
    /// Чем меньше замеров у поставщика, тем сильнее его средняя тянется к глобальной m.
    /// Чем больше замеров, тем меньше коррекция влияет на значение.
    ///
    /// Типичные значения C:
    ///   - 0   — коррекция отключена (используется наивная средняя);
    ///   - 10  — слабая коррекция;
    ///   - 30  — умеренная (значение по умолчанию);
    ///   - 100 — сильная (нужно много замеров, чтобы «перебить» prior).
    ///
    /// Сортировка выполняется по скорректированной средней (AdjustedAverageHumidity):
    ///   - ascending = true  → по возрастанию (хорошие, низкая влажность сверху);
    ///   - ascending = false → по убыванию (плохие, высокая влажность сверху).
    /// </summary>
    /// <param name="top">Количество записей в топе (максимум 100).</param>
    /// <param name="ascending">true — низкая влажность сверху (хорошие), false — высокая (плохие).</param>
    /// <param name="from">Начало периода (включительно).</param>
    /// <param name="to">Конец периода (включительно).</param>
    /// <param name="priorWeight">Вес prior (C) для байесовской коррекции. 0 — без коррекции.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Список DTO поставщиков с наивной и скорректированной средней влажностью.</returns>
    Task<IEnumerable<SupplierDto>> GetTopSuppliersAsync(
        int top,
        bool ascending,
        DateTimeOffset from,
        DateTimeOffset to,
        double priorWeight = 30,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить список сессий работы со штабелями за указанный период.
    ///
    /// Сессии формируются сложным SQL-запросом с оконными функциями:
    ///  - базовый разрыв между замерами, разделяющий сессии, — 24 часа;
    ///  - «склейка» коротких осколков с предыдущей сессией — если разрыв < 72 часов
    ///    и в осколке мало замеров/машин;
    ///  - поставщики группируются по ИНН, при его отсутствии — по нормализованному имени.
    ///
    /// ВАЖНО: сессии считаются по замерам, попавшим в диапазон [from, to].
    /// Если сессия началась до from или заканчивается после to, она будет усечена
    /// по границе диапазона. Это осознанное поведение: фильтр применяется на уровне
    /// «сырых» замеров, а не на уровне готовых сессий.
    /// </summary>
    /// <param name="from">Начало периода (включительно) по Timestamp замера.</param>
    /// <param name="to">Конец периода (включительно) по Timestamp замера.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Список сессий, отсортированный по штабелю и времени начала.</returns>
    Task<IEnumerable<StackSessionDto>> GetStackSessionsAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить сводную статистику по штабелям за период.
    ///
    /// Считает:
    ///  - общее количество замеров (без фильтра по штабелю);
    ///  - количество замеров с заполненным StackNumber;
    ///  - количество замеров без StackNumber (они не попадают в отчёт по штабелям);
    ///  - то же самое по уникальным машинам;
    ///  - количество уникальных штабелей.
    ///
    /// Используется на клиенте, чтобы объяснить расхождение между
    /// «Отчётом за период» и «Отчётом по штабелям».
    /// </summary>
    /// <param name="from">Начало периода (включительно) по Timestamp замера.</param>
    /// <param name="to">Конец периода (включительно) по Timestamp замера.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    Task<StackSessionsStatsDto> GetStackSessionsStatsAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default);
}