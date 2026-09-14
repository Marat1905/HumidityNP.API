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
    /// Используется для синхронизации с 1С, чтобы однозначно идентифицировать запись (резервный метод).
    /// </summary>
    /// <param name="number">Номер пропуска.</param>
    /// <param name="date">Дата создания пропуска.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Сущность Vehicle или null, если не найдена.</returns>
    Task<Vehicle?> GetByNumberAndDateAsync(string number, DateTimeOffset date, CancellationToken cancellationToken = default);

    /// <summary>
    /// Найти машину по уникальному идентификатору 1С (ГУИД).
    /// Основной метод для контроля уникальности при синхронизации.
    /// </summary>
    /// <param name="oneCGuid">Уникальный идентификатор из 1С.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Сущность Vehicle или null, если не найдена.</returns>
    Task<Vehicle?> GetByOneCGuidAsync(string oneCGuid, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить страницу машин с применением фильтров:
    ///  - по поставщику (частичное совпадение, регистронезависимо);
    ///  - по статусу (активные / выехавшие / все);
    ///  - по государственному номеру (частичное совпадение);
    ///  - по ФИО водителя (частичное совпадение);
    ///  - по диапазону даты въезда (EntryDate, включительно с обеих сторон).
    ///
    /// Все фильтры применяются на стороне БД. Параметры с null не участвуют в отборе.
    /// </summary>
    /// <param name="pageNumber">Номер страницы.</param>
    /// <param name="pageSize">Размер страницы.</param>
    /// <param name="counterparty">Частичное совпадение с наименованием поставщика (регистронезависимо).</param>
    /// <param name="isActive">true – только активные (ExitDate == null), false – только выехавшие, null – все.</param>
    /// <param name="plate">Частичное совпадение с госномером (регистронезависимо).</param>
    /// <param name="driver">Частичное совпадение с ФИО водителя (регистронезависимо).</param>
    /// <param name="entryDateFrom">Минимальная дата въезда (включительно). null — без ограничения снизу.</param>
    /// <param name="entryDateTo">Максимальная дата въезда (включительно). null — без ограничения сверху.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Страница машин, соответствующих фильтрам.</returns>
    Task<PagedResult<Vehicle>> GetFilteredPagedAsync(
        int pageNumber,
        int pageSize,
        string? counterparty,
        bool? isActive,
        string? plate,
        string? driver,
        DateTimeOffset? entryDateFrom = null,
        DateTimeOffset? entryDateTo = null,
        CancellationToken cancellationToken = default);
}