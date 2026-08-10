using Humidity.Application.Common.Models;
using Humidity.Application.Interfaces;

namespace Humidity.Application.Services;

/// <summary>
/// Реализация сервиса для отслеживания статуса синхронизации с 1С (Singleton).
/// Хранит состояние в памяти и защищает его от гонок данных при чтении/записи.
/// </summary>
public class OneCHealthStatusService : IOneCHealthStatusService
{
    private readonly OneCHealthStatus _status = new();
    private readonly object _lock = new();

    public void ReportSuccess(int processedCount, int addedCount, int updatedCount, int skippedCount, DateTimeOffset from, DateTimeOffset to)
    {
        lock (_lock)
        {
            _status.LastSuccessfulSync = DateTimeOffset.UtcNow;
            _status.LastSyncPeriodFrom = from.ToString("O");
            _status.LastSyncPeriodTo = to.ToString("O");
            _status.TotalProcessed += processedCount;
            _status.TotalAdded += addedCount;
            _status.TotalUpdated += updatedCount;
            _status.TotalSkipped += skippedCount;
            _status.LastError = null;
            _status.IsSyncing = false;
        }
    }

    public void ReportError(string error, DateTimeOffset from, DateTimeOffset to)
    {
        lock (_lock)
        {
            _status.LastFailedSync = DateTimeOffset.UtcNow;
            _status.LastSyncPeriodFrom = from.ToString("O");
            _status.LastSyncPeriodTo = to.ToString("O");
            _status.LastError = error;
            _status.IsSyncing = false;
        }
    }

    public void ReportSyncStart()
    {
        lock (_lock)
        {
            _status.IsSyncing = true;
        }
    }

    public OneCHealthStatus GetStatus()
    {
        lock (_lock)
        {
            // Возвращаем копию объекта, чтобы избежать гонок данных при чтении из контроллера
            return new OneCHealthStatus
            {
                LastSuccessfulSync = _status.LastSuccessfulSync,
                LastFailedSync = _status.LastFailedSync,
                LastError = _status.LastError,
                TotalProcessed = _status.TotalProcessed,
                TotalAdded = _status.TotalAdded,
                TotalUpdated = _status.TotalUpdated,
                TotalSkipped = _status.TotalSkipped,
                LastSyncPeriodFrom = _status.LastSyncPeriodFrom,
                LastSyncPeriodTo = _status.LastSyncPeriodTo,
                IsSyncing = _status.IsSyncing
            };
        }
    }
}