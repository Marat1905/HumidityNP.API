using System.Text.Json;
using System.Text.Json.Serialization;

namespace Humidity.Contracts.Serialization;

/// <summary>
/// Единые настройки JSON для обмена событиями между сервисами.
///
/// Зачем:
///   • PropertyNamingPolicy = CamelCase — события сериализуются в camelCase,
///     что соответствует конвенции REST-ответов и удобно для внешних подписчиков;
///   • PropertyNameCaseInsensitive = true — при десериализации не важно,
///     пришёл camelCase (наш publisher) или PascalCase (ручной тест или
///     сторонний продюсер). Это устраняет ошибку «поля пришли пустыми»;
///   • DefaultIgnoreCondition = WhenWritingNull — не передаём null-поля
///     в JSON, что уменьшает размер сообщения.
///
/// Используется и в publisher (Humidity.API), и в consumer
/// (Humidity.Notification.Service), чтобы гарантировать симметрию.
/// </summary>
public static class JsonDefaults
{
    /// <summary>
    /// Единый экземпляр настроек. System.Text.Json рекомендует
    /// переиспользовать один JsonSerializerOptions для кэширования
    /// метаданных — это быстрее, чем создавать каждый раз новый.
    /// </summary>
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };
}