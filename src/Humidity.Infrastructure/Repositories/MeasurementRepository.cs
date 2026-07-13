using Microsoft.EntityFrameworkCore;
using Humidity.Domain.Entities;
using Humidity.Domain.Interfaces;
using Humidity.Infrastructure.Data;

namespace Humidity.Infrastructure.Repositories;

/// <summary>
/// Репозиторий для работы с сущностью HumidityMeasurement.
/// </summary>
public class MeasurementRepository : IMeasurementRepository
{
    private readonly HumidityDbContext _context;
    private readonly DbSet<HumidityMeasurement> _dbSet;

    public MeasurementRepository(HumidityDbContext context)
    {
        _context = context;
        _dbSet = context.Set<HumidityMeasurement>();
    }

    public async Task<IEnumerable<HumidityMeasurement>> GetByVehicleIdAsync(Guid vehicleId)
    {
        return await _dbSet
            .Where(m => m.VehicleId == vehicleId)
            .OrderByDescending(m => m.Timestamp)
            .ToListAsync();
    }

    public async Task<HumidityMeasurement?> GetLatestByVehicleIdAsync(Guid vehicleId)
    {
        return await _dbSet
            .Where(m => m.VehicleId == vehicleId)
            .OrderByDescending(m => m.Timestamp)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<HumidityMeasurement>> GetByDateAsync(DateTime date)
    {
        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1).AddTicks(-1);

        return await _dbSet
            .Where(m => m.Timestamp >= startOfDay && m.Timestamp <= endOfDay)
            .OrderByDescending(m => m.Timestamp)
            .ToListAsync();
    }

    public async Task<HumidityMeasurement?> GetByIdAsync(Guid id)
    {
        return await _dbSet.FindAsync(id);
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _dbSet.AnyAsync(m => m.Id == id);
    }

    public async Task<HumidityMeasurement> AddAsync(HumidityMeasurement entity)
    {
        entity.Id = Guid.NewGuid();
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<IEnumerable<HumidityMeasurement>> BulkAddAsync(IEnumerable<HumidityMeasurement> entities)
    {
        await _dbSet.AddRangeAsync(entities);
        await _context.SaveChangesAsync();
        return entities;
    }

    public async Task<HumidityMeasurement> UpdateAsync(HumidityMeasurement entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task DeleteAsync(HumidityMeasurement entity)
    {
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
    }
}