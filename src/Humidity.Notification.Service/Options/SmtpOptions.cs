namespace Humidity.Notification.Service.Options;

/// <summary>
/// Настройки SMTP-сервера, через который отправляются письма.
/// </summary>
public class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 1025;
    public string From { get; set; } = "humidity@example.com";
    public string? Username { get; set; }
    public string? Password { get; set; }
    public bool UseSsl { get; set; } = false;
}

/// <summary>
/// Список получателей писем по умолчанию.
/// </summary>
public class RecipientsOptions
{
    public const string SectionName = "Recipients";

    /// <summary>
    /// Список email через запятую.
    /// </summary>
    public string Default { get; set; } = string.Empty;

    /// <summary>
    /// Разворачивает строку с email через запятую в массив.
    /// </summary>
    public IEnumerable<string> GetRecipients()
    {
        return Default
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(email => !string.IsNullOrWhiteSpace(email));
    }
}