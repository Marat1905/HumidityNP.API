using Humidity.Application.DTOs;

namespace Humidity.Application.Interfaces;

/// <summary>
/// Сервис для управления записями о машинах (CRUD, фильтрация).
/// </summary>
public interface IVehicleService
{
    /// <summary>
    /// Получить список всех машин.
    /// </summary>
    /// <returns>Коллекция DTO машин.</returns>
    Task<IEnumerable<VehicleDto>> GetAllAsync();

    /// <summary>
    /// Получить машину по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор машины.</param>
    /// <returns>DTO машины или null, если не найдена.</returns>
    Task<VehicleDto?> GetByIdAsync(Guid id);

    /// <summary>
    /// Получить список машин, которые ещё не выехали (ExitDate = null).
    /// </summary>
    /// <returns>Коллекция DTO машин на площадке.</returns>
    Task<IEnumerable<VehicleDto>> GetActiveVehiclesAsync();

    /// <summary>
    /// Создать новую запись о машине.
    /// </summary>
    /// <param name="request">Данные для создания.</param>
    /// <returns>DTO созданной машины.</returns>
    Task<VehicleDto> CreateAsync(CreateVehicleRequest request);

    /// <summary>
    /// Обновить существующую запись о машине.
    /// </summary>
    /// <param name="id">Идентификатор машины.</param>
    /// <param name="request">Данные для обновления.</param>
    /// <returns>DTO обновлённой машины.</returns>
    Task<VehicleDto> UpdateAsync(Guid id, UpdateVehicleRequest request);

    /// <summary>
    /// Удалить запись о машине вместе со всеми связанными замерами.
    /// </summary>
    /// <param name="id">Идентификатор машины.</param>
    Task DeleteAsync(Guid id);
}