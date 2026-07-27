using Asp.Versioning;
using Humidity.Application.DTOs;
using Humidity.Application.Interfaces;
using Humidity.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace Humidity.API.Controllers;

/// <summary>
/// Контроллер для работы с аналитикой по поставщикам (группировка по ИНН).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
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
    /// <param name="from">Начало периода (включительно) в формате ISO 8601.</param>
    /// <param name="to">Конец периода (включительно) в формате ISO 8601.</param>
    /// <param name="pageNumber">Номер страницы (начиная с 1).</param>
    /// <param name="pageSize">Размер страницы (макс. 100).</param>
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
    /// </summary>
    /// <param name="inn">ИНН поставщика.</param>
    /// <param name="from">Начало периода (включительно).</param>
    /// <param name="to">Конец периода (включительно).</param>
    [HttpGet("{inn}/details")]
    [ProducesResponseType(typeof(SupplierDetailsDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetSupplierDetails(
        string inn,
        [FromQuery] DateTimeOffset from,
        [FromQuery] DateTimeOffset to)
    {
        var details = await _supplierService.GetSupplierDetailsAsync(inn, from, to, HttpContext.RequestAborted);
        if (details.Vehicles.Count == 0)
        {
            return NotFound($"Поставщик с ИНН {inn} не найден за указанный период");
        }
        return Ok(details);
    }
}