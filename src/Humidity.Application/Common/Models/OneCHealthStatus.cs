namespace Humidity.Application.Common.Models;

/// <summary>
/// Модель состояния здоровья интеграции с 1С.
/// </summary>
public class OneCHealthStatus
{
    /// <summary>
    /// Время последней успешной синхронизации.
    /// </summary>
    public DateTimeOffset? LastSuccessfulSync { get; set; }

    /// <summary>
    /// Время последней неудачной синхронизации.
    /// </summary>
    public DateTimeOffset? LastFailedSync { get; set; }

    /// <summary>
    /// Текст последней ошибки (если была).
    /// </summary>
    public string? LastError { get; set; }

    /// <summary>
    /// Общее количество обработанных записей за всё время работы.
    /// </summary>
    public int TotalProcessed { get; set; }

    /// <summary>
    /// Общее количество добавленных записей.
    /// </summary>
    public int TotalAdded { get; set; }

    /// <summary>
    /// Общее количество обновлённых записей.
    /// </summary>
    public int TotalUpdated { get; set; }

    /// <summary>
    /// Общее количество пропущенных записей.
    /// </summary>
    public int TotalSkipped { get; set; }

    /// <summary>
    /// Начало периода последней синхронизации.
    /// </summary>
    public string? LastSyncPeriodFrom { get; set; }

    /// <summary>
    /// Конец периода последней синхронизации.
    /// </summary>
    public string? LastSyncPeriodTo { get; set; }

    /// <summary>
    /// Флаг, указывающий, выполняется ли синхронизация прямо сейчас.
    /// </summary>
    public bool IsSyncing { get; set; }
}