using Humidity.Domain.Common;
using Humidity.Domain.Entities;
using Humidity.Domain.Interfaces;
using Humidity.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Humidity.Infrastructure.Repositories;

/// <summary>
/// Реализация репозитория для работы с сущностью Vehicle.
/// Наследует базовый репозиторий и добавляет специфичные методы для машин.
/// </summary>
public class VehicleRepository : BaseRepository<Vehicle>, IVehicleRepository
{
    private readonly HumidityDbContext _context;

    public VehicleRepository(HumidityDbContext context) : base(context)
    {
        _context = context;
    }

    /// <summary>
    /// Получить все машины.
    /// Использует AsNoTracking для повышения производительности при чтении.
    /// Связанные измерения не загружаются, так как они не требуются в большинстве сценариев.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    public override async Task<IEnumerable<Vehicle>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Vehicles
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Получить машину по идентификатору.
    /// Возвращает отслеживаемую сущность для возможности последующего обновления.
    /// Связанные измерения не загружаются, так как они не нужны для обновления.
    /// </summary>
    /// <param name="id">Идентификатор машины.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    public override async Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Vehicles
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
    }

    /// <summary>
    /// Получить активные машины (те, у которых дата выезда ещё не установлена).
    /// Только для чтения, используется AsNoTracking.
    /// Связанные измерения не загружаются.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    public async Task<IEnumerable<Vehicle>> GetActiveVehiclesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Vehicles
            .Where(v => v.ExitDate == null)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Получить страницу активных машин.
    /// Только для чтения, используется AsNoTracking.
    /// Связанные измерения не загружаются.
    /// </summary>
    /// <param name="pageNumber">Номер страницы.</param>
    /// <param name="pageSize">Размер страницы.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    public async Task<PagedResult<Vehicle>> GetActiveVehiclesPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        // Защита от невалидных значений
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        IQueryable<Vehicle> query = _context.Vehicles
            .Where(v => v.ExitDate == null);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(v => v.CreatedAt) // сортировка по умолчанию
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking() // для чтения
            .ToListAsync(cancellationToken);

        return new PagedResult<Vehicle>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    /// <summary>
    /// Поиск машин по государственному номеру (регистронезависимый, частичное совпадение).
    /// Использует EF.Functions.ILike с символами % для поиска по подстроке.
    /// Только для чтения, используется AsNoTracking.
    /// Связанные измерения не загружаются.
    /// </summary>
    /// <param name="plate">Государственный номер.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    public async Task<IEnumerable<Vehicle>> GetByPlateAsync(string plate, CancellationToken cancellationToken = default)
    {
        var searchPattern = $"%{plate}%";
        return await _context.Vehicles
            .AsNoTracking()
            .Where(v => EF.Functions.ILike(v.VehiclePlate, searchPattern))
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Поиск машин по номеру заявки (регистронезависимый, частичное совпадение).
    /// Использует EF.Functions.ILike с символами % для поиска по подстроке.
    /// Только для чтения, используется AsNoTracking.
    /// Связанные измерения не загружаются.
    /// </summary>
    /// <param name="number">Номер заявки.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    public async Task<IEnumerable<Vehicle>> GetByNumberAsync(string number, CancellationToken cancellationToken = default)
    {
        var searchPattern = $"%{number}%";
        return await _context.Vehicles
            .AsNoTracking()
            .Where(v => EF.Functions.ILike(v.Number, searchPattern))
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Получить множество существующих идентификаторов машин из переданного списка.
    /// Выполняет один запрос к БД вместо N запросов.
    /// </summary>
    /// <param name="ids">Список проверяемых идентификаторов.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    public async Task<HashSet<Guid>> GetExistingIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        if (!idList.Any())
            return new HashSet<Guid>();

        var existingIds = await _context.Vehicles
            .Where(v => idList.Contains(v.Id))
            .Select(v => v.Id)
            .ToListAsync(cancellationToken);

        return new HashSet<Guid>(existingIds);
    }

    /// <summary>
    /// Переопределение метода GetPagedAsync с использованием AsNoTracking() для read‑only запросов.
    /// </summary>
    public override async Task<PagedResult<Vehicle>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        System.Linq.Expressions.Expression<Func<Vehicle, bool>>? filter = null,
        Func<IQueryable<Vehicle>, IOrderedQueryable<Vehicle>>? orderBy = null,
        CancellationToken cancellationToken = default)
    {
        // Защита от невалидных значений
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        IQueryable<Vehicle> query = DbSet;

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
            .AsNoTracking() // Добавлено AsNoTracking для повышения производительности
            .ToListAsync(cancellationToken);

        return new PagedResult<Vehicle>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }
}