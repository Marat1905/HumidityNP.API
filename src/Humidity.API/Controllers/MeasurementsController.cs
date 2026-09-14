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
}