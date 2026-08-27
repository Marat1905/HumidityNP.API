// API/BackgroundServices/OneCSyncBackgroundService.cs

using Humidity.Application.Interfaces;
using Humidity.Domain.Entities;
using Humidity.Domain.Interfaces;
using Microsoft.Extensions.Options;

namespace Humidity.API.BackgroundServices;

/// <summary>
/// Фоновый сервис для периодической синхронизации машин с 1С.
/// Выполняет инкрементальную синхронизацию (по расписанию, заданному в настройках)
/// и полную синхронизацию для выверки данных.
/// </summary>
public class OneCSyncBackgroundService : BackgroundService
{
    private readonly ILogger<OneCSyncBackgroundService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly OneCIntegrationSettings _settings;
    private readonly IOneCHealthStatusService _healthStatus;

    // Семафор для предотвращения гонок данных между инкрементальной и полной синхронизацией.
    // Гарантирует, что только один процесс синхронизации может выполняться одновременно.
    private readonly SemaphoreSlim _syncLock = new(1, 1);

    public OneCSyncBackgroundService(
        ILogger<OneCSyncBackgroundService> logger,
        IServiceProvider serviceProvider,
        IOptions<OneCIntegrationSettings> options,
        IOneCHealthStatusService healthStatus)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _settings = options.Value;
        _healthStatus = healthStatus;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Сервис синхронизации с 1С запущен.");

        // ========== ВАЖНО: Запускаем полную синхронизацию сразу при старте ==========
        // Выполняем полную синхронизацию один раз до запуска фоновых циклов,
        // чтобы данные были актуальны с самого начала работы приложения.
        try
        {
            var now = DateTimeOffset.UtcNow;
            var from = now.AddDays(-_settings.FullSyncFetchDays);
            var to = now;
            _logger.LogInformation("Запуск первоначальной полной синхронизации за период с {From} по {To}", from, to);
            await SyncVehiclesAsync(from, to, stoppingToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Ошибка при первоначальной полной синхронизации.");
        }

        // Запускаем две независимые задачи: инкрементальную и полную синхронизацию
        var incrementalTask = RunIncrementalSync(stoppingToken);
        var fullSyncTask = RunFullSync(stoppingToken);

        await Task.WhenAll(incrementalTask, fullSyncTask);
    }

    /// <summary>
    /// Циклически выполняет инкрементальную синхронизацию с заданным интервалом.
    /// </summary>
    private async Task RunIncrementalSync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Ждём интервал между запусками
                await Task.Delay(TimeSpan.FromMinutes(_settings.IncrementalIntervalMinutes), stoppingToken);

                var now = DateTimeOffset.UtcNow;
                var from = now.AddHours(-_settings.IncrementalFetchHours);
                var to = now;

                _logger.LogInformation("Запуск инкрементальной синхронизации за период с {From} по {To}", from, to);

