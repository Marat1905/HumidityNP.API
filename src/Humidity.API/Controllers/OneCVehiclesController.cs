using Asp.Versioning;
using Humidity.Application.Interfaces;
using Humidity.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace Humidity.API.Controllers;

/// <summary>
/// Контроллер для интеграции с 1С.
/// Предоставляет данные о разгрузке машин и средней влажности.
///
/// ВАЖНО ПРО ЧАСОВЫЕ ПОЯСА:
/// Сервер 1С работает в часовом поясе Екатеринбурга (UTC+5).
/// В базе данных приложения все даты хранятся в UTC.
/// Все входящие даты в эндпоинтах этого контроллера интерпретируются
/// как локальное время 1С и автоматически преобразуются в UTC внутри сервиса.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("humidity/api/v{version:apiVersion}/[controller]")]
public class OneCVehiclesController : ControllerBase
{
    private readonly IVehicleService _vehicleService;

    public OneCVehiclesController(IVehicleService vehicleService)
    {
        _vehicleService = vehicleService;
    }

    /// <summary>
    /// Получить информацию о разгрузке машины и среднюю влажность по уникальному идентификатору 1С (ГУИД).
    /// </summary>
    /// <param name="oneCGuid">Уникальный идентификатор записи из 1С.</param>
    /// <returns>
    /// DTO с полями:
    ///  - OneCGuid;
    ///  - BaleCount — количество выгруженных тюков;
    ///  - DamagedBaleCount — количество порванных тюков;
    ///  - WeightKg — вес выгруженного груза в килограммах;
    ///  - StackNumber — номер штабеля;
    ///  - AverageHumidity — средняя влажность по замерам машины.
    /// </returns>
    [HttpGet("unload-info/by-guid/{oneCGuid}")]
    [ProducesResponseType(typeof(OneCVehicleUnloadDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetUnloadInfoByOneCGuid(string oneCGuid)
    {
        var result = await _vehicleService.GetUnloadInfoByOneCGuidAsync(oneCGuid, HttpContext.RequestAborted);

        if (result == null)
        {
            return NotFound($"Машина с OneCGuid {oneCGuid} не найдена");
        }

        return Ok(result);
    }

    /// <summary>
    /// Получить информацию о разгрузке машин и среднюю влажность за период.
    /// Период фильтруется по дате создания пропуска (Vehicle.Date).
    ///
    /// ВАЖНО: параметры from и to интерпретируются как локальное время 1С
    /// (часовой пояс Екатеринбурга, UTC+5). Внутри сервиса они преобразуются в UTC
    /// перед сравнением с Vehicle.Date, которое хранится в UTC.
    ///
    /// Примеры корректных запросов:
    ///   ?from=2024-01-01T00:00:00&amp;to=2024-01-31T23:59:59
    ///   ?from=2024-01-01T00:00:00%2B05:00&amp;to=2024-01-31T23:59:59%2B05:00
    /// </summary>
    /// <param name="from">Начало периода (включительно) в локальном времени 1С.</param>
    /// <param name="to">Конец периода (включительно) в локальном времени 1С.</param>
    /// <returns>
    /// Коллекция DTO с полями:
    ///  - OneCGuid;
    ///  - BaleCount — количество выгруженных тюков;
    ///  - DamagedBaleCount — количество порванных тюков;
    ///  - WeightKg — вес выгруженного груза в килограммах;
    ///  - StackNumber — номер штабеля;
    ///  - AverageHumidity — средняя влажность по замерам машины.
    /// </returns>
    [HttpGet("unload-info/by-period")]
    [ProducesResponseType(typeof(IEnumerable<OneCVehicleUnloadDto>), 200)]
    public async Task<IActionResult> GetUnloadInfoByPeriod(
        [FromQuery] DateTimeOffset from,
        [FromQuery] DateTimeOffset to)
    {
        var result = await _vehicleService.GetUnloadInfoByPeriodAsync(from, to, HttpContext.RequestAborted);
        return Ok(result);
    }
}