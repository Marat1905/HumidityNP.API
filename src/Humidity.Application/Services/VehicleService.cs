using AutoMapper;
using Humidity.Application.DTOs;
using Humidity.Application.Interfaces;
using Humidity.Domain.Common;
using Humidity.Domain.Entities;
using Humidity.Domain.Interfaces;
using Microsoft.Extensions.Logging; // добавлен using

namespace Humidity.Application.Services;

/// <summary>
/// Реализация сервиса для управления машинами.
/// </summary>
public class VehicleService : IVehicleService
{
    private readonly IVehicleRepository _repository;
    private readonly IMapper _mapper;
    private readonly ILogger<VehicleService> _logger;

    public VehicleService(IVehicleRepository repository, IMapper mapper, ILogger<VehicleService> logger)
    {
        _repository = repository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IEnumerable<VehicleDto>> GetAllAsync()
    {
        _logger.LogInformation("Запрос всех машин");
        var vehicles = await _repository.GetAllAsync();
        var result = _mapper.Map<IEnumerable<VehicleDto>>(vehicles);
        _logger.LogInformation("Получено {Count} машин", result.Count());
        return result;
    }

    public async Task<PagedResult<VehicleDto>> GetPagedAsync(int pageNumber, int pageSize)
    {
        _logger.LogInformation("Запрос страницы машин: номер {PageNumber}, размер {PageSize}", pageNumber, pageSize);
        var pagedResult = await _repository.GetPagedAsync(pageNumber, pageSize);
        var result = new PagedResult<VehicleDto>
        {
            Items = _mapper.Map<IEnumerable<VehicleDto>>(pagedResult.Items),
            TotalCount = pagedResult.TotalCount,
            PageNumber = pagedResult.PageNumber,
            PageSize = pagedResult.PageSize,
            TotalPages = pagedResult.TotalPages
        };
        _logger.LogInformation("Возвращено {Count} машин из {TotalCount}",
            result.Items.Count(), result.TotalCount);
        return result;
    }

    public async Task<VehicleDto?> GetByIdAsync(Guid id)
    {
        _logger.LogInformation("Запрос машины по id: {VehicleId}", id);
        var vehicle = await _repository.GetByIdAsync(id);
        if (vehicle == null)
        {
            _logger.LogWarning("Машина с id {VehicleId} не найдена", id);
            return null;
        }
        var result = _mapper.Map<VehicleDto>(vehicle);
        _logger.LogInformation("Машина с id {VehicleId} успешно получена", id);
        return result;
    }

    public async Task<IEnumerable<VehicleDto>> GetActiveVehiclesAsync()
    {
        _logger.LogInformation("Запрос активных машин");
        var vehicles = await _repository.GetActiveVehiclesAsync();
        var result = _mapper.Map<IEnumerable<VehicleDto>>(vehicles);
        _logger.LogInformation("Получено {Count} активных машин", result.Count());
        return result;
    }

    public async Task<PagedResult<VehicleDto>> GetActiveVehiclesPagedAsync(int pageNumber, int pageSize)
    {
        _logger.LogInformation("Запрос страницы активных машин: номер {PageNumber}, размер {PageSize}", pageNumber, pageSize);
        var pagedResult = await _repository.GetActiveVehiclesPagedAsync(pageNumber, pageSize);
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

    public async Task<VehicleDto> CreateAsync(CreateVehicleRequest request)
    {
        _logger.LogInformation("Создание новой машины с номером заявки: {Number}", request.Number);
        var vehicle = _mapper.Map<Vehicle>(request);
        var created = await _repository.AddAsync(vehicle);
        var result = _mapper.Map<VehicleDto>(created);
        _logger.LogInformation("Машина создана с id {VehicleId}", created.Id);
        return result;
    }

    public async Task<VehicleDto> UpdateAsync(Guid id, UpdateVehicleRequest request)
    {
        _logger.LogInformation("Обновление машины с id {VehicleId}", id);
        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
        {
            _logger.LogWarning("Машина с id {VehicleId} не найдена для обновления", id);
            throw new KeyNotFoundException($"Машина с id {id} не найдена");
        }

        // Маппер применяет только не-null поля (настроено в MappingProfile)
        // Сущность теперь отслеживается, поэтому EF Core сгенерирует UPDATE только для изменённых свойств
        _mapper.Map(request, existing);
        var updated = await _repository.UpdateAsync(existing);
        var result = _mapper.Map<VehicleDto>(updated);
        _logger.LogInformation("Машина с id {VehicleId} успешно обновлена", id);
        return result;
    }

    public async Task DeleteAsync(Guid id)
    {
        _logger.LogInformation("Удаление машины с id {VehicleId}", id);
        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
        {
            _logger.LogWarning("Машина с id {VehicleId} не найдена для удаления", id);
            throw new KeyNotFoundException($"Машина с id {id} не найдена");
        }

        await _repository.DeleteAsync(existing);
        _logger.LogInformation("Машина с id {VehicleId} успешно удалена", id);
    }
}