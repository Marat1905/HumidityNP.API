using AutoMapper;
using Humidity.Application.DTOs;
using Humidity.Application.Interfaces;
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

    public async Task<MeasurementDto?> GetLatestByVehicleIdAsync(Guid vehicleId)
    {
        var measurement = await _repository.GetLatestByVehicleIdAsync(vehicleId);
        return measurement == null ? null : _mapper.Map<MeasurementDto>(measurement);
    }

    public async Task<IEnumerable<MeasurementDto>> GetByDateAsync(DateTime date)
    {
        var measurements = await _repository.GetByDateAsync(date);
        return _mapper.Map<IEnumerable<MeasurementDto>>(measurements);
    }

    public async Task<MeasurementDto> CreateAsync(CreateMeasurementRequest request)
    {
        var vehicleExists = await _vehicleRepository.ExistsAsync(request.VehicleId);
        if (!vehicleExists)
            throw new KeyNotFoundException($"Машина с id {request.VehicleId} не найдена");

        if (!DateTime.TryParse(request.Timestamp, out var parsedTimestamp))
            throw new ArgumentException("Некорректный формат даты замера");

        if (!Enum.TryParse<MeasurementSource>(request.Source, true, out var source))
            source = MeasurementSource.Auto;

        var measurement = _mapper.Map<HumidityMeasurement>(request);
        measurement.Timestamp = DateTime.SpecifyKind(parsedTimestamp, DateTimeKind.Utc);
        measurement.Source = source;
        measurement.CreatedAt = DateTime.UtcNow;

        var created = await _repository.AddAsync(measurement);
        return _mapper.Map<MeasurementDto>(created);
    }

    public async Task<MeasurementDto> UpdateAsync(Guid id, UpdateMeasurementRequest request)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
            throw new KeyNotFoundException($"Замер с id {id} не найдена");

        if (request.HumidityValue.HasValue)
            existing.HumidityValue = request.HumidityValue.Value;

        if (request.TemperatureC.HasValue)
            existing.TemperatureC = request.TemperatureC.Value;

        if (!string.IsNullOrEmpty(request.MeasurementType))
            existing.MeasurementType = request.MeasurementType;

        if (!string.IsNullOrEmpty(request.Material))
            existing.Material = request.Material;

        if (!string.IsNullOrEmpty(request.Source) && Enum.TryParse<MeasurementSource>(request.Source, true, out var source))
            existing.Source = source;

        if (!string.IsNullOrEmpty(request.Sign))
            existing.Sign = request.Sign;

        existing.UpdatedAt = DateTime.UtcNow;

        var updated = await _repository.UpdateAsync(existing);
        return _mapper.Map<MeasurementDto>(updated);
    }

    public async Task DeleteAsync(Guid id)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
            throw new KeyNotFoundException($"Замер с id {id} не найдена");

        await _repository.DeleteAsync(existing);
    }

    public async Task<IEnumerable<MeasurementDto>> BulkCreateAsync(IEnumerable<CreateMeasurementRequest> requests)
    {
        var measurements = new List<HumidityMeasurement>();

        foreach (var request in requests)
        {
            var vehicleExists = await _vehicleRepository.ExistsAsync(request.VehicleId);
            if (!vehicleExists)
                continue; // Пропускаем замеры для несуществующих машин

            if (!DateTime.TryParse(request.Timestamp, out var parsedTimestamp))
                continue;

            if (!Enum.TryParse<MeasurementSource>(request.Source, true, out var source))
                source = MeasurementSource.Auto;

            var measurement = _mapper.Map<HumidityMeasurement>(request);
            measurement.Timestamp = DateTime.SpecifyKind(parsedTimestamp, DateTimeKind.Utc);
            measurement.Source = source;
            measurement.CreatedAt = DateTime.UtcNow;
            measurement.Id = Guid.NewGuid();

            measurements.Add(measurement);
        }

        var created = await _repository.BulkAddAsync(measurements);
        return _mapper.Map<IEnumerable<MeasurementDto>>(created);
    }
}