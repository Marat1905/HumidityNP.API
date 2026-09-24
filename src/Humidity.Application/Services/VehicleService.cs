using AutoMapper;
using Humidity.Application.Common.Models;
using Humidity.Application.DTOs;
using Humidity.Application.Interfaces;
using Humidity.Domain.Common;
using Humidity.Domain.Entities;
using Humidity.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Humidity.Application.Services;

/// <summary>
/// Реализация сервиса для управления машинами.
/// </summary>
public class VehicleService : IVehicleService
{
    private readonly IVehicleRepository _repository;
    private readonly IMeasurementRepository _measurementRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<VehicleService> _logger;

    /// <summary>
    /// Часовой пояс, в котором работает сервер 1С (например, "Ekaterinburg Standard Time").
    /// Используется для преобразования дат, полученных от 1С (в локальном времени 1С),
    /// в UTC перед сравнением с полями БД, которые хранятся в UTC.
    /// </summary>
    private readonly TimeZoneInfo _oneCTimeZone;

    public VehicleService(
        IVehicleRepository repository,
        IMeasurementRepository measurementRepository,
        IMapper mapper,
        ILogger<VehicleService> logger,
        IOptions<OneCIntegrationSettings> oneCSettings)
    {
        _repository = repository;
        _measurementRepository = measurementRepository;
        _mapper = mapper;
        _logger = logger;

        // Инициализируем часовой пояс 1С из настроек.
        // При ошибке (пояс не найден или некорректен) используем UTC — это безопасный фолбэк,
        // чтобы не падать при старте приложения из-за опечатки в конфиге.
        try
        {
            _oneCTimeZone = TimeZoneInfo.FindSystemTimeZoneById(oneCSettings.Value.TimeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            _logger.LogError("Часовой пояс 1С '{TimeZoneId}' не найден. Используется UTC.", oneCSettings.Value.TimeZoneId);
            _oneCTimeZone = TimeZoneInfo.Utc;
        }
        catch (InvalidTimeZoneException)
        {
            _logger.LogError("Некорректный идентификатор часового пояса 1С '{TimeZoneId}'. Используется UTC.", oneCSettings.Value.TimeZoneId);
            _oneCTimeZone = TimeZoneInfo.Utc;
        }
    }

    public async Task<IEnumerable<VehicleDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Запрос всех машин");
        var vehicles = await _repository.GetAllAsync(cancellationToken);
        var result = _mapper.Map<IEnumerable<VehicleDto>>(vehicles);
        _logger.LogInformation("Получено {Count} машин", result.Count());
        return result;
    }

    public async Task<PagedResult<VehicleDto>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Запрос страницы машин: номер {PageNumber}, размер {PageSize}", pageNumber, pageSize);

        // 1. Получаем пагинированный список сущностей Vehicle
        var pagedResult = await _repository.GetPagedAsync(pageNumber, pageSize, cancellationToken: cancellationToken);

        // 2. Маппим сущности в DTO (без количества замеров)
        var items = _mapper.Map<IEnumerable<VehicleDto>>(pagedResult.Items).ToList();

        // 3. Если есть записи – получаем количество замеров для каждой машины
        if (items.Any())
        {
            var vehicleIds = items.Select(v => v.Id).Distinct().ToList();
            // Запрос к репозиторию замеров: группировка по VehicleId и подсчёт
            var measurementsCounts = await _measurementRepository.GetCountsByVehicleIdsAsync(vehicleIds, cancellationToken);
            // Заполняем свойство MeasurementsCount у каждого DTO
            foreach (var dto in items)
            {
                dto.MeasurementsCount = measurementsCounts.TryGetValue(dto.Id, out var count) ? count : 0;
            }
        }

        // 4. Формируем результат
        var result = new PagedResult<VehicleDto>
        {
            Items = items,
            TotalCount = pagedResult.TotalCount,
            PageNumber = pagedResult.PageNumber,
            PageSize = pagedResult.PageSize,
            TotalPages = pagedResult.TotalPages
        };

