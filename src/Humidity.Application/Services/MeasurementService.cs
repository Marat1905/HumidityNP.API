using AutoMapper;
using Humidity.Application.DTOs;
using Humidity.Application.Interfaces;
using Humidity.Domain.Common;
using Humidity.Domain.Entities;
using Humidity.Domain.Enums;
using Humidity.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using FluentValidation; // добавлен для валидации

namespace Humidity.Application.Services;

/// <summary>
/// Реализация сервиса для управления замерами влажности.
/// </summary>
public class MeasurementService : IMeasurementService
{
    private readonly IMeasurementRepository _repository;
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<MeasurementService> _logger;
    private readonly IValidator<CreateMeasurementRequest> _validator; // добавлен валидатор

    public MeasurementService(
        IMeasurementRepository repository,
        IVehicleRepository vehicleRepository,
        IMapper mapper,
        ILogger<MeasurementService> logger,
        IValidator<CreateMeasurementRequest> validator) // внедряем валидатор
    {
        _repository = repository;
        _vehicleRepository = vehicleRepository;
        _mapper = mapper;
        _logger = logger;
        _validator = validator;
    }

    public async Task<IEnumerable<MeasurementDto>> GetByVehicleIdAsync(Guid vehicleId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Запрос замеров для машины {VehicleId}", vehicleId);
        var measurements = await _repository.GetByVehicleIdAsync(vehicleId, cancellationToken);
        var result = _mapper.Map<IEnumerable<MeasurementDto>>(measurements);
        _logger.LogInformation("Получено {Count} замеров для машины {VehicleId}", result.Count(), vehicleId);
        return result;
    }

