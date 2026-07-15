using Humidity.Domain.Common;
using System.Linq.Expressions;

namespace Humidity.Domain.Interfaces;

/// <summary>
/// Базовый интерфейс репозитория с общими операциями CRUD.
/// </summary>
/// <typeparam name="T">Тип сущности, должен наследоваться от BaseEntity.</typeparam>
public interface IRepository<T> where T : class
{
    /// <summary>
    /// Получить все записи.
    /// </summary>
    Task<IEnumerable<T>> GetAllAsync();

    /// <summary>
    /// Получить запись по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор записи.</param>
    /// <returns>Сущность или null, если не найдена.</returns>
    Task<T?> GetByIdAsync(Guid id);

    /// <summary>
    /// Проверить существование записи по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор записи.</param>
    /// <returns>true, если запись существует.</returns>
    Task<bool> ExistsAsync(Guid id);

    /// <summary>
    /// Добавить новую запись.
    /// </summary>
    /// <param name="entity">Сущность для добавления.</param>
    /// <returns>Добавленная сущность.</returns>
    Task<T> AddAsync(T entity);

    /// <summary>
    /// Массовое добавление записей.
    /// </summary>
    /// <param name="entities">Коллекция сущностей для добавления.</param>
    /// <returns>Добавленные сущности.</returns>
    Task<IEnumerable<T>> BulkAddAsync(IEnumerable<T> entities);

    /// <summary>
    /// Обновить существующую запись.
    /// </summary>
    /// <param name="entity">Сущность с обновлёнными данными.</param>
    /// <returns>Обновлённая сущность.</returns>
    Task<T> UpdateAsync(T entity);

    /// <summary>
    /// Удалить запись.
    /// </summary>
    /// <param name="entity">Сущность для удаления.</param>
    Task DeleteAsync(T entity);

    /// <summary>
    /// Удалить запись по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор записи для удаления.</param>
    /// <returns>true, если запись была найдена и удалена.</returns>
    Task<bool> DeleteByIdAsync(Guid id);

    /// <summary>
    /// Получить страницу записей с возможностью применения фильтра.
    /// </summary>
    /// <param name="pageNumber">Номер страницы (начиная с 1).</param>
    /// <param name="pageSize">Размер страницы.</param>
    /// <param name="filter">Фильтр (опционально).</param>
    /// <param name="orderBy">Функция сортировки (опционально).</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Объект <see cref="PagedResult{T}"/> с элементами страницы и метаданными.</returns>
    Task<PagedResult<T>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Expression<Func<T, bool>>? filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        CancellationToken cancellationToken = default);
}