using Microsoft.AspNetCore.Mvc;
using System.Reflection;

namespace Humidity.API.Controllers;


/// <summary>
/// Контроллер для получения информации о версии запущенного приложения.
/// </summary>
[ApiController]
[Route("humidity/api/v{version:apiVersion}/[controller]")]
public class VersionsController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(VersionResponse), StatusCodes.Status200OK)]
    public IActionResult GetVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var assemblyName = assembly.GetName();

        var response = new VersionResponse
        {
            ApplicationName = assemblyName.Name ?? "Humidity.API",
            // Приоритет: переменная из Docker -> версия сборки .NET
            Version = Environment.GetEnvironmentVariable("APP_VERSION")
                      ?? assemblyName.Version?.ToString()
                      ?? "Unknown",
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
            // Хэш коммита из GitHub Actions
            GitCommit = Environment.GetEnvironmentVariable("GIT_COMMIT") ?? "N/A",
            BuildDate = GetBuildDate(assembly)
        };

        return Ok(response);
    }

    private static string GetBuildDate(Assembly assembly)
    {
        try
        {
            var location = assembly.Location;
            if (string.IsNullOrEmpty(location) || !System.IO.File.Exists(location))
                return "Unknown";

            return System.IO.File.GetLastWriteTimeUtc(location).ToString("yyyy-MM-dd HH:mm:ss UTC");
        }
        catch
        {
            return "Unknown";
        }
    }
}