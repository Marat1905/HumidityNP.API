using Microsoft.EntityFrameworkCore;
using Humidity.Domain.Entities;
using Humidity.Domain.Interfaces;
using Humidity.Infrastructure.Data;

namespace Humidity.Infrastructure.Repositories;

/// <summary>
/// Репозиторий для работы с сущностью Vehicle.
/// </summary>
public class VehicleRepository : IVehicleRepository
{
    private readonly HumidityDbContext _context;
    private readonly DbSet<Vehicle> _dbSet;

    public VehicleRepository(HumidityDbContext context)
    {
        _context = context;
        _dbSet = context.Set<Vehicle>();
    }

    public async Task<IEnumerable<Vehicle>> GetAllAsync()
    {
        return await _dbSet
            .OrderByDescending(v => v.Date)
            .ToListAsync();
    }

    public async Task<Vehicle?> GetByIdAsync(Guid id)
    {
        return await _dbSet.FindAsync(id);
    }

    public async Task<IEnumerable<Vehicle>> GetActiveVehiclesAsync()
    {
        return await _dbSet
            .Where(v => v.ExitDate == null)
            .OrderByDescending(v => v.EntryDate)
            .ToListAsync();
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _dbSet.AnyAsync(v => v.Id == id);
    }

    public async Task<Vehicle> AddAsync(Vehicle entity)
    {
        entity.Id = Guid.NewGuid();
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<Vehicle> UpdateAsync(Vehicle entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task DeleteAsync(Vehicle entity)
    {
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
    }
}