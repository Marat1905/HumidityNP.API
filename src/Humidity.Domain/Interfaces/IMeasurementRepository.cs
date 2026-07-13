using Humidity.Domain.Entities;

namespace Humidity.Domain.Interfaces;

/// <summary>
/// Интерфейс репозитория для работы с замерами влажности.
/// </summary>
public interface IMeasurementRepository
{
    Task<IEnumerable<HumidityMeasurement>> GetByVehicleIdAsync(Guid vehicleId);
    Task<HumidityMeasurement?> GetLatestByVehicleIdAsync(Guid vehicleId);
    Task<IEnumerable<HumidityMeasurement>> GetByDateAsync(DateTime date);
    Task<HumidityMeasurement?> GetByIdAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
    Task<HumidityMeasurement> AddAsync(HumidityMeasurement entity);
    Task<IEnumerable<HumidityMeasurement>> BulkAddAsync(IEnumerable<HumidityMeasurement> entities);
    Task<HumidityMeasurement> UpdateAsync(HumidityMeasurement entity);
    Task DeleteAsync(HumidityMeasurement entity);
}