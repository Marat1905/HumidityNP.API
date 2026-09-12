using Asp.Versioning;
using Humidity.Application.Interfaces;
using Humidity.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace Humidity.API.Controllers;

/// <summary>
/// Контроллер для работы с аналитикой по поставщикам (группировка по ИНН).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("humidity/api/v{version:apiVersion}/[controller]")]
public class SuppliersController : ControllerBase
{
    private readonly ISupplierService _supplierService;

    public SuppliersController(ISupplierService supplierService)
    {
        _supplierService = supplierService;
    }

    /// <summary>
    /// Получить список поставщиков с агрегированными данными за период (пагинированный).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<SupplierDto>), 200)]
    public async Task<IActionResult> GetSuppliers(
        [FromQuery] DateTimeOffset from,
        [FromQuery] DateTimeOffset to,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var result = await _supplierService.GetSuppliersAsync(from, to, pageNumber, pageSize, HttpContext.RequestAborted);
        return Ok(result);
    }

    /// <summary>
    /// Получить детальную информацию по поставщику (ИНН) за период.
    /// Пагинация и сортировка выполняются на стороне сервера.
    /// </summary>
    /// <param name="inn">ИНН поставщика.</param>
    /// <param name="from">Начало периода.</param>
    /// <param name="to">Конец периода.</param>
    /// <param name="pageNumber">Номер страницы (начиная с 1).</param>
    /// <param name="pageSize">Размер страницы (макс. 100).</param>
    /// <param name="order">Порядок сортировки по дате въезда: 'desc' — новые сверху (по умолчанию), 'asc' — старые сверху.</param>
    [HttpGet("{inn}/details")]
    [ProducesResponseType(typeof(SupplierDetailsDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetSupplierDetails(
        string inn,
        [FromQuery] DateTimeOffset from,
        [FromQuery] DateTimeOffset to,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string order = "desc")
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        // 'asc' — по возрастанию, всё остальное (включая 'desc') — по убыванию
        bool sortDescending = !string.Equals(order, "asc", StringComparison.OrdinalIgnoreCase);

        var details = await _supplierService.GetSupplierDetailsAsync(
            inn, from, to, pageNumber, pageSize, sortDescending, HttpContext.RequestAborted);

        if (details.Vehicles.TotalCount == 0)
        {
            return NotFound($"Поставщик с ИНН {inn} не найден за указанный период");
        }
        return Ok(details);
    }

    /// <summary>
    /// Получить полный (без пагинации) список машин поставщика за период,
    /// отсортированный по дате въезда. Используется для построения графика.
    /// </summary>
    /// <param name="inn">ИНН поставщика.</param>
    /// <param name="from">Начало периода.</param>
    /// <param name="to">Конец периода.</param>
    /// <param name="order">Порядок сортировки по дате въезда: 'desc' — новые сверху (по умолчанию), 'asc' — старые сверху.</param>
    [HttpGet("{inn}/chart")]
    [ProducesResponseType(typeof(IEnumerable<SupplierVehicleSummaryDto>), 200)]
    public async Task<IActionResult> GetSupplierVehiclesForChart(
        string inn,
        [FromQuery] DateTimeOffset from,
        [FromQuery] DateTimeOffset to,
        [FromQuery] string order = "desc")
    {
        bool sortDescending = !string.Equals(order, "asc", StringComparison.OrdinalIgnoreCase);

        var result = await _supplierService.GetSupplierVehiclesForChartAsync(
            inn, from, to, sortDescending, HttpContext.RequestAborted);

        return Ok(result);
    }

    /// <summary>
    /// Получить топ-N поставщиков по средней влажности за период.
    /// </summary>
    /// <param name="from">Начало периода (включительно).</param>
    /// <param name="to">Конец периода (включительно).</param>
    /// <param name="top">Количество поставщиков в топе (по умолчанию 10).</param>
    /// <param name="order">Порядок сортировки: 'asc' — хорошие (низкая влажность), 'desc' — плохие (высокая).</param>
    [HttpGet("top")]
    [ProducesResponseType(typeof(IEnumerable<SupplierDto>), 200)]
    public async Task<IActionResult> GetTopSuppliers(
        [FromQuery] DateTimeOffset from,
        [FromQuery] DateTimeOffset to,
        [FromQuery] int top = 10,
        [FromQuery] string order = "asc")
    {
        if (top < 1) top = 1;
        if (top > 100) top = 100;

        bool ascending = order?.ToLower() == "asc";
        var result = await _supplierService.GetTopSuppliersAsync(top, ascending, from, to, HttpContext.RequestAborted);
        return Ok(result);
    }
}