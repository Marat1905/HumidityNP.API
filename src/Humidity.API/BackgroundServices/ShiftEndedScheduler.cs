using Humidity.API.Services;
using Humidity.Application.Interfaces;
using Humidity.Contracts.Events;
using Microsoft.Extensions.Options;

namespace Humidity.API.BackgroundServices;

/// <summary>
/// Настройки детектора окончания смены.
/// </summary>
public class ShiftSchedulerOptions
{
    public const string SectionName = "ShiftScheduler";

    /// <summary>
    /// Идентификатор часового пояса площадки (IANA или Windows ID).
    /// </summary>
    public string TimeZoneId { get; set; } = "Ekaterinburg Standard Time";

    /// <summary>
    /// Час начала дневной смены (локальное время).
    /// </summary>
    public int DayShiftStartHour { get; set; } = 8;

    /// <summary>
    /// Час начала ночной смены (локальное время).
    /// </summary>
    public int NightShiftStartHour { get; set; } = 20;

    /// <summary>
    /// Интервал опроса планировщика (в секундах). Каждые N секунд проверяем,
    /// не пересекли ли мы границу смены.
    /// </summary>
    public int PollIntervalSeconds { get; set; } = 30;
}

/// <summary>
/// Фоновый сервис, который определяет момент окончания смены
/// (в 08:00 или 20:00 локального времени) и публикует
/// ShiftEndedEvent в RabbitMQ. Дополнительно уведомляет SignalR.
///
/// Идемпотентность: в памяти храним последнюю опубликованную границу смены,
/// чтобы не публиковать событие повторно.
/// </summary>
public class ShiftEndedScheduler : BackgroundService
{
    private readonly ILogger<ShiftEndedScheduler> _logger;
    private readonly IRabbitMqPublisher _publisher;
    private readonly IHumidityRealtimeNotifier _realtime;
    private readonly ShiftSchedulerOptions _options;
    private readonly TimeZoneInfo _timeZone;

    private DateTimeOffset _lastPublishedBoundary = DateTimeOffset.MinValue;

    public ShiftEndedScheduler(
        ILogger<ShiftEndedScheduler> logger,
        IRabbitMqPublisher publisher,
        IHumidityRealtimeNotifier realtime,
        IOptions<ShiftSchedulerOptions> options)
    {
        _logger = logger;
        _publisher = publisher;
        _realtime = realtime;
        _options = options.Value;

        try
        {
            _timeZone = TimeZoneInfo.FindSystemTimeZoneById(_options.TimeZoneId);
        }
        catch
        {
            _logger.LogWarning("Таймзона {Tz} не найдена, используется UTC.", _options.TimeZoneId);
            _timeZone = TimeZoneInfo.Utc;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "ShiftEndedScheduler запущен. Таймзона: {Tz}, дневная смена: {DayStart}:00, ночная: {NightStart}:00",
            _timeZone.Id, _options.DayShiftStartHour, _options.NightShiftStartHour);

        // Инициализируем _lastPublishedBoundary, чтобы после первого запуска
        // не публиковать событие задним числом за прошлую смену.
        _lastPublishedBoundary = GetMostRecentBoundary(DateTimeOffset.UtcNow);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckBoundaryAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка в ShiftEndedScheduler.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(_options.PollIntervalSeconds), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    /// <summary>
    /// Проверяет, не пересекли ли мы границу смены с момента последней проверки.
    /// </summary>
    private async Task CheckBoundaryAsync(CancellationToken ct)
    {
        var nowUtc = DateTimeOffset.UtcNow;
        var latestBoundary = GetMostRecentBoundary(nowUtc);

        // Если самая свежая граница новее той, что мы уже опубликовали,
        // значит мы её только что пересекли.
        if (latestBoundary > _lastPublishedBoundary)
        {
            // Определяем тип смены, которая ТОЛЬКО ЧТО ЗАКОНЧИЛАСЬ.
            // Границы:
            //   day shift   08:00 – 20:00, заканчивается в 20:00
            //   night shift 20:00 – 08:00, заканчивается в 08:00
            var localBoundary = TimeZoneInfo.ConvertTime(latestBoundary, _timeZone);
            var endedShiftType = localBoundary.Hour == _options.NightShiftStartHour
                ? "day"     // в 20:00 закончилась дневная
                : "night";  // в 08:00 закончилась ночная

            // Вычисляем начало и конец завершившейся смены.
            DateTimeOffset shiftStart;
            DateTimeOffset shiftEnd = latestBoundary;

            if (endedShiftType == "day")
            {
                shiftStart = new DateTimeOffset(
                    localBoundary.Date.AddHours(_options.DayShiftStartHour),
                    localBoundary.Offset);
            }
            else
            {
                // Ночная началась вчера в 20:00
                shiftStart = new DateTimeOffset(
                    localBoundary.Date.AddDays(-1).AddHours(_options.NightShiftStartHour),
                    localBoundary.Offset);
            }

            var evt = new ShiftEndedEvent
            {
                EventId = Guid.NewGuid(),
                ShiftType = endedShiftType,
                ShiftStart = shiftStart.ToUniversalTime(),
                ShiftEnd = shiftEnd.ToUniversalTime(),
                PublishedAt = DateTimeOffset.UtcNow
            };

            _logger.LogInformation(
                "Обнаружено окончание смены {ShiftType}: {Start} — {End}. Публикуем событие {EventId}.",
                endedShiftType, evt.ShiftStart, evt.ShiftEnd, evt.EventId);

            // 1. В RabbitMQ — для сервиса уведомлений.
            await _publisher.PublishAsync(evt, RabbitMqTopology.ShiftEndedRoutingKey, ct);

            // 2. В SignalR — для UI.
            await _realtime.NotifyShiftEndedAsync(evt.EventId, evt.ShiftType, evt.ShiftEnd, ct);

            _lastPublishedBoundary = latestBoundary;
        }
    }

    /// <summary>
    /// Возвращает последнюю (самую свежую) границу смены до указанного момента.
    /// Границы — это 08:00 и 20:00 локального времени.
    /// </summary>
    private DateTimeOffset GetMostRecentBoundary(DateTimeOffset nowUtc)
    {
        var localNow = TimeZoneInfo.ConvertTime(nowUtc, _timeZone);

        // Сегодняшние границы
        var todayDayBoundary = new DateTimeOffset(
            localNow.Date.AddHours(_options.NightShiftStartHour),
            localNow.Offset);

        var todayNightBoundary = new DateTimeOffset(
            localNow.Date.AddHours(_options.DayShiftStartHour),
            localNow.Offset);

        // Берём ту, что уже наступила и самая свежая.
        var candidates = new[]
        {
            todayDayBoundary,
            todayNightBoundary,
            todayDayBoundary.AddDays(-1),
            todayNightBoundary.AddDays(-1)
        };

        return candidates
            .Where(c => c <= localNow)
            .OrderByDescending(c => c)
            .First()
            .ToUniversalTime();
    }
}