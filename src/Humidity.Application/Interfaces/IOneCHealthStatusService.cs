using Humidity.Application.Common.Models;

namespace Humidity.Application.Interfaces;

/// <summary>
/// Интерфейс сервиса для отслеживания статуса синхронизации с 1С.
/// </summary>
public interface IOneCHealthStatusService
{
    /// <summary>
    /// Отметить успешное завершение синхронизации.
    /// </summary>
    void ReportSuccess(int processedCount, int addedCount, int updatedCount, int skippedCount, DateTimeOffset from, DateTimeOffset to);

    /// <summary>
    /// Отметить ошибку синхронизации.
    /// </summary>
    void ReportError(string error, DateTimeOffset from, DateTimeOffset to);

    /// <summary>
    /// Отметить начало синхронизации.
    /// </summary>
    void ReportSyncStart();

    /// <summary>
    /// Получить текущий статус здоровья.
    /// </summary>
    OneCHealthStatus GetStatus();
}