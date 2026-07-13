using Microsoft.EntityFrameworkCore;
using Humidity.Domain.Entities;
using Humidity.Domain.Interfaces;
using Humidity.Infrastructure.Data;

namespace Humidity.Infrastructure.Repositories;

/// <summary>
/// Репозиторий для работы с сущностью Vehicle.
/// Наследует все базовые CRUD-операции от BaseRepository.
/// Реализует только методы, специфичные для машин.
/// </summary>
public class VehicleRepository : BaseRepository<Vehicle>, IVehicleRepository
{
    public VehicleRepository(HumidityDbContext context)
        : base(context)
    {
    }

    /// <summary>
    /// Переопределяем базовый GetAllAsync, чтобы eagerly load замеры.
    /// Это позволяет избежать N+1 проблемы при сериализации.
    /// </summary>
    public override async Task<IEnumerable<Vehicle>> GetAllAsync()
    {
        return await DbSet
            .Include(v => v.Measurements)
            .OrderByDescending(v => v.Date)
            .ToListAsync();
    }

    /// <summary>
    /// Переопределяем GetByIdAsync, чтобы eagerly load замеры.
    /// </summary>
    public override async Task<Vehicle?> GetByIdAsync(Guid id)
    {
        return await DbSet
            .Include(v => v.Measurements)
            .FirstOrDefaultAsync(v => v.Id == id);
    }

    /// <summary>
    /// Получить список машин, которые ещё не выехали (ExitDate = null).
    /// </summary>
    public async Task<IEnumerable<Vehicle>> GetActiveVehiclesAsync()
    {
        return await DbSet
            .Where(v => v.ExitDate == null)
            .OrderByDescending(v => v.EntryDate)
            .ToListAsync();
    }

    /// <summary>
    /// Найти машины по государственному номеру (без учёта регистра).
    /// </summary>
    public async Task<IEnumerable<Vehicle>> GetByPlateAsync(string plate)
    {
        return await DbSet
            .Where(v => v.VehiclePlate.ToLower() == plate.ToLower())
            .OrderByDescending(v => v.Date)
            .ToListAsync();
    }

    /// <summary>
    /// Найти машины по номеру заявки (без учёта регистра).
    /// </summary>
    public async Task<IEnumerable<Vehicle>> GetByNumberAsync(string number)
    {
        return await DbSet
            .Where(v => v.Number.ToLower() == number.ToLower())
            .OrderByDescending(v => v.Date)
            .ToListAsync();
    }
}