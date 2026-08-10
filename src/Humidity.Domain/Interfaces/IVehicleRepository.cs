// Domain/Interfaces/IVehicleRepository.cs
using Humidity.Domain.Common;
using Humidity.Domain.Entities;

namespace Humidity.Domain.Interfaces;

/// <summary>
/// Интерфейс репозитория для работы с машинами.
/// Расширяет базовый IRepository дополнительными методами, специфичными для Vehicle.
/// </summary>
public interface IVehicleRepository : IRepository<Vehicle>
{
    /// <summary>
    /// Получить список машин, которые ещё не выехали (ExitDate = null).
    /// </summary>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Коллекция активных машин.</returns>
    Task<IEnumerable<Vehicle>> GetActiveVehiclesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить страницу активных машин.
    /// </summary>
    /// <param name="pageNumber">Номер страницы.</param>
    /// <param name="pageSize">Размер страницы.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Страница активных машин.</returns>
    Task<PagedResult<Vehicle>> GetActiveVehiclesPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Найти машины по государственному номеру.
    /// </summary>
    /// <param name="plate">Государственный номер.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Коллекция машин с указанным номером.</returns>
    Task<IEnumerable<Vehicle>> GetByPlateAsync(string plate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Найти машины по номеру заявки.
    /// </summary>
    /// <param name="number">Номер заявки.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Коллекция машин с указанным номером заявки.</returns>
    Task<IEnumerable<Vehicle>> GetByNumberAsync(string number, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить множество существующих идентификаторов машин из переданного списка.
    /// Выполняет один запрос к БД вместо N запросов.
    /// </summary>
    /// <param name="ids">Список проверяемых идентификаторов.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>HashSet существующих идентификаторов для быстрого поиска.</returns>
    Task<HashSet<Guid>> GetExistingIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);

    /// <summary>
    /// Найти машину по номеру пропуска и дате создания пропуска.
    /// Используется для синхронизации с 1С, чтобы однозначно идентифицировать запись.
    /// </summary>
    /// <param name="number">Номер пропуска.</param>
    /// <param name="date">Дата создания пропуска.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Сущность Vehicle или null, если не найдена.</returns>
    Task<Vehicle?> GetByNumberAndDateAsync(string number, DateTimeOffset date, CancellationToken cancellationToken = default);

    // НОВЫЙ МЕТОД для фильтрации с пагинацией
    /// <summary>
    /// Получить страницу машин с применением фильтров по поставщику, статусу, госномеру и водителю.
    /// Фильтрация выполняется на стороне базы данных.
    /// </summary>
    /// <param name="pageNumber">Номер страницы.</param>
    /// <param name="pageSize">Размер страницы.</param>
    /// <param name="counterparty">Частичное совпадение с наименованием поставщика (регистронезависимо).</param>
    /// <param name="isActive">true – только активные (ExitDate == null), false – только выехавшие, null – все.</param>
    /// <param name="plate">Частичное совпадение с госномером (регистронезависимо).</param>
    /// <param name="driver">Частичное совпадение с ФИО водителя (регистронезависимо).</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Страница машин, соответствующих фильтрам.</returns>
    Task<PagedResult<Vehicle>> GetFilteredPagedAsync(
        int pageNumber,
        int pageSize,
        string? counterparty,
        bool? isActive,
        string? plate,
        string? driver,
        CancellationToken cancellationToken = default);
}