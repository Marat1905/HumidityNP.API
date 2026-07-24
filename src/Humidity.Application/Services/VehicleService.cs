using AutoMapper;
using Humidity.Application.DTOs;
using Humidity.Application.Interfaces;
using Humidity.Domain.Common;
using Humidity.Domain.Entities;
using Humidity.Domain.Interfaces;
using Microsoft.Extensions.Logging;

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

    public VehicleService(
        IVehicleRepository repository,
        IMeasurementRepository measurementRepository,
        IMapper mapper,
        ILogger<VehicleService> logger)
    {
        _repository = repository;
        _measurementRepository = measurementRepository;
        _mapper = mapper;
        _logger = logger;
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
}