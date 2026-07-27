using Humidity.Domain.Common;
using Humidity.Domain.Entities;
using Humidity.Domain.Enums;
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
    /// <param name="cancellationToken">Токен отмены операции.</param>
    public override async Task<IEnumerable<HumidityMeasurement>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(m => m.Vehicle)
            .OrderByDescending(m => m.Timestamp)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Получить все замеры для указанной машины, отсортированные по времени (новые первыми).
    /// </summary>
    /// <param name="vehicleId">Идентификатор машины.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    public async Task<IEnumerable<HumidityMeasurement>> GetByVehicleIdAsync(Guid vehicleId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(m => m.VehicleId == vehicleId)
            .Include(m => m.Vehicle)
            .OrderByDescending(m => m.Timestamp)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Получить страницу замеров для указанной машины.
    /// </summary>
    /// <param name="vehicleId">Идентификатор машины.</param>
    /// <param name="pageNumber">Номер страницы.</param>
    /// <param name="pageSize">Размер страницы.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
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
    /// <param name="vehicleId">Идентификатор машины.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    public async Task<HumidityMeasurement?> GetLatestByVehicleIdAsync(Guid vehicleId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(m => m.VehicleId == vehicleId)
            .Include(m => m.Vehicle)
            .OrderByDescending(m => m.Timestamp)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Получить все замеры за указанную дату (весь день от 00:00:00 до 23:59:59.9999999 в UTC).
    /// Входная дата приводится к UTC, чтобы корректно сравнивать с Timestamp, хранящимся в UTC.
    /// Используется полуинтервал [начало дня, начало следующего дня) для корректного учёта микросекунд.
    /// </summary>
    /// <param name="date">Дата.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    public async Task<IEnumerable<HumidityMeasurement>> GetByDateAsync(DateTimeOffset date, CancellationToken cancellationToken = default)
    {
        // Приводим дату к UTC и берём начало дня в UTC
        var startOfDayUtc = new DateTimeOffset(date.UtcDateTime.Date, TimeSpan.Zero);
        // Конец интервала – начало следующего дня (исключительно)
        var endOfDayUtc = startOfDayUtc.AddDays(1);

        return await DbSet
            .Where(m => m.Timestamp >= startOfDayUtc && m.Timestamp < endOfDayUtc)
            .Include(m => m.Vehicle)
            .OrderByDescending(m => m.Timestamp)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Получить страницу замеров за указанную дату.
    /// Входная дата приводится к UTC, чтобы корректно сравнивать с Timestamp, хранящимся в UTC.
    /// Используется полуинтервал [начало дня, начало следующего дня) для корректного учёта микросекунд.
    /// </summary>
    /// <param name="date">Дата.</param>
    /// <param name="pageNumber">Номер страницы.</param>
    /// <param name="pageSize">Размер страницы.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    public async Task<PagedResult<HumidityMeasurement>> GetByDatePagedAsync(DateTimeOffset date, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var startOfDayUtc = new DateTimeOffset(date.UtcDateTime.Date, TimeSpan.Zero);
        var endOfDayUtc = startOfDayUtc.AddDays(1);

        IQueryable<HumidityMeasurement> query = DbSet
            .Where(m => m.Timestamp >= startOfDayUtc && m.Timestamp < endOfDayUtc)
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
    /// Предполагается, что from и to уже корректно заданы (например, с учётом UTC).
    /// </summary>
    /// <param name="from">Начало диапазона (включительно).</param>
    /// <param name="to">Конец диапазона (включительно).</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    public async Task<IEnumerable<HumidityMeasurement>> GetByDateRangeAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(m => m.Timestamp >= from && m.Timestamp <= to)
            .Include(m => m.Vehicle)
            .OrderByDescending(m => m.Timestamp)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Переопределение метода GetPagedAsync с добавлением Include для Vehicle и AsNoTracking().
    /// </summary>
    public override async Task<PagedResult<HumidityMeasurement>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        System.Linq.Expressions.Expression<Func<HumidityMeasurement, bool>>? filter = null,
        Func<IQueryable<HumidityMeasurement>, IOrderedQueryable<HumidityMeasurement>>? orderBy = null,
        CancellationToken cancellationToken = default)
    {
        // Защита от невалидных значений
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        IQueryable<HumidityMeasurement> query = DbSet
            .Include(m => m.Vehicle); // Добавляем подгрузку связанной машины

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
            // Сортировка по умолчанию – по Timestamp убыванию (новые первыми)
            query = query.OrderByDescending(m => m.Timestamp);
        }

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking() // Добавлено AsNoTracking для повышения производительности
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
    /// Получить словарь (VehicleId → количество замеров) для переданного списка идентификаторов машин.
    /// Выполняет один запрос к БД с группировкой.
    /// </summary>
    /// <param name="vehicleIds">Список идентификаторов машин.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    public async Task<Dictionary<Guid, int>> GetCountsByVehicleIdsAsync(IEnumerable<Guid> vehicleIds, CancellationToken cancellationToken = default)
    {
        var ids = vehicleIds.Distinct().ToList();
        if (!ids.Any())
            return new Dictionary<Guid, int>();

        var counts = await DbSet
            .Where(m => ids.Contains(m.VehicleId))
            .GroupBy(m => m.VehicleId)
            .Select(g => new { VehicleId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(k => k.VehicleId, v => v.Count, cancellationToken);

        return counts;
    }

    /// <summary>
    /// Получить статистику по замерам для указанной машины.
    /// </summary>
    /// <param name="vehicleId">Идентификатор машины.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    public async Task<MeasurementStatisticsDto> GetStatisticsByVehicleIdAsync(Guid vehicleId, CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(m => m.VehicleId == vehicleId);

        var statistics = new MeasurementStatisticsDto();

        // Общее количество
        statistics.Count = await query.CountAsync(cancellationToken);

        if (statistics.Count > 0)
        {
            // Агрегации влажности
            statistics.Average = await query.AverageAsync(m => m.HumidityValue, cancellationToken);
            statistics.Min = await query.MinAsync(m => m.HumidityValue, cancellationToken);
            statistics.Max = await query.MaxAsync(m => m.HumidityValue, cancellationToken);

            // Последний замер по времени
            var last = await query.OrderByDescending(m => m.Timestamp).FirstOrDefaultAsync(cancellationToken);
            statistics.LastMeasurementTimestamp = last?.Timestamp;

            // Количество по источникам
            statistics.ManualCount = await query.CountAsync(m => m.Source == MeasurementSource.Manual, cancellationToken);
            statistics.AutoCount = await query.CountAsync(m => m.Source == MeasurementSource.Auto, cancellationToken);
        }

        return statistics;
    }

    /// <summary>
    /// Получить страницу замеров в диапазоне дат.
    /// </summary>
    /// <param name="from">Начало диапазона (включительно).</param>
    /// <param name="to">Конец диапазона (включительно).</param>
    /// <param name="pageNumber">Номер страницы.</param>
    /// <param name="pageSize">Размер страницы.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    public async Task<PagedResult<HumidityMeasurement>> GetByDateRangePagedAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        IQueryable<HumidityMeasurement> query = DbSet
            .Where(m => m.Timestamp >= from && m.Timestamp <= to)
            .Include(m => m.Vehicle);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(m => m.Timestamp)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
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
    /// Получить сводку по поставщикам (группировка по ИНН) за период.
    /// Включает все машины, въехавшие в период, даже без замеров.
    /// </summary>
    public async Task<PagedResult<SupplierDto>> GetSuppliersSummaryAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var fromUtc = from.ToUniversalTime();
        var toUtc = to.ToUniversalTime();

        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        // Основной запрос: все машины, въехавшие в период, с левым присоединением замеров за тот же период
        var query = from vehicle in Context.Vehicles
                    join measurement in Context.Measurements
                        on new { VehicleId = vehicle.Id, TimestampRange = true }
                        equals new { VehicleId = measurement.VehicleId, TimestampRange = measurement.Timestamp >= fromUtc && measurement.Timestamp <= toUtc }
                        into measurementsGroup
                    from measurement in measurementsGroup.DefaultIfEmpty()
                    where vehicle.EntryDate >= fromUtc && vehicle.EntryDate <= toUtc
                          && vehicle.Inn != null && vehicle.Inn != string.Empty
                    group new { vehicle, measurement } by vehicle.Inn into g
                    select new
                    {
                        Inn = g.Key,
                        LastCounterparty = g.OrderByDescending(x => x.vehicle.Date)
                                            .Select(x => x.vehicle.Counterparty)
                                            .FirstOrDefault(),
                        VehiclesCount = g.Select(x => x.vehicle.Id).Distinct().Count(),
                        TotalMeasurements = g.Count(x => x.measurement != null),
                        AverageHumidity = g.Where(x => x.measurement != null)
                                           .Average(x => x.measurement!.HumidityValue),
                        MinHumidity = g.Where(x => x.measurement != null)
                                       .Min(x => x.measurement!.HumidityValue),
                        MaxHumidity = g.Where(x => x.measurement != null)
                                       .Max(x => x.measurement!.HumidityValue)
                    };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.TotalMeasurements)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new SupplierDto
            {
                Inn = x.Inn,
                Counterparty = x.LastCounterparty ?? x.Inn,
                VehiclesCount = x.VehiclesCount,
                TotalMeasurements = x.TotalMeasurements,
                AverageHumidity = x.AverageHumidity,
                MinHumidity = x.MinHumidity,
                MaxHumidity = x.MaxHumidity
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<SupplierDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    /// <summary>
    /// Получить детальную информацию по поставщику (ИНН) за период.
    /// Возвращает все машины поставщика, въехавшие в период, и их замеры (если есть).
    /// </summary>
    public async Task<SupplierDetailsDto> GetSupplierDetailsAsync(
        string inn,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default)
    {
        var fromUtc = from.ToUniversalTime();
        var toUtc = to.ToUniversalTime();

        // Получаем все машины поставщика, въехавшие в период, с их замерами (за тот же период)
        var dataQuery = from vehicle in Context.Vehicles
                        join measurement in Context.Measurements
                            on new { VehicleId = vehicle.Id, TimestampRange = true }
                            equals new { VehicleId = measurement.VehicleId, TimestampRange = measurement.Timestamp >= fromUtc && measurement.Timestamp <= toUtc }
                            into measurementsGroup
                        from measurement in measurementsGroup.DefaultIfEmpty()
                        where vehicle.Inn == inn
                              && vehicle.EntryDate >= fromUtc && vehicle.EntryDate <= toUtc
                        select new { Vehicle = vehicle, Measurement = measurement };

        var list = await dataQuery.ToListAsync(cancellationToken);
        if (!list.Any())
        {
            return new SupplierDetailsDto
            {
                Inn = inn,
                Counterparty = inn,
                Vehicles = new List<SupplierVehicleSummaryDto>(),
                OverallStatistics = new MeasurementStatisticsDto()
            };
        }

        // Определяем актуальное название поставщика (последнее по дате пропуска)
        var latestVehicle = list.OrderByDescending(x => x.Vehicle.Date).FirstOrDefault()?.Vehicle;
        var counterparty = latestVehicle?.Counterparty ?? inn;

        // Группируем по машинам
        var vehicleGroups = list.GroupBy(x => x.Vehicle.Id);

        var vehicleSummaries = new List<SupplierVehicleSummaryDto>();
        int totalMeasurements = 0;
        double totalHumiditySum = 0;
        double? globalMin = null;
        double? globalMax = null;
        int autoCount = 0, manualCount = 0;

        foreach (var group in vehicleGroups)
        {
            var vehicle = group.First().Vehicle;
            var measurements = group.Where(x => x.Measurement != null)
                                    .Select(x => x.Measurement!)
                                    .ToList();
            var count = measurements.Count;
            var avg = count > 0 ? measurements.Average(m => m.HumidityValue) : (double?)null;
            var min = count > 0 ? measurements.Min(m => m.HumidityValue) : (double?)null;
            var max = count > 0 ? measurements.Max(m => m.HumidityValue) : (double?)null;
            var auto = measurements.Count(m => m.Source == MeasurementSource.Auto);
            var manual = measurements.Count(m => m.Source == MeasurementSource.Manual);
            var last = measurements.OrderByDescending(m => m.Timestamp).FirstOrDefault()?.Timestamp;

            vehicleSummaries.Add(new SupplierVehicleSummaryDto
            {
                VehicleId = vehicle.Id,
                Number = vehicle.Number,
                VehiclePlate = vehicle.VehiclePlate,
                EntryDate = vehicle.EntryDate,
                ExitDate = vehicle.ExitDate,
                MeasurementsCount = count,
                AverageHumidity = avg,
                MinHumidity = min,
                MaxHumidity = max,
                AutoCount = auto,
                ManualCount = manual,
                LastMeasurementTimestamp = last
            });

            totalMeasurements += count;
            if (count > 0)
            {
                totalHumiditySum += measurements.Sum(m => m.HumidityValue);
                if (globalMin == null || min < globalMin) globalMin = min;
                if (globalMax == null || max > globalMax) globalMax = max;
            }
            autoCount += auto;
            manualCount += manual;
        }

        var overallStats = new MeasurementStatisticsDto
        {
            Count = totalMeasurements,
            Average = totalMeasurements > 0 ? totalHumiditySum / totalMeasurements : null,
            Min = globalMin,
            Max = globalMax,
            ManualCount = manualCount,
            AutoCount = autoCount,
            LastMeasurementTimestamp = list.Where(x => x.Measurement != null)
                                           .OrderByDescending(x => x.Measurement!.Timestamp)
                                           .FirstOrDefault()?.Measurement?.Timestamp
        };

        return new SupplierDetailsDto
        {
            Inn = inn,
            Counterparty = counterparty,
            Vehicles = vehicleSummaries.OrderByDescending(v => v.MeasurementsCount).ToList(),
            OverallStatistics = overallStats
        };
    }
}