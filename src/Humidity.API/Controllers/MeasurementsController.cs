using Microsoft.AspNetCore.Mvc;
using Humidity.Application.DTOs;
using Humidity.Application.Interfaces;
using Asp.Versioning; // Важно: используем Asp.Versioning

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
    /// Получить все замеры для указанной машины
    /// </summary>
    /// <param name="vehicleId">Идентификатор машины</param>
    [HttpGet("vehicle/{vehicleId}")]
    [ProducesResponseType(typeof(IEnumerable<MeasurementDto>), 200)]
    public async Task<IActionResult> GetByVehicle(Guid vehicleId)
    {
        var measurements = await _measurementService.GetByVehicleIdAsync(vehicleId);
        return Ok(measurements);
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
    /// Получить все замеры за указанную дату
    /// </summary>
    /// <param name="date">Дата в формате YYYY-MM-DD</param>
    [HttpGet("date/{date}")]
    [ProducesResponseType(typeof(IEnumerable<MeasurementDto>), 200)]
    public async Task<IActionResult> GetByDate(DateTime date)
    {
        var measurements = await _measurementService.GetByDateAsync(date);
        return Ok(measurements);
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
    /// </summary>
    /// <param name="requests">Список запросов на создание</param>
    [HttpPost("bulk")]
    [ProducesResponseType(typeof(IEnumerable<MeasurementDto>), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> BulkCreate([FromBody] IEnumerable<CreateMeasurementRequest> requests)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var created = await _measurementService.BulkCreateAsync(requests);
        return CreatedAtAction(nameof(GetByDate), new { date = DateTime.UtcNow.Date }, created);
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