    public async Task<PagedResult<MeasurementDto>> GetByVehicleIdPagedAsync(Guid vehicleId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Запрос страницы замеров для машины {VehicleId}: номер {PageNumber}, размер {PageSize}",
            vehicleId, pageNumber, pageSize);
        var pagedResult = await _repository.GetByVehicleIdPagedAsync(vehicleId, pageNumber, pageSize, cancellationToken);
        var result = new PagedResult<MeasurementDto>
        {
            Items = _mapper.Map<IEnumerable<MeasurementDto>>(pagedResult.Items),
            TotalCount = pagedResult.TotalCount,
            PageNumber = pagedResult.PageNumber,
            PageSize = pagedResult.PageSize,
            TotalPages = pagedResult.TotalPages
        };
        _logger.LogInformation("Возвращено {Count} замеров из {TotalCount} для машины {VehicleId}",
            result.Items.Count(), result.TotalCount, vehicleId);
        return result;
    }

    public async Task<MeasurementDto?> GetLatestByVehicleIdAsync(Guid vehicleId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Запрос последнего замера для машины {VehicleId}", vehicleId);
        var measurement = await _repository.GetLatestByVehicleIdAsync(vehicleId, cancellationToken);
        if (measurement == null)
        {
            _logger.LogWarning("Последний замер для машины {VehicleId} не найден", vehicleId);
            return null;
        }
        var result = _mapper.Map<MeasurementDto>(measurement);
        _logger.LogInformation("Последний замер для машины {VehicleId} получен", vehicleId);
        return result;
    }

    public async Task<IEnumerable<MeasurementDto>> GetByDateAsync(DateTimeOffset date, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Запрос замеров за дату {Date:yyyy-MM-dd}", date);
        var measurements = await _repository.GetByDateAsync(date, cancellationToken);
        var result = _mapper.Map<IEnumerable<MeasurementDto>>(measurements);
        _logger.LogInformation("Получено {Count} замеров за дату {Date:yyyy-MM-dd}", result.Count(), date);
        return result;
    }

    public async Task<PagedResult<MeasurementDto>> GetByDatePagedAsync(DateTimeOffset date, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Запрос страницы замеров за дату {Date:yyyy-MM-dd}: номер {PageNumber}, размер {PageSize}",
            date, pageNumber, pageSize);
        var pagedResult = await _repository.GetByDatePagedAsync(date, pageNumber, pageSize, cancellationToken);
        var result = new PagedResult<MeasurementDto>
        {
            Items = _mapper.Map<IEnumerable<MeasurementDto>>(pagedResult.Items),
            TotalCount = pagedResult.TotalCount,
            PageNumber = pagedResult.PageNumber,
            PageSize = pagedResult.PageSize,
            TotalPages = pagedResult.TotalPages
        };
        _logger.LogInformation("Возвращено {Count} замеров из {TotalCount} за дату {Date:yyyy-MM-dd}",
            result.Items.Count(), result.TotalCount, date);
        return result;
    }

    public async Task<MeasurementDto> CreateAsync(CreateMeasurementRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Создание замера для машины {VehicleId} с влажностью {HumidityValue}%",
            request.VehicleId, request.HumidityValue);

        var vehicleExists = await _vehicleRepository.ExistsAsync(request.VehicleId, cancellationToken);
        if (!vehicleExists)
        {
            _logger.LogWarning("Машина с id {VehicleId} не найдена при создании замера", request.VehicleId);
            throw new KeyNotFoundException($"Машина с id {request.VehicleId} не найдена");
        }

        var measurement = _mapper.Map<HumidityMeasurement>(request);
        measurement.Source = Enum.Parse<MeasurementSource>(request.Source, true);
        var created = await _repository.AddAsync(measurement, cancellationToken);
        var result = _mapper.Map<MeasurementDto>(created);
        _logger.LogInformation("Замер создан с id {MeasurementId} для машины {VehicleId}", created.Id, request.VehicleId);
        return result;
    }

    public async Task<MeasurementDto> UpdateAsync(Guid id, UpdateMeasurementRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Обновление замера с id {MeasurementId}", id);
        var existing = await _repository.GetByIdAsync(id, cancellationToken);
        if (existing == null)
        {
            _logger.LogWarning("Замер с id {MeasurementId} не найден для обновления", id);
            throw new KeyNotFoundException($"Замер с id {id} не найден");
        }

        // Используем AutoMapper для обновления только переданных полей (настроено игнорирование null)
        _mapper.Map(request, existing);

        var updated = await _repository.UpdateAsync(existing, cancellationToken);
        var result = _mapper.Map<MeasurementDto>(updated);
        _logger.LogInformation("Замер с id {MeasurementId} успешно обновлён", id);
        return result;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Удаление замера с id {MeasurementId}", id);
        var existing = await _repository.GetByIdAsync(id, cancellationToken);
        if (existing == null)
        {
            _logger.LogWarning("Замер с id {MeasurementId} не найден для удаления", id);
            throw new KeyNotFoundException($"Замер с id {id} не найден");
        }

        await _repository.DeleteAsync(existing, cancellationToken);
        _logger.LogInformation("Замер с id {MeasurementId} успешно удалён", id);
    }

    /// <summary>
    /// Массовая загрузка замеров с полной валидацией каждого запроса.
    /// Для каждого запроса проверяется:
    /// - Валидность данных через FluentValidation (правила из CreateMeasurementRequestValidator)
    /// - Существование машины по VehicleId
    /// Запросы, не прошедшие валидацию или с несуществующей машиной, пропускаются,
    /// информация о них возвращается в результате с деталями ошибок.
    /// </summary>
    /// <param name="requests">Список запросов на создание.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Результат массовой загрузки с количеством созданных, пропущенных и списком ошибок.</returns>
    public async Task<BulkMeasurementResult> BulkCreateAsync(IEnumerable<CreateMeasurementRequest> requests, CancellationToken cancellationToken = default)
    {
        var requestList = requests.ToList();
        _logger.LogInformation("Начало массовой загрузки {Count} замеров", requestList.Count);

        if (!requestList.Any())
        {
            _logger.LogInformation("Список запросов на массовую загрузку пуст");
            return new BulkMeasurementResult();
        }

        // Собираем все уникальные VehicleId из запросов для проверки существования машин
        var vehicleIds = requestList.Select(r => r.VehicleId).Distinct();
        var existingVehicleIds = await _vehicleRepository.GetExistingIdsAsync(vehicleIds, cancellationToken);

        var validMeasurements = new List<HumidityMeasurement>();
        var errors = new List<MeasurementBulkError>();

        // Проходим по каждому запросу с индексом
        for (int i = 0; i < requestList.Count; i++)
        {
            var request = requestList[i];
            var errorMessages = new List<string>();

            // 1. Валидация данных запроса с помощью FluentValidation
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                // Собираем все сообщения об ошибках валидации
                var validationErrors = validationResult.Errors.Select(e => e.ErrorMessage);
                errorMessages.AddRange(validationErrors);
                _logger.LogWarning("Запрос с индексом {Index} не прошёл валидацию: {Errors}",
                    i, string.Join("; ", validationErrors));
            }

            // 2. Проверка существования машины
            if (!existingVehicleIds.Contains(request.VehicleId))
            {
                errorMessages.Add($"Машина с id {request.VehicleId} не найдена.");
                _logger.LogWarning("Запрос с индексом {Index}: машина {VehicleId} не найдена", i, request.VehicleId);
            }

            // Если есть ошибки, добавляем запись в список пропущенных и переходим к следующему запросу
            if (errorMessages.Any())
            {
                var error = new MeasurementBulkError
                {
                    Index = i,
                    VehicleId = request.VehicleId,
                    Message = string.Join("; ", errorMessages)
                };
                errors.Add(error);
                continue;
            }

            // Запрос валиден и машина существует — добавляем в список для создания
            var measurement = _mapper.Map<HumidityMeasurement>(request);
            // Преобразуем строковое значение Source в enum (регистронезависимо)
            measurement.Source = Enum.Parse<MeasurementSource>(request.Source, true);
            validMeasurements.Add(measurement);
        }

        // Выполняем массовую вставку валидных записей
        var created = new List<HumidityMeasurement>();
        if (validMeasurements.Any())
        {
            created = (await _repository.BulkAddAsync(validMeasurements, cancellationToken)).ToList();
            _logger.LogInformation("Успешно создано {CreatedCount} замеров", created.Count);
        }

        if (errors.Any())
        {
            _logger.LogWarning("Пропущено {SkippedCount} замеров из-за ошибок валидации или отсутствия машины", errors.Count);
        }

        return new BulkMeasurementResult
        {
            CreatedCount = created.Count,
            SkippedCount = errors.Count,
            Errors = errors
        };
    }
}