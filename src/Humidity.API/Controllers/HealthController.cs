using Humidity.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Humidity.API.Controllers;

/// <summary>
/// Контроллер для мониторинга здоровья интеграции с 1С.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
//[Authorize(Policy = "TCXPolicy")]
public class HealthController : ControllerBase
{
    private readonly IOneCHealthStatusService _healthStatus;

    public HealthController(IOneCHealthStatusService healthStatus)
    {
        _healthStatus = healthStatus;
    }

    /// <summary>
    /// Возвращает статус последней синхронизации с 1С (время, количество ошибок, статистику).
    /// Позволяет операторам мониторить здоровье интеграции без просмотра логов.
    /// </summary>
    [HttpGet("1c")]
    public IActionResult Get1CHealth()
    {
        return Ok(_healthStatus.GetStatus());
    }
}