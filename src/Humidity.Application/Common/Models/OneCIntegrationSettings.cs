/// <summary>
/// Настройки для интеграции с 1С.
/// </summary>
public class OneCIntegrationSettings
{
    /// <summary>
    /// URL SOAP-сервиса 1С.
    /// </summary>
    public string ServiceUrl { get; set; } = string.Empty;

    /// <summary>
    /// Имя пользователя для базовой аутентификации.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Пароль для базовой аутентификации.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Количество повторных попыток при ошибках сети.
    /// </summary>
    public int RetryCount { get; set; } = 3;

    /// <summary>
    /// Базовая задержка между попытками (в секундах), используется экспоненциальная задержка.
    /// </summary>
    public double RetryBaseDelaySeconds { get; set; } = 2.0;

    /// <summary>
    /// Интервал инкрементальной синхронизации (в минутах).
    /// </summary>
    public int IncrementalIntervalMinutes { get; set; } = 5;

    /// <summary>
    /// Глубина инкрементальной синхронизации (в часах) – сколько часов данных запрашивать.
    /// </summary>
    public int IncrementalFetchHours { get; set; } = 4;

    /// <summary>
    /// Интервал полной синхронизации (в часах).
    /// </summary>
    public int FullSyncIntervalHours { get; set; } = 12;

    /// <summary>
    /// Глубина полной синхронизации (в днях) – сколько дней данных запрашивать.
    /// </summary>
    public int FullSyncFetchDays { get; set; } = 30;

    /// <summary>
    /// Идентификатор часового пояса, используемого сервером 1С (например, "Russian Standard Time" для Windows или "Europe/Moscow" для Linux).
    /// Будет использован для преобразования UTC-времени приложения в локальное время 1С при формировании запроса,
    /// а также для преобразования локальных дат из ответа 1С обратно в UTC.
    /// </summary>
    public string TimeZoneId { get; set; } = "Russian Standard Time";
}