using AutoMapper;
using Humidity.Application.DTOs;
using Humidity.Application.Interfaces;
using Humidity.Domain.Common;
using Humidity.Domain.Entities;
using Humidity.Domain.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

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

    public async Task<PagedResult<VehicleDto>> GetPagedAsync(int pageNumber, int pageSize)
    {
        var pagedResult = await _repository.GetPagedAsync(pageNumber, pageSize);
        return new PagedResult<VehicleDto>
        {
            Items = _mapper.Map<IEnumerable<VehicleDto>>(pagedResult.Items),
            TotalCount = pagedResult.TotalCount,
            PageNumber = pagedResult.PageNumber,
            PageSize = pagedResult.PageSize,
            TotalPages = pagedResult.TotalPages
        };
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

    public async Task<PagedResult<VehicleDto>> GetActiveVehiclesPagedAsync(int pageNumber, int pageSize)
    {
        var pagedResult = await _repository.GetActiveVehiclesPagedAsync(pageNumber, pageSize);
        return new PagedResult<VehicleDto>
        {
            Items = _mapper.Map<IEnumerable<VehicleDto>>(pagedResult.Items),
            TotalCount = pagedResult.TotalCount,
            PageNumber = pagedResult.PageNumber,
            PageSize = pagedResult.PageSize,
            TotalPages = pagedResult.TotalPages
        };
    }

    public async Task<VehicleDto> CreateAsync(CreateVehicleRequest request)
    {
        var vehicle = _mapper.Map<Vehicle>(request);
        var created = await _repository.AddAsync(vehicle);
        return _mapper.Map<VehicleDto>(created);
    }

    public async Task<VehicleDto> UpdateAsync(Guid id, UpdateVehicleRequest request)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
            throw new KeyNotFoundException($"Машина с id {id} не найдена");

        _mapper.Map(request, existing);
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