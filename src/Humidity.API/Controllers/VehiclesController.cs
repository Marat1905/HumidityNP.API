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
    /// Получить страницу всех машин с возможностью фильтрации
    /// </summary>
    /// <param name="pageNumber">Номер страницы (начиная с 1).</param>
    /// <param name="pageSize">Количество записей на странице (макс. 100).</param>
    /// <param name="counterparty">Фильтр по поставщику (частичное совпадение).</param>
    /// <param name="status">Фильтр по статусу: active, exited, all (по умолчанию active).</param>
    /// <param name="plate">Фильтр по госномеру (частичное совпадение).</param>
    /// <param name="driver">Фильтр по водителю (частичное совпадение).</param>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<VehicleDto>), 200)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? counterparty = null,
        [FromQuery] string? status = "active",
        [FromQuery] string? plate = null,
        [FromQuery] string? driver = null)
    {
        // Нормализация пагинации
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        // Преобразуем статус в bool? 
        bool? isActive = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (status.Equals("active", StringComparison.OrdinalIgnoreCase))
                isActive = true;
            else if (status.Equals("exited", StringComparison.OrdinalIgnoreCase))
                isActive = false;
            // "all" или любое другое значение оставляем null
        }

        var result = await _vehicleService.GetFilteredPagedAsync(
            pageNumber,
            pageSize,
            counterparty,
            isActive,
            plate,
            driver,
            HttpContext.RequestAborted);

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
        var vehicle = await _vehicleService.GetByIdAsync(id, HttpContext.RequestAborted);
        if (vehicle == null)
        {
            return NotFound($"Машина с id {id} не найдена");
        }

        return Ok(vehicle);
    }

    /// <summary>
    /// Получить все активные машины (без пагинации)
    /// </summary>
    [HttpGet("active/all")]
    [ProducesResponseType(typeof(IEnumerable<VehicleDto>), 200)]
    public async Task<IActionResult> GetAllActiveVehicles()
    {
        // Используем существующий метод сервиса, передавая токен отмены текущего HTTP-запроса
        var result = await _vehicleService.GetActiveVehiclesAsync(HttpContext.RequestAborted);

        return Ok(result);
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

        var result = await _vehicleService.GetActiveVehiclesPagedAsync(pageNumber, pageSize, HttpContext.RequestAborted);
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
        // Валидация выполняется автоматически благодаря [ApiController] и FluentValidation.
        var created = await _vehicleService.CreateAsync(request, HttpContext.RequestAborted);
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
        // Валидация выполняется автоматически.
        var updated = await _vehicleService.UpdateAsync(id, request, HttpContext.RequestAborted);
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
        await _vehicleService.DeleteAsync(id, HttpContext.RequestAborted);
        return NoContent();
    }

    /// <summary>
    /// Зафиксировать разгрузку машины: количество тюков, порванных тюков, вес и номер штабеля.
    /// </summary>
    /// <param name="id">Идентификатор машины.</param>
    /// <param name="request">Данные разгрузки.</param>
    [HttpPost("{id}/unload")]
    [ProducesResponseType(typeof(VehicleDto), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Unload(Guid id, [FromBody] UnloadVehicleRequest request)
    {
        // Валидация выполняется автоматически через FluentValidation.
        // Исключение KeyNotFoundException будет обработано глобальным middleware.
        var updated = await _vehicleService.UnloadAsync(id, request, HttpContext.RequestAborted);
        return Ok(updated);
    }
}