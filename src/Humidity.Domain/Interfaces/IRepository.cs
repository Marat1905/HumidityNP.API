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
    /// <param name="cancellationToken">Токен отмены операции.</param>
    Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить запись по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор записи.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Сущность или null, если не найдена.</returns>
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Проверить существование записи по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор записи.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>true, если запись существует.</returns>
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Добавить новую запись.
    /// </summary>
    /// <param name="entity">Сущность для добавления.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Добавленная сущность.</returns>
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Массовое добавление записей.
    /// </summary>
    /// <param name="entities">Коллекция сущностей для добавления.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Добавленные сущности.</returns>
    Task<IEnumerable<T>> BulkAddAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Обновить существующую запись.
    /// </summary>
    /// <param name="entity">Сущность с обновлёнными данными.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Обновлённая сущность.</returns>
    Task<T> UpdateAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Удалить запись.
    /// </summary>
    /// <param name="entity">Сущность для удаления.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    Task DeleteAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Удалить запись по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор записи для удаления.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>true, если запись была найдена и удалена.</returns>
    Task<bool> DeleteByIdAsync(Guid id, CancellationToken cancellationToken = default);

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