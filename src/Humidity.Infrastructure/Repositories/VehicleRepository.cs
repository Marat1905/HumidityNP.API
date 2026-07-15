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
    /// </summary>
    public override async Task<IEnumerable<Vehicle>> GetAllAsync()
    {
        return await _context.Vehicles
            .Include(v => v.Measurements)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <summary>
    /// Получить машину по идентификатору.
    /// Возвращает отслеживаемую сущность для возможности последующего обновления.
    /// </summary>
    public override async Task<Vehicle?> GetByIdAsync(Guid id)
    {
        // Убрали AsNoTracking(), чтобы сущность отслеживалась контекстом
        return await _context.Vehicles
            .Include(v => v.Measurements)
            .FirstOrDefaultAsync(v => v.Id == id);
    }

    /// <summary>
    /// Получить активные машины (те, у которых дата выезда ещё не установлена).
    /// Только для чтения, используется AsNoTracking.
    /// </summary>
    public async Task<IEnumerable<Vehicle>> GetActiveVehiclesAsync()
    {
        return await _context.Vehicles
            .Include(v => v.Measurements)
            .Where(v => v.ExitDate == null)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <summary>
    /// Получить страницу активных машин.
    /// Только для чтения, используется AsNoTracking.
    /// </summary>
    public async Task<PagedResult<Vehicle>> GetActiveVehiclesPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        // Защита от невалидных значений
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        IQueryable<Vehicle> query = _context.Vehicles
            .Where(v => v.ExitDate == null)
            .Include(v => v.Measurements); // при необходимости можно загружать связанные данные

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
    /// Использует EF.Functions.ILike с символами % для поиска по подстроке,
    /// что позволяет найти все машины, номер которых содержит переданную строку.
    /// Для эффективного поиска в PostgreSQL рекомендуется создать GIN-индекс с расширением pg_trgm.
    /// Только для чтения, используется AsNoTracking.
    /// </summary>
    public async Task<IEnumerable<Vehicle>> GetByPlateAsync(string plate)
    {
        // Добавляем % с обеих сторон для поиска по подстроке
        var searchPattern = $"%{plate}%";
        return await _context.Vehicles
            .Include(v => v.Measurements)
            .AsNoTracking()
            .Where(v => EF.Functions.ILike(v.VehiclePlate, searchPattern))
            .ToListAsync();
    }

    /// <summary>
    /// Поиск машин по номеру заявки (регистронезависимый, частичное совпадение).
    /// Возвращает коллекцию согласно сигнатуре интерфейса.
    /// Использует EF.Functions.ILike с символами % для поиска по подстроке.
    /// Только для чтения, используется AsNoTracking.
    /// </summary>
    public async Task<IEnumerable<Vehicle>> GetByNumberAsync(string number)
    {
        // Добавляем % с обеих сторон для поиска по подстроке
        var searchPattern = $"%{number}%";
        return await _context.Vehicles
            .Include(v => v.Measurements)
            .AsNoTracking()
            .Where(v => EF.Functions.ILike(v.Number, searchPattern))
            .ToListAsync();
    }

    /// <summary>
    /// Проверить существование машины по идентификатору.
    /// </summary>
    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.Vehicles.AnyAsync(v => v.Id == id);
    }

    /// <summary>
    /// Получить множество существующих идентификаторов машин из переданного списка.
    /// Выполняет один запрос к БД вместо N запросов.
    /// </summary>
    public async Task<HashSet<Guid>> GetExistingIdsAsync(IEnumerable<Guid> ids)
    {
        var idList = ids.ToList();
        if (!idList.Any())
            return new HashSet<Guid>();

        // Один запрос к БД: SELECT "Id" FROM "Vehicles" WHERE "Id" IN (...)
        var existingIds = await _context.Vehicles
            .Where(v => idList.Contains(v.Id))
            .Select(v => v.Id)
            .ToListAsync();

        return new HashSet<Guid>(existingIds);
    }
}