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
    /// </summary>
    public override async Task<Vehicle?> GetByIdAsync(Guid id)
    {
        return await _context.Vehicles
            .Include(v => v.Measurements)
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == id);
    }

    /// <summary>
    /// Получить активные машины (те, у которых дата выезда ещё не установлена).
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
    /// Поиск машин по государственному номеру (регистронезависимый).
    /// Использует EF.Functions.ILike для эффективного поиска в PostgreSQL,
    /// который позволяет использовать функциональный индекс LOWER(vehicle_plate).
    /// Возвращает коллекцию, так как один и тот же гос. номер может встречаться
    /// у разных заявок в разное время (например, одна машина — несколько визитов).
    /// </summary>
    public async Task<IEnumerable<Vehicle>> GetByPlateAsync(string plate)
    {
        return await _context.Vehicles
            .Include(v => v.Measurements)
            .AsNoTracking()
            .Where(v => EF.Functions.ILike(v.VehiclePlate, plate))
            .ToListAsync();
    }

    /// <summary>
    /// Поиск машин по номеру заявки (регистронезависимый).
    /// Возвращает коллекцию согласно сигнатуре интерфейса.
    /// Использует EF.Functions.ILike для единообразия с поиском по гос. номеру.
    /// </summary>
    public async Task<IEnumerable<Vehicle>> GetByNumberAsync(string number)
    {
        return await _context.Vehicles
            .Include(v => v.Measurements)
            .AsNoTracking()
            .Where(v => EF.Functions.ILike(v.Number, number))
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