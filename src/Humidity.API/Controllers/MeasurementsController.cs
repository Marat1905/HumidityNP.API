using Asp.Versioning;
using Humidity.Application.DTOs;
using Humidity.Application.Interfaces;
using Humidity.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace Humidity.API.Controllers;

/// <summary>
/// Контроллер для управления записями о замерах влажности
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class MeasurementsController : ControllerBase
{
    private readonly IMeasurementService _measurementService;

    public MeasurementsController(IMeasurementService measurementService)
    {
        _measurementService = measurementService;
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

        var result = await _measurementService.GetByVehicleIdPagedAsync(vehicleId, pageNumber, pageSize);
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
        var measurement = await _measurementService.GetLatestByVehicleIdAsync(vehicleId);
        if (measurement == null)
        {
            return NotFound($"Замеры для машины с id {vehicleId} не найдены");
        }

        return Ok(measurement);
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

        var result = await _measurementService.GetByDatePagedAsync(date, pageNumber, pageSize);
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
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var created = await _measurementService.CreateAsync(request);
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
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _measurementService.BulkCreateAsync(requests);
        return CreatedAtAction(nameof(GetByDate), new { date = DateTimeOffset.UtcNow.Date }, result);
    }

    /// <summary>
    /// Обновить существующую запись о замере
    /// </summary>
    /// <param name="id">Идентификатор замера</param>
    /// <param name="request">Данные для обновления</param>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(MeasurementDto), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateMeasurementRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var updated = await _measurementService.UpdateAsync(id, request);
        return Ok(updated);
    }

    /// <summary>
    /// Удалить запись о замере
    /// </summary>
    /// <param name="id">Идентификатор замера</param>
    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _measurementService.DeleteAsync(id);
        return NoContent();
    }
}