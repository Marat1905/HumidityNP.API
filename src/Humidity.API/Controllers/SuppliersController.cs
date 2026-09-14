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
    ///
    /// Параметр <paramref name="search"/> позволяет фильтровать поставщиков по частичному
    /// совпадению ИНН или наименования (регистронезависимо). Пустая строка или отсутствие
    /// параметра — поиск не применяется.
    /// </summary>
    /// <param name="from">Начало периода.</param>
    /// <param name="to">Конец периода.</param>
    /// <param name="pageNumber">Номер страницы (начиная с 1).</param>
    /// <param name="pageSize">Размер страницы (макс. 100).</param>
    /// <param name="search">Строка поиска по ИНН или наименованию поставщика.</param>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<SupplierDto>), 200)]
    public async Task<IActionResult> GetSuppliers(
        [FromQuery] DateTimeOffset from,
        [FromQuery] DateTimeOffset to,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var result = await _supplierService.GetSuppliersAsync(
            from, to, pageNumber, pageSize, search, HttpContext.RequestAborted);

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
    /// Получить топ-N поставщиков по средней влажности за период с байесовской коррекцией.
    ///
    /// БАЙЕСОВСКАЯ КОРРЕКЦИЯ:
    /// Наивная средняя влажности плохо работает при малом числе замеров: поставщик с 3 замерами
    /// может случайно оказаться «лучшим», а с 100 — «средним». Чтобы избежать этого, применяется
    /// credibility adjustment:
    ///
    ///     adjusted_i = (C * m + sum_i) / (C + n_i)
    ///
    /// где:
    ///   - m — глобальная средняя влажность по всем замерам за период;
    ///   - C — priorWeight (вес prior, «сколько виртуальных замеров со средней m»);
    ///   - sum_i — сумма влажностей у поставщика i;
    ///   - n_i — количество замеров у поставщика i.
    ///
    /// Чем меньше замеров у поставщика, тем сильнее его средняя тянется к m.
    /// </summary>
    /// <param name="from">Начало периода (включительно).</param>
    /// <param name="to">Конец периода (включительно).</param>
    /// <param name="top">Количество поставщиков в топе (по умолчанию 10, максимум 100).</param>
    /// <param name="order">Порядок сортировки: 'asc' — хорошие (низкая влажность), 'desc' — плохие (высокая).</param>
    /// <param name="priorWeight">
    /// Вес prior для байесовской коррекции. Значение по умолчанию — 30.
    /// Допустимые значения: 0 (без коррекции), 10 (слабая), 30 (умеренная), 100 (сильная).
    /// Ограничивается диапазоном [0; 1000].
    /// </param>
    [HttpGet("top")]
    [ProducesResponseType(typeof(IEnumerable<SupplierDto>), 200)]
    public async Task<IActionResult> GetTopSuppliers(
        [FromQuery] DateTimeOffset from,
        [FromQuery] DateTimeOffset to,
        [FromQuery] int top = 10,
        [FromQuery] string order = "asc",
        [FromQuery] double priorWeight = 30)
    {
        if (top < 1) top = 1;
        if (top > 100) top = 100;

        // Ограничиваем priorWeight разумными рамками.
        // Отрицательный вес не имеет смысла (увеличивал бы разброс),
        // слишком большой (тысячи) — превращает все средние в глобальную m, топ становится бесполезным.
        if (priorWeight < 0) priorWeight = 0;
        if (priorWeight > 1000) priorWeight = 1000;

        bool ascending = order?.ToLower() == "asc";

        var result = await _supplierService.GetTopSuppliersAsync(
            top, ascending, from, to, priorWeight, HttpContext.RequestAborted);

        return Ok(result);
    }
}