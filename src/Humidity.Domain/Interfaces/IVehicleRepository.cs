using Humidity.Domain.Entities;

namespace Humidity.Domain.Interfaces;

/// <summary>
/// Интерфейс репозитория для работы с машинами.
/// </summary>
public interface IVehicleRepository
{
    Task<IEnumerable<Vehicle>> GetAllAsync();
    Task<Vehicle?> GetByIdAsync(Guid id);
    Task<IEnumerable<Vehicle>> GetActiveVehiclesAsync();
    Task<bool> ExistsAsync(Guid id);
    Task<Vehicle> AddAsync(Vehicle entity);
    Task<Vehicle> UpdateAsync(Vehicle entity);
    Task DeleteAsync(Vehicle entity);
}