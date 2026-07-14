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
    /// <returns>Коллекция активных машин.</returns>
    Task<IEnumerable<Vehicle>> GetActiveVehiclesAsync();

    /// <summary>
    /// Найти машины по государственному номеру.
    /// </summary>
    /// <param name="plate">Государственный номер.</param>
    /// <returns>Коллекция машин с указанным номером.</returns>
    Task<IEnumerable<Vehicle>> GetByPlateAsync(string plate);

    /// <summary>
    /// Найти машины по номеру заявки.
    /// </summary>
    /// <param name="number">Номер заявки.</param>
    /// <returns>Коллекция машин с указанным номером заявки.</returns>
    Task<IEnumerable<Vehicle>> GetByNumberAsync(string number);

    /// <summary>
    /// Получить множество существующих идентификаторов машин из переданного списка.
    /// Выполняет один запрос к БД вместо N запросов.
    /// </summary>
    /// <param name="ids">Список проверяемых идентификаторов.</param>
    /// <returns>HashSet существующих идентификаторов для быстрого поиска.</returns>
    Task<HashSet<Guid>> GetExistingIdsAsync(IEnumerable<Guid> ids);
}