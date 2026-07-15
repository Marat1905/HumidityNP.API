using Humidity.Domain.Common;
using Humidity.Domain.Entities;
using Humidity.Domain.Interfaces;
using Humidity.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Humidity.Infrastructure.Repositories;

/// <summary>
/// Репозиторий для работы с сущностью HumidityMeasurement.
/// Наследует все базовые CRUD-операции от BaseRepository.
/// Реализует только методы, специфичные для замеров влажности.
/// </summary>
public class MeasurementRepository : BaseRepository<HumidityMeasurement>, IMeasurementRepository
{
    public MeasurementRepository(HumidityDbContext context)
        : base(context)
    {
    }

    /// <summary>
    /// Переопределяем базовый GetAllAsync, чтобы eagerly load связанную машину.
    /// </summary>
    public override async Task<IEnumerable<HumidityMeasurement>> GetAllAsync()
    {
        return await DbSet
            .Include(m => m.Vehicle)
            .OrderByDescending(m => m.Timestamp)
            .ToListAsync();
    }

    /// <summary>
    /// Получить все замеры для указанной машины, отсортированные по времени (новые первыми).
    /// </summary>
    public async Task<IEnumerable<HumidityMeasurement>> GetByVehicleIdAsync(Guid vehicleId)
    {
        return await DbSet
            .Where(m => m.VehicleId == vehicleId)
            .OrderByDescending(m => m.Timestamp)
            .ToListAsync();
    }

    /// <summary>
    /// Получить страницу замеров для указанной машины.
    /// </summary>
    public async Task<PagedResult<HumidityMeasurement>> GetByVehicleIdPagedAsync(Guid vehicleId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        IQueryable<HumidityMeasurement> query = DbSet
            .Where(m => m.VehicleId == vehicleId)
            .Include(m => m.Vehicle);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(m => m.Timestamp)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<HumidityMeasurement>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    /// <summary>
    /// Получить последний (самый свежий) замер для указанной машины.
    /// </summary>
    public async Task<HumidityMeasurement?> GetLatestByVehicleIdAsync(Guid vehicleId)
    {
        return await DbSet
            .Where(m => m.VehicleId == vehicleId)
            .OrderByDescending(m => m.Timestamp)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Получить все замеры за указанную дату (весь день от 00:00:00 до 23:59:59.9999999).
    /// </summary>
    public async Task<IEnumerable<HumidityMeasurement>> GetByDateAsync(DateTime date)
    {
        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1).AddTicks(-1);

        return await DbSet
            .Where(m => m.Timestamp >= startOfDay && m.Timestamp <= endOfDay)
            .OrderByDescending(m => m.Timestamp)
            .ToListAsync();
    }

    /// <summary>
    /// Получить страницу замеров за указанную дату.
    /// </summary>
    public async Task<PagedResult<HumidityMeasurement>> GetByDatePagedAsync(DateTime date, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1).AddTicks(-1);

        IQueryable<HumidityMeasurement> query = DbSet
            .Where(m => m.Timestamp >= startOfDay && m.Timestamp <= endOfDay)
            .Include(m => m.Vehicle);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(m => m.Timestamp)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<HumidityMeasurement>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    /// <summary>
    /// Получить замеры в произвольном диапазоне дат.
    /// </summary>
    public async Task<IEnumerable<HumidityMeasurement>> GetByDateRangeAsync(DateTime from, DateTime to)
    {
        return await DbSet
            .Where(m => m.Timestamp >= from && m.Timestamp <= to)
            .OrderByDescending(m => m.Timestamp)
            .ToListAsync();
    }
}