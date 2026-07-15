using Asp.Versioning;
using Humidity.Application.DTOs;
using Humidity.Application.Interfaces;
using Humidity.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace Humidity.API.Controllers;

/// <summary>
/// Контроллер для управления записями о машинах
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class VehiclesController : ControllerBase
{
    private readonly IVehicleService _vehicleService;

    public VehiclesController(IVehicleService vehicleService)
    {
        _vehicleService = vehicleService;
    }

    /// <summary>
    /// Получить страницу всех машин
    /// </summary>
    /// <param name="pageNumber">Номер страницы (начиная с 1).</param>
    /// <param name="pageSize">Количество записей на странице (макс. 100).</param>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<VehicleDto>), 200)]
    public async Task<IActionResult> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var result = await _vehicleService.GetPagedAsync(pageNumber, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// Получить машину по идентификатору
    /// </summary>
    /// <param name="id">Идентификатор машины</param>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(VehicleDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var vehicle = await _vehicleService.GetByIdAsync(id);
        if (vehicle == null)
        {
            return NotFound($"Машина с id {id} не найдена");
        }

        return Ok(vehicle);
    }

    /// <summary>
    /// Получить страницу активных машин (которые ещё не выехали)
    /// </summary>
    /// <param name="pageNumber">Номер страницы.</param>
    /// <param name="pageSize">Размер страницы.</param>
    [HttpGet("active")]
    [ProducesResponseType(typeof(PagedResult<VehicleDto>), 200)]
    public async Task<IActionResult> GetActive([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var result = await _vehicleService.GetActiveVehiclesPagedAsync(pageNumber, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// Создать новую запись о машине
    /// </summary>
    /// <param name="request">Данные для создания</param>
    [HttpPost]
    [ProducesResponseType(typeof(VehicleDto), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Create([FromBody] CreateVehicleRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var created = await _vehicleService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>
    /// Обновить существующую запись о машине
    /// </summary>
    /// <param name="id">Идентификатор машины</param>
    /// <param name="request">Данные для обновления</param>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(VehicleDto), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateVehicleRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var updated = await _vehicleService.UpdateAsync(id, request);
        return Ok(updated);
    }

    /// <summary>
    /// Удалить запись о машине
    /// </summary>
    /// <param name="id">Идентификатор машины</param>
    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _vehicleService.DeleteAsync(id);
        return NoContent();
    }
}