                await SyncVehiclesAsync(from, to, stoppingToken);
            }
            catch (OperationCanceledException ex) when (ex.CancellationToken == stoppingToken)
            {
                // Это штатная отмена при остановке приложения (например, при деплое или перезапуске)
                _logger.LogInformation("Синхронизация корректно остановлена.");
                break;
            }
            catch (Exception ex)
            {
                // Перехватываем ВСЕ остальные ошибки, включая TaskCanceledException от таймаута HttpClient.
                // Цикл продолжит работу и попытается снова через заданный интервал.
                _logger.LogError(ex, "Ошибка при выполнении инкрементальной синхронизации. Будет предпринята повторная попытка через {Minutes} мин.", _settings.IncrementalIntervalMinutes);
            }
        }
    }

    /// <summary>
    /// Циклически выполняет полную синхронизацию с заданным интервалом.
    /// </summary>
    private async Task RunFullSync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Ждём интервал перед следующим запуском (12 часов по умолчанию)
                await Task.Delay(TimeSpan.FromHours(_settings.FullSyncIntervalHours), stoppingToken);

                var now = DateTimeOffset.UtcNow;
                var from = now.AddDays(-_settings.FullSyncFetchDays);
                var to = now;

                _logger.LogInformation("Запуск полной синхронизации за период с {From} по {To}", from, to);

                await SyncVehiclesAsync(from, to, stoppingToken);
            }
            catch (OperationCanceledException ex) when (ex.CancellationToken == stoppingToken)
            {
                // Это штатная отмена при остановке приложения (например, при деплое или перезапуске)
                _logger.LogInformation("Синхронизация корректно остановлена.");
                break;
            }
            catch (Exception ex)
            {
                // Перехватываем ВСЕ остальные ошибки, включая TaskCanceledException от таймаута HttpClient.
                // Цикл продолжит работу и попытается снова через заданный интервал.
                _logger.LogError(ex, "Ошибка при выполнении полной синхронизации. Будет предпринята повторная попытка через {Hours} ч.", _settings.FullSyncIntervalHours);
            }
        }
    }

    /// <summary>
    /// Основной метод синхронизации: получает данные из 1С и обновляет/добавляет записи в БД.
    /// Реализует строгий контроль уникальности по полю OneCGuid.
    /// </summary>
    private async Task SyncVehiclesAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken)
    {
        // Используем семафор для блокировки параллельных запусков синхронизации (Fix Race Conditions)
        await _syncLock.WaitAsync(cancellationToken);
        _healthStatus.ReportSyncStart();

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var oneCClient = scope.ServiceProvider.GetRequiredService<IOneCClient>();
            var vehicleRepository = scope.ServiceProvider.GetRequiredService<IVehicleRepository>();

            try
            {
                // Получаем список машин из 1С
                var vehiclesFromOneC = await oneCClient.GetVehiclesAsync(from, to, cancellationToken);

                _logger.LogInformation("Получено {Count} машин из 1С за период с {From} по {To}",
                    vehiclesFromOneC.Count(), from, to);

                var processedCount = 0;
                var addedCount = 0;
                var updatedCount = 0;
                var skippedCount = 0;

                foreach (var oneCVehicle in vehiclesFromOneC)
                {
                    // Проверяем обязательные поля
                    if (string.IsNullOrEmpty(oneCVehicle.Number))
                    {
                        _logger.LogWarning("Пропущена запись без номера пропуска.");
                        skippedCount++;
                        continue;
                    }

                    // 1. КОНТРОЛЬ УНИКАЛЬНОСТИ: Ищем существующую машину по ГУИД (приоритетный метод)
                    Vehicle? existing = null;
                    if (!string.IsNullOrWhiteSpace(oneCVehicle.OneCGuid))
                    {
                        existing = await vehicleRepository.GetByOneCGuidAsync(oneCVehicle.OneCGuid, cancellationToken);
                    }

                    // 2. Если по GUID не найдено, пробуем найти по номеру пропуска и дате (для обратной совместимости со старыми записями)
                    if (existing == null)
                    {
                        existing = await vehicleRepository.GetByNumberAndDateAsync(
                            oneCVehicle.Number,
                            oneCVehicle.Date,
                            cancellationToken);
                    }

                    if (existing == null)
                    {
                        // Создаём новую запись
                        var newVehicle = new Vehicle
                        {
                            Id = Guid.NewGuid(),
                            Number = oneCVehicle.Number,
                            Date = oneCVehicle.Date,
                            EntryDate = oneCVehicle.EntryDate,
                            ExitDate = oneCVehicle.ExitDate,
                            Counterparty = oneCVehicle.Counterparty,
                            Inn = oneCVehicle.Inn,
                            VehicleBrand = oneCVehicle.VehicleBrand,
                            VehiclePlate = oneCVehicle.VehiclePlate,
                            Trailer = oneCVehicle.Trailer,
                            Driver = oneCVehicle.Driver,
                            OneCGuid = oneCVehicle.OneCGuid
                        };

                        await vehicleRepository.AddAsync(newVehicle, cancellationToken);
                        addedCount++;
                        _logger.LogDebug("Добавлена новая машина: {Number} {Date} (GUID: {OneCGuid})", oneCVehicle.Number, oneCVehicle.Date, oneCVehicle.OneCGuid);
                    }
                    else
                    {
                        // Проверяем, изменились ли поля (кроме Id, CreatedAt, UpdatedAt)
                        bool needUpdate = false;

                        if (existing.Number != oneCVehicle.Number)
                        {
                            existing.Number = oneCVehicle.Number;
                            needUpdate = true;
                        }

                        if (existing.Date != oneCVehicle.Date)
                        {
                            existing.Date = oneCVehicle.Date;
                            needUpdate = true;
                        }

                        if (existing.EntryDate != oneCVehicle.EntryDate)
                        {
                            existing.EntryDate = oneCVehicle.EntryDate;
                            needUpdate = true;
                        }

                        if (existing.ExitDate != oneCVehicle.ExitDate)
                        {
                            existing.ExitDate = oneCVehicle.ExitDate;
                            needUpdate = true;
                        }

                        if (existing.Counterparty != oneCVehicle.Counterparty)
                        {
                            existing.Counterparty = oneCVehicle.Counterparty;
                            needUpdate = true;
                        }

                        if (existing.Inn != oneCVehicle.Inn)
                        {
                            existing.Inn = oneCVehicle.Inn;
                            needUpdate = true;
                        }

                        if (existing.VehicleBrand != oneCVehicle.VehicleBrand)
                        {
                            existing.VehicleBrand = oneCVehicle.VehicleBrand;
                            needUpdate = true;
                        }

                        if (existing.VehiclePlate != oneCVehicle.VehiclePlate)
                        {
                            existing.VehiclePlate = oneCVehicle.VehiclePlate;
                            needUpdate = true;
                        }

                        if (existing.Trailer != oneCVehicle.Trailer)
                        {
                            existing.Trailer = oneCVehicle.Trailer;
                            needUpdate = true;
                        }

                        if (existing.Driver != oneCVehicle.Driver)
                        {
                            existing.Driver = oneCVehicle.Driver;
                            needUpdate = true;
                        }

                        // Обновляем GUID, если он появился или изменился (например, был пустой, а теперь 1С начала его передавать)
                        if (existing.OneCGuid != oneCVehicle.OneCGuid)
                        {
                            existing.OneCGuid = oneCVehicle.OneCGuid;
                            needUpdate = true;
                        }

                        if (needUpdate)
                        {
                            await vehicleRepository.UpdateAsync(existing, cancellationToken);
                            updatedCount++;
                            _logger.LogDebug("Обновлена машина: {Number} {Date} (GUID: {OneCGuid})", oneCVehicle.Number, oneCVehicle.Date, oneCVehicle.OneCGuid);
                        }
                    }

                    processedCount++;
                }

                _logger.LogInformation("Синхронизация завершена. Обработано {Processed}, добавлено {Added}, обновлено {Updated}, пропущено {Skipped}",
                    processedCount, addedCount, updatedCount, skippedCount);

                // Обновляем статус здоровья
                _healthStatus.ReportSuccess(processedCount, addedCount, updatedCount, skippedCount, from, to);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при синхронизации данных с 1С.");
                // Обновляем статус здоровья при ошибке
                _healthStatus.ReportError(ex.Message, from, to);
                throw; // Пробрасываем, чтобы внешний обработчик залогировал
            }
        }
        finally
        {
            // Освобождаем семафор, чтобы разрешить следующий запуск синхронизации
            _syncLock.Release();
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Сервис синхронизации с 1С останавливается.");
        _syncLock.Dispose();
        await base.StopAsync(cancellationToken);
    }
}