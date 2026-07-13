using AutoMapper;
using Humidity.Application.DTOs;
using Humidity.Application.Interfaces;
using Humidity.Domain.Entities;
using Humidity.Domain.Interfaces;

namespace Humidity.Application.Services;

/// <summary>
/// Реализация сервиса для управления машинами.
/// </summary>
public class VehicleService : IVehicleService
{
    private readonly IVehicleRepository _repository;
    private readonly IMapper _mapper;

    public VehicleService(IVehicleRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<VehicleDto>> GetAllAsync()
    {
        var vehicles = await _repository.GetAllAsync();
        return _mapper.Map<IEnumerable<VehicleDto>>(vehicles);
    }

    public async Task<VehicleDto?> GetByIdAsync(Guid id)
    {
        var vehicle = await _repository.GetByIdAsync(id);
        return vehicle == null ? null : _mapper.Map<VehicleDto>(vehicle);
    }

    public async Task<IEnumerable<VehicleDto>> GetActiveVehiclesAsync()
    {
        var vehicles = await _repository.GetActiveVehiclesAsync();
        return _mapper.Map<IEnumerable<VehicleDto>>(vehicles);
    }

    public async Task<VehicleDto> CreateAsync(CreateVehicleRequest request)
    {
        if (!DateTime.TryParse(request.Date, out var parsedDate))
            throw new ArgumentException("Некорректный формат даты");

        var vehicle = _mapper.Map<Vehicle>(request);
        vehicle.Date = DateTime.SpecifyKind(parsedDate, DateTimeKind.Utc);

        if (DateTime.TryParse(request.ArrivalDate, out var arrivalDate))
            vehicle.ArrivalDate = DateTime.SpecifyKind(arrivalDate, DateTimeKind.Utc);

        if (DateTime.TryParse(request.EntryDate, out var entryDate))
            vehicle.EntryDate = DateTime.SpecifyKind(entryDate, DateTimeKind.Utc);

        if (!string.IsNullOrEmpty(request.ExitDate) && DateTime.TryParse(request.ExitDate, out var exitDate))
            vehicle.ExitDate = DateTime.SpecifyKind(exitDate, DateTimeKind.Utc);

        vehicle.CreatedAt = DateTime.UtcNow;

        var created = await _repository.AddAsync(vehicle);
        return _mapper.Map<VehicleDto>(created);
    }

    public async Task<VehicleDto> UpdateAsync(Guid id, UpdateVehicleRequest request)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
            throw new KeyNotFoundException($"Машина с id {id} не найдена");

        _mapper.Map(request, existing);

        if (!string.IsNullOrEmpty(request.ExitDate) && DateTime.TryParse(request.ExitDate, out var exitDate))
            existing.ExitDate = DateTime.SpecifyKind(exitDate, DateTimeKind.Utc);

        existing.UpdatedAt = DateTime.UtcNow;

        var updated = await _repository.UpdateAsync(existing);
        return _mapper.Map<VehicleDto>(updated);
    }

    public async Task DeleteAsync(Guid id)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
            throw new KeyNotFoundException($"Машина с id {id} не найдена");

        await _repository.DeleteAsync(existing);
    }
}