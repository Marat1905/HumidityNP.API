using Asp.Versioning;
using Humidity.Application.DTOs;
using Humidity.Application.Interfaces;
using Humidity.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Humidity.API.Controllers;

/// <summary>
/// Контроллер для управления записями о замерах влажности.
///
/// Отвечает только за CRUD-операции над замерами и отчёты, построенные на них:
///  - список замеров с фильтрами и пагинацией;
///  - создание/обновление/удаление замеров;
///  - массовая загрузка;
///  - статистика по конкретной машине;
///  - отчёт по сменам (фильтр по времени выезда машины);
///  - агрегированный отчёт за период.
///
/// Аналитика по поставщикам (включая топ) НЕ входит в зону ответственности
/// этого контроллера — за неё отвечает SuppliersController.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("humidity/api/v{version:apiVersion}/[controller]")]
public class MeasurementsController : ControllerBase
{
    private readonly IMeasurementService _measurementService;

    public MeasurementsController(IMeasurementService measurementService)
    {
        _measurementService = measurementService;
    }

    /// <summary>
    /// Получить страницу всех замеров (без привязки к машине)
    /// </summary>
    /// <param name="pageNumber">Номер страницы</param>
    /// <param name="pageSize">Размер страницы</param>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<MeasurementDto>), 200)]
    public async Task<IActionResult> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var result = await _measurementService.GetAllPagedAsync(pageNumber, pageSize, HttpContext.RequestAborted);
        return Ok(result);
    }

    /// <summary>
    /// Получить страницу замеров для указанной машины
    /// </summary>
    /// <param name="vehicleId">Идентификатор машины</param>
    /// <param name="pageNumber">Номер страницы.</param>
    /// <param name="pageSize">Размер страницы.</param>
    [HttpGet("vehicle/{vehicleId}")]
    [ProducesResponseType(typeof(PagedResult<MeasurementDto>), 200)]
    public async Task<IActionResult> GetByVehicle(Guid vehicleId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var result = await _measurementService.GetByVehicleIdPagedAsync(vehicleId, pageNumber, pageSize, HttpContext.RequestAborted);
        return Ok(result);
    }

    /// <summary>
    /// Получить последний замер для указанной машины
    /// </summary>
    /// <param name="vehicleId">Идентификатор машины</param>
    [HttpGet("vehicle/{vehicleId}/latest")]
    [ProducesResponseType(typeof(MeasurementDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetLatestByVehicle(Guid vehicleId)
    {
        var measurement = await _measurementService.GetLatestByVehicleIdAsync(vehicleId, HttpContext.RequestAborted);

        if (measurement == null)
        {
            return NotFound($"Замеры для машины с id {vehicleId} не найдены");
        }

        return Ok(measurement);
    }

    /// <summary>
    /// Получить статистику по замерам для указанной машины.
    /// </summary>
    /// <param name="vehicleId">Идентификатор машины.</param>
    [HttpGet("vehicle/{vehicleId}/statistics")]
    [ProducesResponseType(typeof(MeasurementStatisticsDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetStatisticsByVehicle(Guid vehicleId)
    {
        var stats = await _measurementService.GetStatisticsByVehicleIdAsync(vehicleId, HttpContext.RequestAborted);
        return Ok(stats);
    }

    /// <summary>
    /// Получить страницу замеров за указанную дату
    /// </summary>
    /// <param name="date">Дата в формате YYYY-MM-DD</param>
    /// <param name="pageNumber">Номер страницы.</param>
    /// <param name="pageSize">Размер страницы.</param>
    [HttpGet("date/{date}")]
    [ProducesResponseType(typeof(PagedResult<MeasurementDto>), 200)]
    public async Task<IActionResult> GetByDate(DateTimeOffset date, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var result = await _measurementService.GetByDatePagedAsync(date, pageNumber, pageSize, HttpContext.RequestAborted);
        return Ok(result);
    }

    /// <summary>
    /// Создать новую запись о замере
    /// </summary>
    /// <param name="request">Данные для создания</param>
    [HttpPost]
    [ProducesResponseType(typeof(MeasurementDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Create([FromBody] CreateMeasurementRequest request)
    {
        // Валидация выполняется автоматически благодаря [ApiController] и FluentValidation.
        var created = await _measurementService.CreateAsync(request, HttpContext.RequestAborted);
        return CreatedAtAction(nameof(GetByVehicle), new { vehicleId = request.VehicleId }, created);
    }

    /// <summary>
    /// Массовая загрузка замеров (для выгрузки с мобильного приложения)
    /// Возвращает результат с количеством созданных и пропущенных записей.
    /// </summary>
    /// <param name="requests">Список запросов на создание</param>
    [HttpPost("bulk")]
    [ProducesResponseType(typeof(BulkMeasurementResult), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> BulkCreate([FromBody] IEnumerable<CreateMeasurementRequest> requests)
    {
        // Валидация каждого элемента выполняется внутри сервиса.
        var result = await _measurementService.BulkCreateAsync(requests, HttpContext.RequestAborted);
        return CreatedAtAction(nameof(GetByDate), new { date = DateTimeOffset.UtcNow.Date }, result);
    }

    /// <summary>
    /// Обновить существующую запись о замере
    /// </summary>
    /// <param name="id">Идентификатор замера</param>
    /// <param name="request">Данные для обновления</param>
    [HttpPut("{id}")]
    [Authorize(Policy = "TCXPolicy")]
    [ProducesResponseType(typeof(MeasurementDto), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateMeasurementRequest request)
    {
        // Валидация выполняется автоматически.
        var updated = await _measurementService.UpdateAsync(id, request, HttpContext.RequestAborted);
        return Ok(updated);
    }

    /// <summary>
    /// Удалить запись о замере
    /// </summary>
    /// <param name="id">Идентификатор замера</param>
    [HttpDelete("{id}")]
    [Authorize(Policy = "TCXPolicy")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _measurementService.DeleteAsync(id, HttpContext.RequestAborted);
        return NoContent();
    }

    /// <summary>
    /// Получить страницу замеров в диапазоне дат (фильтр по Timestamp замера).
    /// </summary>
    /// <param name="from">Начало диапазона (включительно) в формате ISO 8601.</param>
    /// <param name="to">Конец диапазона (включительно) в формате ISO 8601.</param>
    /// <param name="pageNumber">Номер страницы.</param>
    /// <param name="pageSize">Размер страницы.</param>
    [HttpGet("range")]
    [ProducesResponseType(typeof(PagedResult<MeasurementDto>), 200)]
    public async Task<IActionResult> GetByDateRange(
        [FromQuery] DateTimeOffset from,
        [FromQuery] DateTimeOffset to,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10000)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 20000) pageSize = 20000;

        var result = await _measurementService.GetByDateRangePagedAsync(
            from, to, pageNumber, pageSize, HttpContext.RequestAborted);

        return Ok(result);
    }

    /// <summary>
    /// Получить страницу замеров для машин, у которых ВРЕМЯ ВЫЕЗДА (Vehicle.ExitDate)
    /// попадает в указанный диапазон. Ключевой эндпоинт для отчёта по сменам.
    /// </summary>
    /// <param name="from">Начало диапазона (включительно) для времени выезда машины.</param>
    /// <param name="to">Конец диапазона (включительно) для времени выезда машины.</param>
    /// <param name="pageNumber">Номер страницы.</param>
    /// <param name="pageSize">Размер страницы.</param>
    /// <param name="order">Порядок сортировки по дате выезда: 'desc' — новые сверху (по умолчанию), 'asc' — старые сверху.</param>
    [HttpGet("shift")]
    [ProducesResponseType(typeof(PagedResult<MeasurementDto>), 200)]
    public async Task<IActionResult> GetByVehicleExitDate(
        [FromQuery] DateTimeOffset from,
        [FromQuery] DateTimeOffset to,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20000,
        [FromQuery] string order = "desc")
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 20000) pageSize = 20000;

        bool sortDescending = !string.Equals(order, "asc", StringComparison.OrdinalIgnoreCase);

        var result = await _measurementService.GetByVehicleExitDateRangePagedAsync(
            from, to, pageNumber, pageSize, sortDescending, HttpContext.RequestAborted);

        return Ok(result);
    }

    /// <summary>
    /// Получить агрегированный отчёт за период с сортировкой и пагинацией на стороне сервера.
    ///
    /// Эндпоинт рассчитан на длительные периоды (вплоть до года и больше):
    ///  - сервер сам делает GROUP BY VehicleId, считает все агрегаты,
    ///    сортирует и применяет пагинацию в SQL;
    ///  - на клиент уходит только одна страница + общая статистика по всем машинам.
    /// </summary>
    /// <param name="from">Начало периода (включительно) по Timestamp замера.</param>
    /// <param name="to">Конец периода (включительно) по Timestamp замера.</param>
    /// <param name="sortBy">
    /// Поле сортировки: "exitDate" (по умолчанию), "averageHumidity", "lastMeasurement".
    /// </param>
    /// <param name="order">Порядок сортировки: 'desc' — по убыванию (по умолчанию), 'asc' — по возрастанию.</param>
    /// <param name="pageNumber">Номер страницы (начиная с 1).</param>
    /// <param name="pageSize">Размер страницы (макс. 500).</param>
    [HttpGet("period-report")]
    [ProducesResponseType(typeof(PeriodReportResponseDto), 200)]
    public async Task<IActionResult> GetPeriodReport(
        [FromQuery] DateTimeOffset from,
        [FromQuery] DateTimeOffset to,
        [FromQuery] string sortBy = "exitDate",
        [FromQuery] string order = "desc",
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 100)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 100;
        if (pageSize > 500) pageSize = 500;

        bool sortDescending = !string.Equals(order, "asc", StringComparison.OrdinalIgnoreCase);

        var result = await _measurementService.GetPeriodReportAsync(
            from, to, sortBy, sortDescending, pageNumber, pageSize, HttpContext.RequestAborted);

        return Ok(result);
    }

    /// <summary>
    /// Получить список сессий работы со штабелями за период.
    ///
    /// Сессии формируются на сервере сложным SQL-запросом с оконными функциями.
    /// Все четыре порога алгоритма вынесены в опциональные параметры эндпоинта,
    /// чтобы UI мог управлять «строгостью» разбиения без пересборки и переразвёртывания.
    ///
    /// АЛГОРИТМ:
    ///  1. Замеры внутри одного штабеля сортируются по времени.
    ///  2. Между соседними замерами считается разрыв.
    ///  3. Если разрыв >= baseGapHours — начинается новая базовая сессия.
    ///  4. Короткие «осколки» склеиваются с предыдущей сессией, если:
    ///     - разрыв между ними < mergeGapHours, И
    ///     - в осколке мало замеров (< minMeas) ИЛИ мало машин (< minVeh).
    ///  5. Поставщики группируются по ИНН, при его отсутствии — по нормализованному имени;
    ///     «основной поставщик» сессии — тот, у которого больше всего машин.
    ///
    /// ПАРАМЕТРЫ АЛГОРИТМА (все опциональные, дефолты подставляются в репозитории):
    ///  - baseGapHours  — минимальный разрыв в часах между замерами, РАЗРЕЗАЮЩИЙ
    ///                    сессию на две. Дефолт 48ч. Диапазон: [0; 720] (до 30 суток).
    ///                    Именно этот параметр чинит случай, когда ночная пауза
    ///                    ~24–28 часов ошибочно разрезала одну смену на две.
    ///  - mergeGapHours — максимальный разрыв в часах между двумя СОСЕДНИМИ сессиями,
    ///                    при котором ещё возможна склейка. Дефолт 72ч.
    ///                    Должен быть строго больше baseGapHours; если пользователь
    ///                    передаст некорректную пару, репозиторий молча поднимет
    ///                    mergeGap до baseGap + 24ч.
    ///  - minMeas       — порог «мало замеров». Дефолт 20. Диапазон: [0; 10000].
    ///  - minVeh        — порог «мало машин». Дефолт 3. Диапазон: [0; 1000].
    ///
    /// ВАЖНО: границы периода применяются к «сырым» замерам, а не к сессиям.
    /// Если сессия пересекает границу периода, она будет усечена по границе.
    /// </summary>
    /// <param name="from">Начало периода (включительно) в формате ISO 8601.</param>
    /// <param name="to">Конец периода (включительно) в формате ISO 8601.</param>
    /// <param name="baseGapHours">Разрыв в часах, разрезающий сессии. Дефолт 48.</param>
    /// <param name="mergeGapHours">Максимальный разрыв в часах для склейки. Дефолт 72.</param>
    /// <param name="minMeas">Порог «мало замеров». Дефолт 20.</param>
    /// <param name="minVeh">Порог «мало машин». Дефолт 3.</param>
    [HttpGet("stack-sessions")]
    [ProducesResponseType(typeof(IEnumerable<StackSessionDto>), 200)]
    public async Task<IActionResult> GetStackSessions(
        [FromQuery] DateTimeOffset from,
        [FromQuery] DateTimeOffset to,
        [FromQuery] int? baseGapHours = null,
        [FromQuery] int? mergeGapHours = null,
        [FromQuery] int? minMeas = null,
        [FromQuery] int? minVeh = null)
    {
        // Конвертируем часы (int?) в TimeSpan? — внутри репозитория уже
        // подставятся дефолты, если параметр не задан. Это позволяет UI
        // передавать «простые» целые часы, а не ISO-8601 duration.
        TimeSpan? baseGap = baseGapHours.HasValue
            ? TimeSpan.FromHours(baseGapHours.Value)
            : (TimeSpan?)null;

        TimeSpan? mergeGap = mergeGapHours.HasValue
            ? TimeSpan.FromHours(mergeGapHours.Value)
            : (TimeSpan?)null;

        var result = await _measurementService.GetStackSessionsAsync(
            from,
            to,
            baseGap,
            mergeGap,
            minMeas,
            minVeh,
            HttpContext.RequestAborted);

        return Ok(result);
    }

    /// <summary>
    /// Получить сводную статистику по штабелям за период.
    ///
    /// Возвращает разбивку замеров и машин:
    ///  - с заполненным StackNumber (попадают в отчёт по штабелям);
    ///  - без StackNumber (в отчёт по штабелям не попадают).
    ///
    /// Нужно на клиенте, чтобы объяснить расхождение между
    /// «Отчётом за период» (там фильтра по штабелю нет) и
    /// «Отчётом по штабелям» (там фильтр StackNumber обязателен).
    /// </summary>
    /// <param name="from">Начало периода (включительно) в формате ISO 8601.</param>
    /// <param name="to">Конец периода (включительно) в формате ISO 8601.</param>
    [HttpGet("stack-sessions-stats")]
    [ProducesResponseType(typeof(StackSessionsStatsDto), 200)]
    public async Task<IActionResult> GetStackSessionsStats(
        [FromQuery] DateTimeOffset from,
        [FromQuery] DateTimeOffset to)
    {
        var result = await _measurementService.GetStackSessionsStatsAsync(
            from, to, HttpContext.RequestAborted);
        return Ok(result);
    }
}