using Humidity.Domain.Common;
using Humidity.Domain.Entities;
using Humidity.Domain.Interfaces;
using Humidity.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Humidity.Infrastructure.Repositories;

/// <summary>
/// Абстрактная реализация базового репозитория с общими операциями CRUD.
/// Все конкретные репозитории наследуются от этого класса, 
/// получая готовую реализацию стандартных методов и переопределяя только специфические.
/// </summary>
/// <typeparam name="T">Тип сущности, должен наследоваться от BaseEntity.</typeparam>
public abstract class BaseRepository<T> : IRepository<T> where T : BaseEntity
{
    /// <summary>
    /// Контекст базы данных.
    /// </summary>
    protected readonly HumidityDbContext Context;

    /// <summary>
    /// Набор данных для текущей сущности.
    /// </summary>
    protected readonly DbSet<T> DbSet;

    /// <summary>
    /// Конструктор, принимающий контекст БД и инициализирующий набор данных.
    /// </summary>
    /// <param name="context">Контекст базы данных.</param>
    protected BaseRepository(HumidityDbContext context)
    {
        Context = context;
        DbSet = context.Set<T>();
    }

    /// <summary>
    /// Получить все записи, отсортированные по дате создания (новые первыми).
    /// Может быть переопределён в наследниках для изменения сортировки или добавления Include.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Получить запись по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор записи.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet.FindAsync(new object[] { id }, cancellationToken);
    }

    /// <summary>
    /// Проверить существование записи.
    /// </summary>
    /// <param name="id">Идентификатор записи.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    public virtual async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(e => e.Id == id, cancellationToken);
    }

    /// <summary>
    /// Добавить новую запись. Автоматически генерирует Guid, если он пустой.
    /// </summary>
    /// <param name="entity">Сущность для добавления.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    public virtual async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        if (entity.Id == Guid.Empty)
        {
            entity.Id = Guid.NewGuid();
        }

        await DbSet.AddAsync(entity, cancellationToken);
        await Context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    /// <summary>
    /// Массовое добавление записей.
    /// </summary>
    /// <param name="entities">Коллекция сущностей для добавления.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    public virtual async Task<IEnumerable<T>> BulkAddAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        var entityList = entities.ToList();

        foreach (var entity in entityList)
        {
            if (entity.Id == Guid.Empty)
            {
                entity.Id = Guid.NewGuid();
            }
        }

        await DbSet.AddRangeAsync(entityList, cancellationToken);
        await Context.SaveChangesAsync(cancellationToken);
        return entityList;
    }

    /// <summary>
    /// Обновить существующую запись.
    /// </summary>
    /// <param name="entity">Сущность с обновлёнными данными.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    public virtual async Task<T> UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        DbSet.Update(entity);
        await Context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    /// <summary>
    /// Удалить запись.
    /// </summary>
    /// <param name="entity">Сущность для удаления.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    public virtual async Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
    {
        DbSet.Remove(entity);
        await Context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Удалить запись по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор записи для удаления.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    public virtual async Task<bool> DeleteByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        if (entity == null)
            return false;

        DbSet.Remove(entity);
        await Context.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// Получить страницу записей с возможностью применения фильтра и сортировки.
    /// </summary>
    /// <param name="pageNumber">Номер страницы (начиная с 1).</param>
    /// <param name="pageSize">Размер страницы.</param>
    /// <param name="filter">Фильтр (опционально).</param>
    /// <param name="orderBy">Функция сортировки (опционально).</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    public virtual async Task<PagedResult<T>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Expression<Func<T, bool>>? filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        CancellationToken cancellationToken = default)
    {
        // Защита от невалидных значений
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100; // ограничиваем максимальный размер страницы

        IQueryable<T> query = DbSet;

        if (filter != null)
        {
            query = query.Where(filter);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        if (orderBy != null)
        {
            query = orderBy(query);
        }
        else
        {
            // Сортировка по умолчанию – по CreatedAt убыванию (новые первыми)
            query = query.OrderByDescending(e => e.CreatedAt);
        }

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<T>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }
}