        _logger.LogInformation("Возвращено {Count} машин из {TotalCount}",
            result.Items.Count(), result.TotalCount);
        return result;
    }

    // МЕТОД С ФИЛЬТРАМИ, включая фильтр по диапазону даты въезда.
    public async Task<PagedResult<VehicleDto>> GetFilteredPagedAsync(
        int pageNumber,
        int pageSize,
        string? counterparty,
        bool? isActive,
        string? plate,
        string? driver,
        DateTimeOffset? entryDateFrom = null,
        DateTimeOffset? entryDateTo = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Запрос отфильтрованной страницы машин: номер {PageNumber}, размер {PageSize}, " +
            "поставщик='{Counterparty}', статус={IsActive}, госномер='{Plate}', водитель='{Driver}', " +
            "въезд с {From} по {To}",
            pageNumber, pageSize, counterparty, isActive, plate, driver, entryDateFrom, entryDateTo);

        // Получаем данные из репозитория с фильтрами (включая фильтр по дате въезда).
        var pagedResult = await _repository.GetFilteredPagedAsync(
            pageNumber, pageSize, counterparty, isActive, plate, driver,
            entryDateFrom, entryDateTo, cancellationToken);

        var items = _mapper.Map<IEnumerable<VehicleDto>>(pagedResult.Items).ToList();

        // Подсчёт количества замеров для каждой машины
        if (items.Any())
        {
            var vehicleIds = items.Select(v => v.Id).Distinct().ToList();
            var measurementsCounts = await _measurementRepository.GetCountsByVehicleIdsAsync(vehicleIds, cancellationToken);
            foreach (var dto in items)
            {
                dto.MeasurementsCount = measurementsCounts.TryGetValue(dto.Id, out var count) ? count : 0;
            }
        }

        var result = new PagedResult<VehicleDto>
        {
            Items = items,
            TotalCount = pagedResult.TotalCount,
            PageNumber = pagedResult.PageNumber,
            PageSize = pagedResult.PageSize,
            TotalPages = pagedResult.TotalPages
        };

        _logger.LogInformation("Возвращено {Count} машин из {TotalCount} после фильтрации",
            result.Items.Count(), result.TotalCount);
        return result;
    }

    public async Task<VehicleDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Запрос машины по id: {VehicleId}", id);
        var vehicle = await _repository.GetByIdAsync(id, cancellationToken);
        if (vehicle == null)
        {
            _logger.LogWarning("Машина с id {VehicleId} не найдена", id);
            return null;
        }
        var result = _mapper.Map<VehicleDto>(vehicle);
        _logger.LogInformation("Машина с id {VehicleId} успешно получена", id);
        return result;
    }

    public async Task<IEnumerable<VehicleDto>> GetActiveVehiclesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Запрос активных машин");
        var vehicles = await _repository.GetActiveVehiclesAsync(cancellationToken);
        var result = _mapper.Map<IEnumerable<VehicleDto>>(vehicles);
        _logger.LogInformation("Получено {Count} активных машин", result.Count());
        return result;
    }

    public async Task<PagedResult<VehicleDto>> GetActiveVehiclesPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Запрос страницы активных машин: номер {PageNumber}, размер {PageSize}", pageNumber, pageSize);
        var pagedResult = await _repository.GetActiveVehiclesPagedAsync(pageNumber, pageSize, cancellationToken);
        var result = new PagedResult<VehicleDto>
        {
            Items = _mapper.Map<IEnumerable<VehicleDto>>(pagedResult.Items),
            TotalCount = pagedResult.TotalCount,
            PageNumber = pagedResult.PageNumber,
            PageSize = pagedResult.PageSize,
            TotalPages = pagedResult.TotalPages
        };
        _logger.LogInformation("Возвращено {Count} активных машин из {TotalCount}",
            result.Items.Count(), result.TotalCount);
        return result;
    }

    public async Task<VehicleDto> CreateAsync(CreateVehicleRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Создание новой машины с номером пропуска: {Number}", request.Number);
        var vehicle = _mapper.Map<Vehicle>(request);
        var created = await _repository.AddAsync(vehicle, cancellationToken);
        var result = _mapper.Map<VehicleDto>(created);
        _logger.LogInformation("Машина создана с id {VehicleId}", created.Id);
        return result;
    }

    public async Task<VehicleDto> UpdateAsync(Guid id, UpdateVehicleRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Обновление машины с id {VehicleId}", id);
        var existing = await _repository.GetByIdAsync(id, cancellationToken);
        if (existing == null)
        {
            _logger.LogWarning("Машина с id {VehicleId} не найдена для обновления", id);
            throw new KeyNotFoundException($"Машина с id {id} не найдена");
        }

        // Дополнительная валидация для ExitDate: если передан ExitDate, он должен быть не раньше EntryDate
        if (request.ExitDate.HasValue)
        {
            // Проверяем, что ExitDate >= EntryDate (существующее значение)
            if (request.ExitDate.Value < existing.EntryDate)
            {
                _logger.LogWarning("Попытка установить ExitDate {ExitDate} раньше EntryDate {EntryDate} для машины {VehicleId}",
                    request.ExitDate.Value, existing.EntryDate, id);
                throw new InvalidOperationException(
                    $"Дата выезда ({request.ExitDate.Value:yyyy-MM-dd HH:mm:ss}) не может быть раньше даты въезда ({existing.EntryDate:yyyy-MM-dd HH:mm:ss}).");
            }

            // Дополнительно проверяем, что ExitDate не в будущем (это уже есть в валидаторе, но продублируем для надёжности)
            if (request.ExitDate.Value > DateTimeOffset.UtcNow.AddMinutes(1))
            {
                _logger.LogWarning("Попытка установить ExitDate {ExitDate} в будущем для машины {VehicleId}",
                    request.ExitDate.Value, id);
                throw new InvalidOperationException("Дата выезда не может быть в будущем.");
            }
        }

        // Маппер применяет только не-null поля (настроено в MappingProfile)
        // Сущность теперь отслеживается, поэтому EF Core сгенерирует UPDATE только для изменённых свойств
        _mapper.Map(request, existing);
        var updated = await _repository.UpdateAsync(existing, cancellationToken);
        var result = _mapper.Map<VehicleDto>(updated);
        _logger.LogInformation("Машина с id {VehicleId} успешно обновлена", id);
        return result;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Удаление машины с id {VehicleId}", id);
        var existing = await _repository.GetByIdAsync(id, cancellationToken);
        if (existing == null)
        {
            _logger.LogWarning("Машина с id {VehicleId} не найдена для удаления", id);
            throw new KeyNotFoundException($"Машина с id {id} не найдена");
        }

        await _repository.DeleteAsync(existing, cancellationToken);
        _logger.LogInformation("Машина с id {VehicleId} успешно удалена", id);
    }

    /// <summary>
    /// Зафиксировать разгрузку машины: количество тюков, порванных тюков, вес и номер штабеля.
    /// </summary>
    public async Task<VehicleDto> UnloadAsync(Guid id, UnloadVehicleRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Фиксация разгрузки для машины {VehicleId}", id);

        // 1. Получаем машину
        var vehicle = await _repository.GetByIdAsync(id, cancellationToken);
        if (vehicle == null)
        {
            _logger.LogWarning("Машина с id {VehicleId} не найдена для фиксации разгрузки", id);
            throw new KeyNotFoundException($"Машина с id {id} не найдена.");
        }

        // 2. Обновляем поля разгрузки (без проверки ExitDate – оператор может вносить данные в любое время)
        vehicle.BaleCount = request.BaleCount;
        vehicle.DamagedBaleCount = request.DamagedBaleCount;
        vehicle.WeightKg = request.WeightKg;
        vehicle.StackNumber = request.StackNumber;

        // 3. Сохраняем изменения
        var updated = await _repository.UpdateAsync(vehicle, cancellationToken);
        var result = _mapper.Map<VehicleDto>(updated);

        _logger.LogInformation("Разгрузка для машины {VehicleId} успешно зафиксирована: тюков {BaleCount}, порванных {DamagedBaleCount}, вес {WeightKg} кг, штабель {StackNumber}",
            id, request.BaleCount, request.DamagedBaleCount, request.WeightKg, request.StackNumber);

        return result;
    }

    /// <summary>
    /// Получить информацию о разгрузке машины и среднюю влажность по уникальному идентификатору 1С (ГУИД).
    /// Даты здесь не участвуют, поэтому преобразование часового пояса не требуется.
    /// </summary>
    /// <param name="oneCGuid">Уникальный идентификатор записи из 1С.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>DTO с информацией о разгрузке и средней влажности или null, если машина не найдена.</returns>
    public async Task<OneCVehicleUnloadDto?> GetUnloadInfoByOneCGuidAsync(string oneCGuid, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Запрос информации о разгрузке по OneCGuid: {OneCGuid}", oneCGuid);

        var result = await _repository.GetUnloadInfoByOneCGuidAsync(oneCGuid, cancellationToken);

        if (result == null)
        {
            _logger.LogWarning("Машина с OneCGuid {OneCGuid} не найдена", oneCGuid);
        }
        else
        {
            _logger.LogInformation("Информация о разгрузке для OneCGuid {OneCGuid} получена", oneCGuid);
        }

        return result;
    }

    /// <summary>
    /// Получить информацию о разгрузке машин и среднюю влажность за период.
    /// Период фильтруется по дате создания пропуска (Vehicle.Date).
    ///
    /// ВАЖНО: 1С работает в часовом поясе Екатеринбурга (UTC+5), а в БД все даты хранятся в UTC.
    /// Поэтому входящие даты (from, to), полученные от 1С, интерпретируются как локальное
    /// время 1С и преобразуются в UTC перед сравнением с Vehicle.Date.
    /// </summary>
    /// <param name="from">Начало периода (включительно) в локальном времени 1С.</param>
    /// <param name="to">Конец периода (включительно) в локальном времени 1С.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Коллекция DTO с информацией о разгрузке и средней влажности.</returns>
    public async Task<IEnumerable<OneCVehicleUnloadDto>> GetUnloadInfoByPeriodAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default)
    {
        // Преобразуем даты из локального времени 1С в UTC.
        // Логика:
        //   - Если 1С передала дату со смещением (например, "2024-01-01T00:00:00+05:00"),
        //     просто конвертируем её в UTC.
        //   - Если 1С передала дату без смещения или с Z (например, "2024-01-01T00:00:00Z"
        //     или "2024-01-01T00:00:00"), интерпретируем её как локальное время 1С
        //     и преобразуем в UTC через TimeZoneInfo.
        var fromUtc = ConvertFromOneCTimeZone(from);
        var toUtc = ConvertFromOneCTimeZone(to);

        _logger.LogInformation(
            "Запрос информации о разгрузке за период (1С локальное время): {From:O} — {To:O}; " +
            "после преобразования в UTC: {FromUtc:O} — {ToUtc:O}",
            from, to, fromUtc, toUtc);

        var result = await _repository.GetUnloadInfoByPeriodAsync(fromUtc, toUtc, cancellationToken);

        _logger.LogInformation("Получено {Count} записей о разгрузке за период", result.Count());

        return result;
    }

    /// <summary>
    /// Преобразует дату, полученную от 1С, в UTC.
    ///
    /// Правила:
    ///   1. Если дата пришла с явным ненулевым смещением (например, +05:00),
    ///      считаем, что 1С передала корректный DateTimeOffset, и просто конвертируем в UTC.
    ///   2. Если смещение нулевое (Z или отсутствует), считаем, что 1С передала
    ///      локальное время в своём часовом поясе (Екатеринбург, UTC+5).
    ///      В этом случае интерпретируем компоненты даты/времени как локальные
    ///      в часовом поясе 1С и преобразуем в UTC.
    ///
    /// Такой подход устойчив к обоим вариантам, которые может использовать 1С,
    /// и не приводит к двойному сдвигу, если 1С вдруг начнёт передавать смещение.
    /// </summary>
    /// <param name="oneCDate">Дата, полученная от 1С.</param>
    /// <returns>Дата в UTC.</returns>
    private DateTimeOffset ConvertFromOneCTimeZone(DateTimeOffset oneCDate)
    {
        // Случай 1: смещение уже задано и не равно нулю — доверяем ему.
        if (oneCDate.Offset != TimeSpan.Zero)
        {
            return oneCDate.ToUniversalTime();
        }

        // Случай 2: смещение нулевое — интерпретируем как локальное время 1С.
        // DateTimeKind.Unspecified нужен, чтобы TimeZoneInfo корректно применил правила
        // часового пояса (включая возможный переход на летнее время, если он есть).
        var localDateTime = DateTime.SpecifyKind(oneCDate.DateTime, DateTimeKind.Unspecified);
        var utcDateTime = TimeZoneInfo.ConvertTimeToUtc(localDateTime, _oneCTimeZone);
        return new DateTimeOffset(utcDateTime, TimeSpan.Zero);
    }
}