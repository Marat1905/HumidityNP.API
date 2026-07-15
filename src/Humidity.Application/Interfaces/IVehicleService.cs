using Humidity.Application.DTOs;
using Humidity.Domain.Common;

namespace Humidity.Application.Interfaces;

/// <summary>
/// Сервис для управления записями о машинах (CRUD, фильтрация).
/// </summary>
public interface IVehicleService
{
    /// <summary>
    /// Получить список всех машин.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Коллекция DTO машин.</returns>
    Task<IEnumerable<VehicleDto>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить страницу всех машин.
    /// </summary>
    /// <param name="pageNumber">Номер страницы.</param>
    /// <param name="pageSize">Размер страницы.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Страница машин.</returns>
    Task<PagedResult<VehicleDto>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить машину по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор машины.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>DTO машины или null, если не найдена.</returns>
    Task<VehicleDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить список машин, которые ещё не выехали (ExitDate = null).
    /// </summary>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Коллекция DTO машин на площадке.</returns>
    Task<IEnumerable<VehicleDto>> GetActiveVehiclesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить страницу активных машин.
    /// </summary>
    /// <param name="pageNumber">Номер страницы.</param>
    /// <param name="pageSize">Размер страницы.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Страница активных машин.</returns>
    Task<PagedResult<VehicleDto>> GetActiveVehiclesPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Создать новую запись о машине.
    /// </summary>
    /// <param name="request">Данные для создания.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>DTO созданной машины.</returns>
    Task<VehicleDto> CreateAsync(CreateVehicleRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Обновить существующую запись о машине.
    /// </summary>
    /// <param name="id">Идентификатор машины.</param>
    /// <param name="request">Данные для обновления.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>DTO обновлённой машины.</returns>
    Task<VehicleDto> UpdateAsync(Guid id, UpdateVehicleRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Удалить запись о машине вместе со всеми связанными замерами.
    /// </summary>
    /// <param name="id">Идентификатор машины.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}