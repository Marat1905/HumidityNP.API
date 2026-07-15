using AutoMapper;
using Humidity.Application.DTOs;
using Humidity.Application.Interfaces;
using Humidity.Domain.Common;
using Humidity.Domain.Entities;
using Humidity.Domain.Enums;
using Humidity.Domain.Interfaces;

namespace Humidity.Application.Services;

/// <summary>
/// Реализация сервиса для управления замерами влажности.
/// </summary>
public class MeasurementService : IMeasurementService
{
    private readonly IMeasurementRepository _repository;
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IMapper _mapper;

    public MeasurementService(
        IMeasurementRepository repository,
        IVehicleRepository vehicleRepository,
        IMapper mapper)
    {
        _repository = repository;
        _vehicleRepository = vehicleRepository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<MeasurementDto>> GetByVehicleIdAsync(Guid vehicleId)
    {
        var measurements = await _repository.GetByVehicleIdAsync(vehicleId);
        return _mapper.Map<IEnumerable<MeasurementDto>>(measurements);
    }

    public async Task<PagedResult<MeasurementDto>> GetByVehicleIdPagedAsync(Guid vehicleId, int pageNumber, int pageSize)
    {
        var pagedResult = await _repository.GetByVehicleIdPagedAsync(vehicleId, pageNumber, pageSize);
        return new PagedResult<MeasurementDto>
        {
            Items = _mapper.Map<IEnumerable<MeasurementDto>>(pagedResult.Items),
            TotalCount = pagedResult.TotalCount,
            PageNumber = pagedResult.PageNumber,
            PageSize = pagedResult.PageSize,
            TotalPages = pagedResult.TotalPages
        };
    }

    public async Task<MeasurementDto?> GetLatestByVehicleIdAsync(Guid vehicleId)
    {
        var measurement = await _repository.GetLatestByVehicleIdAsync(vehicleId);
        return measurement == null ? null : _mapper.Map<MeasurementDto>(measurement);
    }

    public async Task<IEnumerable<MeasurementDto>> GetByDateAsync(DateTimeOffset date)
    {
        var measurements = await _repository.GetByDateAsync(date);
        return _mapper.Map<IEnumerable<MeasurementDto>>(measurements);
    }

    public async Task<PagedResult<MeasurementDto>> GetByDatePagedAsync(DateTimeOffset date, int pageNumber, int pageSize)
    {
        var pagedResult = await _repository.GetByDatePagedAsync(date, pageNumber, pageSize);
        return new PagedResult<MeasurementDto>
        {
            Items = _mapper.Map<IEnumerable<MeasurementDto>>(pagedResult.Items),
            TotalCount = pagedResult.TotalCount,
            PageNumber = pagedResult.PageNumber,
            PageSize = pagedResult.PageSize,
            TotalPages = pagedResult.TotalPages
        };
    }

    public async Task<MeasurementDto> CreateAsync(CreateMeasurementRequest request)
    {
        var vehicleExists = await _vehicleRepository.ExistsAsync(request.VehicleId);
        if (!vehicleExists)
            throw new KeyNotFoundException($"Машина с id {request.VehicleId} не найдена");

        var measurement = _mapper.Map<HumidityMeasurement>(request);
        measurement.Source = Enum.Parse<MeasurementSource>(request.Source, true);
        var created = await _repository.AddAsync(measurement);
        return _mapper.Map<MeasurementDto>(created);
    }

    public async Task<MeasurementDto> UpdateAsync(Guid id, UpdateMeasurementRequest request)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
            throw new KeyNotFoundException($"Замер с id {id} не найден");

        // Используем AutoMapper для обновления только переданных полей (настроено игнорирование null)
        _mapper.Map(request, existing);

        var updated = await _repository.UpdateAsync(existing);
        return _mapper.Map<MeasurementDto>(updated);
    }

    public async Task DeleteAsync(Guid id)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
            throw new KeyNotFoundException($"Замер с id {id} не найден");

        await _repository.DeleteAsync(existing);
    }

    /// <summary>
    /// Массовая загрузка замеров.
    /// Для каждого запроса проверяется существование машины.
    /// Запросы с несуществующим VehicleId пропускаются, информация о них возвращается в результате.
    /// </summary>
    public async Task<BulkMeasurementResult> BulkCreateAsync(IEnumerable<CreateMeasurementRequest> requests)
    {
        var requestList = requests.ToList();
        if (!requestList.Any())
            return new BulkMeasurementResult();

        // Собираем все уникальные VehicleId из запросов
        var vehicleIds = requestList.Select(r => r.VehicleId).Distinct();
        var existingVehicleIds = await _vehicleRepository.GetExistingIdsAsync(vehicleIds);

        var validMeasurements = new List<HumidityMeasurement>();
        var errors = new List<MeasurementBulkError>();

        for (int i = 0; i < requestList.Count; i++)
        {
            var request = requestList[i];
            if (!existingVehicleIds.Contains(request.VehicleId))
            {
                // Машина не найдена – пропускаем запись
                errors.Add(new MeasurementBulkError
                {
                    Index = i,
                    VehicleId = request.VehicleId,
                    Message = $"Машина с id {request.VehicleId} не найдена."
                });
                continue;
            }

            // Валидация остальных полей может быть выполнена через FluentValidation,
            // но здесь мы полагаемся на то, что запросы уже прошли валидацию на уровне контроллера.
            // Тем не менее, при необходимости можно добавить дополнительную проверку.

            var measurement = _mapper.Map<HumidityMeasurement>(request);
            measurement.Source = Enum.Parse<MeasurementSource>(request.Source, true);
            validMeasurements.Add(measurement);
        }

        var created = new List<HumidityMeasurement>();
        if (validMeasurements.Any())
        {
            created = (await _repository.BulkAddAsync(validMeasurements)).ToList();
        }

        return new BulkMeasurementResult
        {
            CreatedCount = created.Count,
            SkippedCount = errors.Count,
            Errors = errors
        };
    }
}