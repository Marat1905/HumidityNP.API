using Humidity.Application.DTOs;
using Humidity.Domain.Common;

namespace Humidity.Application.Interfaces;

/// <summary>
/// Сервис для управления записями о машинах (CRUD, фильтрация, разгрузка).
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
    /// Получить страницу машин с применением фильтров.
    /// </summary>
    /// <param name="pageNumber">Номер страницы.</param>
    /// <param name="pageSize">Размер страницы.</param>
    /// <param name="counterparty">Фильтр по поставщику (частичное совпадение).</param>
    /// <param name="isActive">Фильтр по статусу: true – активные, false – выехавшие, null – все.</param>
    /// <param name="plate">Фильтр по госномеру (частичное совпадение).</param>
    /// <param name="driver">Фильтр по водителю (частичное совпадение).</param>
    /// <param name="entryDateFrom">Минимальная дата въезда (включительно). null — без ограничения снизу.</param>
    /// <param name="entryDateTo">Максимальная дата въезда (включительно). null — без ограничения сверху.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Страница машин, соответствующих фильтрам.</returns>
    Task<PagedResult<VehicleDto>> GetFilteredPagedAsync(
        int pageNumber,
        int pageSize,
        string? counterparty,
        bool? isActive,
        string? plate,
        string? driver,
        DateTimeOffset? entryDateFrom = null,
        DateTimeOffset? entryDateTo = null,
        CancellationToken cancellationToken = default);

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

    /// <summary>
    /// Зафиксировать разгрузку машины: количество тюков, порванных тюков, вес и номер штабеля.
    /// </summary>
    /// <param name="id">Идентификатор машины.</param>
    /// <param name="request">Данные разгрузки.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Обновлённый DTO машины.</returns>
    /// <exception cref="KeyNotFoundException">Если машина не найдена.</exception>
    Task<VehicleDto> UnloadAsync(Guid id, UnloadVehicleRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить информацию о разгрузке машины и среднюю влажность по уникальному идентификатору 1С (ГУИД).
    /// </summary>
    /// <param name="oneCGuid">Уникальный идентификатор записи из 1С.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>DTO с информацией о разгрузке и средней влажности или null, если машина не найдена.</returns>
    Task<OneCVehicleUnloadDto?> GetUnloadInfoByOneCGuidAsync(string oneCGuid, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить информацию о разгрузке машин и среднюю влажность за период.
    /// Период фильтруется по дате создания пропуска (Vehicle.Date).
    /// </summary>
    /// <param name="from">Начало периода (включительно).</param>
    /// <param name="to">Конец периода (включительно).</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Коллекция DTO с информацией о разгрузке и средней влажности.</returns>
    Task<IEnumerable<OneCVehicleUnloadDto>> GetUnloadInfoByPeriodAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);
}