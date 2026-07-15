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
    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        return await DbSet
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Получить запись по идентификатору.
    /// </summary>
    public virtual async Task<T?> GetByIdAsync(Guid id)
    {
        return await DbSet.FindAsync(id);
    }

    /// <summary>
    /// Проверить существование записи.
    /// </summary>
    public virtual async Task<bool> ExistsAsync(Guid id)
    {
        return await DbSet.AnyAsync(e => e.Id == id);
    }

    /// <summary>
    /// Добавить новую запись. Автоматически генерирует Guid, если он пустой.
    /// </summary>
    public virtual async Task<T> AddAsync(T entity)
    {
        if (entity.Id == Guid.Empty)
        {
            entity.Id = Guid.NewGuid();
        }

        await DbSet.AddAsync(entity);
        await Context.SaveChangesAsync();
        return entity;
    }

    /// <summary>
    /// Массовое добавление записей.
    /// </summary>
    public virtual async Task<IEnumerable<T>> BulkAddAsync(IEnumerable<T> entities)
    {
        var entityList = entities.ToList();

        foreach (var entity in entityList)
        {
            if (entity.Id == Guid.Empty)
            {
                entity.Id = Guid.NewGuid();
            }
        }

        await DbSet.AddRangeAsync(entityList);
        await Context.SaveChangesAsync();
        return entityList;
    }

    /// <summary>
    /// Обновить существующую запись.
    /// </summary>
    public virtual async Task<T> UpdateAsync(T entity)
    {
        DbSet.Update(entity);
        await Context.SaveChangesAsync();
        return entity;
    }

    /// <summary>
    /// Удалить запись.
    /// </summary>
    public virtual async Task DeleteAsync(T entity)
    {
        DbSet.Remove(entity);
        await Context.SaveChangesAsync();
    }

    /// <summary>
    /// Удалить запись по идентификатору.
    /// </summary>
    public virtual async Task<bool> DeleteByIdAsync(Guid id)
    {
        var entity = await GetByIdAsync(id);
        if (entity == null)
            return false;

        DbSet.Remove(entity);
        await Context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Получить страницу записей с возможностью применения фильтра и сортировки.
    /// </summary>
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