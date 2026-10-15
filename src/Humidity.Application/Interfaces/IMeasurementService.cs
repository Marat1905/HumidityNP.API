using Humidity.Application.DTOs;
using Humidity.Domain.Common;

namespace Humidity.Application.Interfaces;

/// <summary>
/// Сервис для управления записями о замерах влажности (CRUD).
///
/// Отвечает только за CRUD-операции над замерами и отчёты, построенные на них.
/// Аналитика по поставщикам (включая топ) — зона ответственности ISupplierService.
/// </summary>
public interface IMeasurementService
{
    /// <summary>
    /// Получить все замеры для указанной машины.
    /// </summary>
    /// <param name="vehicleId">Идентификатор машины.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Коллекция DTO замеров, отсортированных по времени.</returns>
    Task<IEnumerable<MeasurementDto>> GetByVehicleIdAsync(Guid vehicleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить страницу замеров для указанной машины.
    /// </summary>
    /// <param name="vehicleId">Идентификатор машины.</param>
    /// <param name="pageNumber">Номер страницы.</param>
    /// <param name="pageSize">Размер страницы.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Страница замеров.</returns>
    Task<PagedResult<MeasurementDto>> GetByVehicleIdPagedAsync(Guid vehicleId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить последний замер для указанной машины.
    /// </summary>
    /// <param name="vehicleId">Идентификатор машины.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>DTO последнего замера или null.</returns>
    Task<MeasurementDto?> GetLatestByVehicleIdAsync(Guid vehicleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить все замеры за указанную дату.
    /// </summary>
    /// <param name="date">Дата (только дата, время игнорируется).</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Коллекция DTO замеров.</returns>
    Task<IEnumerable<MeasurementDto>> GetByDateAsync(DateTimeOffset date, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить страницу замеров за указанную дату.
    /// </summary>
    /// <param name="date">Дата.</param>
    /// <param name="pageNumber">Номер страницы.</param>
    /// <param name="pageSize">Размер страницы.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Страница замеров за день.</returns>
    Task<PagedResult<MeasurementDto>> GetByDatePagedAsync(DateTimeOffset date, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Создать новую запись о замере.
    /// </summary>
    /// <param name="request">Данные для создания.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>DTO созданного замера.</returns>
    Task<MeasurementDto> CreateAsync(CreateMeasurementRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Обновить существующую запись о замере.
    /// </summary>
    /// <param name="id">Идентификатор замера.</param>
    /// <param name="request">Данные для обновления.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>DTO обновлённого замера.</returns>
    Task<MeasurementDto> UpdateAsync(Guid id, UpdateMeasurementRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Удалить запись о замере.
    /// </summary>
    /// <param name="id">Идентификатор замера.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Массовая загрузка замеров (для выгрузки с мобильного приложения).
    /// Возвращает результат с количеством созданных и пропущенных записей и списком ошибок.
    /// </summary>
    /// <param name="requests">Список запросов на создание.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Результат массовой загрузки.</returns>
    Task<BulkMeasurementResult> BulkCreateAsync(IEnumerable<CreateMeasurementRequest> requests, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить страницу всех замеров (без фильтра по машине).
    /// </summary>
    /// <param name="pageNumber">Номер страницы.</param>
    /// <param name="pageSize">Размер страницы.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Страница всех замеров.</returns>
    Task<PagedResult<MeasurementDto>> GetAllPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить статистику по замерам для указанной машины.
    /// </summary>
    /// <param name="vehicleId">Идентификатор машины.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Статистика.</returns>
    Task<MeasurementStatisticsDto> GetStatisticsByVehicleIdAsync(Guid vehicleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить страницу замеров в диапазоне дат (фильтр по Timestamp замера).
    /// </summary>
    /// <param name="from">Начало диапазона (включительно).</param>
    /// <param name="to">Конец диапазона (включительно).</param>
    /// <param name="pageNumber">Номер страницы.</param>
    /// <param name="pageSize">Размер страницы.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Страница замеров.</returns>
    Task<PagedResult<MeasurementDto>> GetByDateRangePagedAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить страницу замеров для машин, у которых ВРЕМЯ ВЫЕЗДА (Vehicle.ExitDate)
    /// попадает в указанный диапазон. Ключевой метод для отчёта по сменам.
    /// </summary>
    /// <param name="from">Начало диапазона (включительно) для времени выезда машины.</param>
    /// <param name="to">Конец диапазона (включительно) для времени выезда машины.</param>
    /// <param name="pageNumber">Номер страницы.</param>
    /// <param name="pageSize">Размер страницы.</param>
    /// <param name="sortDescending">true — сортировка по ExitDate по убыванию (новые сверху), false — по возрастанию.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Страница замеров.</returns>
    Task<PagedResult<MeasurementDto>> GetByVehicleExitDateRangePagedAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        int pageNumber,
        int pageSize,
        bool sortDescending,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить агрегированный отчёт за период с сортировкой и пагинацией на стороне сервера.
    /// Возвращает постраничный список машин с агрегатами и общую статистику по всем машинам.
    /// </summary>
    /// <param name="from">Начало периода (включительно).</param>
    /// <param name="to">Конец периода (включительно).</param>
    /// <param name="sortBy">Поле сортировки: "exitDate", "averageHumidity", "lastMeasurement".</param>
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
    /// Получить список сессий работы со штабелями за указанный период.
    ///
    /// Сессии формируются сложным SQL-запросом с оконными функциями.
    /// Все четыре порога вынесены в параметры метода — это позволяет UI
    /// управлять «строгостью» разбиения без пересборки и переразвёртывания.
    ///
    /// ПАРАМЕТРЫ АЛГОРИТМА (все опциональные, дефолты подставляются в репозитории):
    ///  - baseGap   — минимальный разрыв между замерами, РАЗРЕЗАЮЩИЙ сессию на две.
    ///                Дефолт 48 часов. Диапазон: [1 мин; 30 суток].
    ///                Именно этот параметр чинит случай, когда ночная пауза
    ///                ~24–28 часов ошибочно разрезала одну смену на две.
    ///  - mergeGap  — максимальный разрыв между двумя СОСЕДНИМИ сессиями,
    ///                при котором ещё возможна склейка. Дефолт 72 часа.
    ///                Должен быть строго больше baseGap, иначе склейка
    ///                никогда не сработает (метод поднимет его автоматически).
    ///  - minMeas   — порог «мало замеров». Дефолт 20. Диапазон: [0; 10000].
    ///  - minVeh    — порог «мало машин». Дефолт 3. Диапазон: [0; 1000].
    ///
    /// ПАРАМЕТРЫ СОРТИРОВКИ (добавлены для UI):
    ///  - sortBy    — "stack" (по умолчанию) — по номеру штабеля;
    ///                "lastSession" — по времени последней сессии в штабеле.
    ///  - order     — "asc" (по умолчанию) или "desc".
    ///                Применяется только к sortBy = "lastSession".
    /// </summary>
    /// <param name="from">Начало периода (включительно) по Timestamp замера.</param>
    /// <param name="to">Конец периода (включительно) по Timestamp замера.</param>
    /// <param name="baseGap">Разрыв, разрезающий сессии. Дефолт 48ч.</param>
    /// <param name="mergeGap">Максимальный разрыв для склейки. Дефолт 72ч.</param>
    /// <param name="minMeas">Порог «мало замеров». Дефолт 20.</param>
    /// <param name="minVeh">Порог «мало машин». Дефолт 3.</param>
    /// <param name="sortBy">Поле сортировки штабелей: "stack" (по умолчанию) или "lastSession".</param>
    /// <param name="order">Направление сортировки: "asc" (по умолчанию) или "desc".</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    Task<IEnumerable<StackSessionDto>> GetStackSessionsAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        TimeSpan? baseGap = null,
        TimeSpan? mergeGap = null,
        int? minMeas = null,
        int? minVeh = null,
        string? sortBy = null,
        string? order = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить сводную статистику по штабелям за период.
    ///
    /// Возвращает разбивку замеров и машин на «с указанным штабелем» и «без штабеля»,
    /// чтобы клиент мог объяснить пользователю расхождение между
    /// «Отчётом за период» и «Отчётом по штабелям».
    /// </summary>
    /// <param name="from">Начало периода (включительно).</param>
    /// <param name="to">Конец периода (включительно).</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    Task<StackSessionsStatsDto> GetStackSessionsStatsAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default);
}