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
    /// Получить замеры в произвольном диапазоне дат (фильтр по Timestamp замера).
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
    /// Получить страницу замеров в диапазоне дат (фильтр по Timestamp замера).
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
        if (pageSize > 20000) pageSize = 20000; // Максимальный лимит увеличен для поддержки отчётов за смену/период

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
    /// Получить страницу замеров для машин, у которых ВРЕМЯ ВЫЕЗДА (Vehicle.ExitDate)
    /// попадает в указанный диапазон.
    ///
    /// КЛЮЧЕВАЯ ИДЕЯ: все замеры машины относятся к той смене, в которую машина выехала с площадки.
    /// Это устраняет ситуацию, когда одна машина оставляет замеры в разных сменах
    /// (например, начала мерить в дневную, а закончила в ночную).
    ///
    /// Сортировка выполняется по Vehicle.ExitDate (по умолчанию — по убыванию: новые сверху).
    /// При равенстве ExitDate — по Timestamp замера (тоже по убыванию).
    /// Машины без даты выезда в выборку не попадают (они всё ещё на площадке).
    /// </summary>
    /// <param name="from">Начало диапазона (включительно) для времени выезда машины.</param>
    /// <param name="to">Конец диапазона (включительно) для времени выезда машины.</param>
    /// <param name="pageNumber">Номер страницы (начиная с 1).</param>
    /// <param name="pageSize">Размер страницы.</param>
    /// <param name="sortDescending">true — сортировка по ExitDate по убыванию (новые сверху), false — по возрастанию.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    public async Task<PagedResult<HumidityMeasurement>> GetByVehicleExitDateRangePagedAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        int pageNumber,
        int pageSize,
        bool sortDescending,
        CancellationToken cancellationToken = default)
    {
        // Защита от невалидных значений
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 20000) pageSize = 20000;

        // Фильтруем по времени выезда машины.
        // ExitDate != null — машина уже выехала (смена завершена для неё).
        IQueryable<HumidityMeasurement> query = DbSet
            .Where(m => m.Vehicle.ExitDate != null
                        && m.Vehicle.ExitDate >= from
                        && m.Vehicle.ExitDate <= to)
            .Include(m => m.Vehicle);

        var totalCount = await query.CountAsync(cancellationToken);

        // Сортировка: по времени выезда машины (главный критерий), затем по времени замера.
        // Это гарантирует, что все замеры одной машины идут подряд и упорядочены по времени.
        if (sortDescending)
        {
            query = query
                .OrderByDescending(m => m.Vehicle.ExitDate)
                .ThenByDescending(m => m.Timestamp);
        }
        else
        {
            query = query
                .OrderBy(m => m.Vehicle.ExitDate)
                .ThenBy(m => m.Timestamp);
        }

        var items = await query
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
    /// Получить агрегированный отчёт за период с сортировкой и пагинацией на стороне сервера.
    ///
    /// Основной SQL-запрос делает INNER JOIN Measurements + Vehicles, GROUP BY vehicle.Id,
    /// считает все агрегаты и возвращает одну страницу после сортировки.
    /// Общая статистика (Summary) считается отдельным запросом по полному набору данных,
    /// чтобы не зависеть от пагинации.
    ///
    /// Такой подход позволяет безопасно запрашивать отчёты за длительные периоды (год и больше):
    /// на клиент уезжает только одна страница агрегатов, а не все замеры.
    /// </summary>
    public async Task<PeriodReportResponseDto> GetPeriodReportAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        string sortBy,
        bool sortDescending,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        // Приводим границы периода к UTC (на случай, если клиент прислал локальное время со смещением).
        var fromUtc = from.ToUniversalTime();
        var toUtc = to.ToUniversalTime();

        // Нормализация параметров пагинации.
        // Максимум 500 записей на страницу — этого достаточно для отображения,
        // при этом предотвращает случайные запросы «выгрузить всё».
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 100;
        if (pageSize > 500) pageSize = 500;

        // Основной запрос: INNER JOIN машин и замеров, фильтр по Timestamp замера,
        // группировка по машине. Машины без замеров за период в отчёт не попадают —
        // это согласуется с прежней клиентской логикой (группировка по vehicleId из замеров).
        var groupedQuery =
            from measurement in Context.Measurements
            join vehicle in Context.Vehicles on measurement.VehicleId equals vehicle.Id
            where measurement.Timestamp >= fromUtc && measurement.Timestamp <= toUtc
            group new { measurement, vehicle } by vehicle.Id into g
            select new
            {
                VehicleId = g.Key,
                Number = g.Select(x => x.vehicle.Number).FirstOrDefault() ?? string.Empty,
                VehiclePlate = g.Select(x => x.vehicle.VehiclePlate).FirstOrDefault() ?? string.Empty,
                Counterparty = g.Select(x => x.vehicle.Counterparty).FirstOrDefault() ?? string.Empty,
                EntryDate = (DateTimeOffset?)g.Select(x => x.vehicle.EntryDate).FirstOrDefault(),
                ExitDate = (DateTimeOffset?)g.Select(x => x.vehicle.ExitDate).FirstOrDefault(),
                MeasurementsCount = g.Count(),
                AverageHumidity = (double?)g.Average(x => x.measurement.HumidityValue),
                MinHumidity = (double?)g.Min(x => x.measurement.HumidityValue),
                MaxHumidity = (double?)g.Max(x => x.measurement.HumidityValue),
                AutoCount = g.Count(x => x.measurement.Source == MeasurementSource.Auto),
                ManualCount = g.Count(x => x.measurement.Source == MeasurementSource.Manual),
                LastMeasurementTimestamp = (DateTimeOffset?)g.Max(x => x.measurement.Timestamp)
            };

        // Применяем сортировку в SQL.
        // Используем стабильные tie-breakers (VehicleId) для детерминированного порядка —
        // это критично для корректной пагинации, иначе одни и те же строки могут «прыгать» между страницами.
        var normalizedSortBy = (sortBy ?? string.Empty).Trim().ToLowerInvariant();

        IOrderedQueryable<dynamic> orderedQuery;

        if (normalizedSortBy == "averagehumidity")
        {
            orderedQuery = sortDescending
                ? groupedQuery.OrderByDescending(x => x.AverageHumidity).ThenBy(x => x.VehicleId)
                : groupedQuery.OrderBy(x => x.AverageHumidity).ThenBy(x => x.VehicleId);
        }
        else if (normalizedSortBy == "lastmeasurement" || normalizedSortBy == "lastmeasurementtimestamp")
        {
            orderedQuery = sortDescending
                ? groupedQuery.OrderByDescending(x => x.LastMeasurementTimestamp).ThenBy(x => x.VehicleId)
                : groupedQuery.OrderBy(x => x.LastMeasurementTimestamp).ThenBy(x => x.VehicleId);
        }
        else
        {
            // По умолчанию — сортировка по дате выезда машины.
            orderedQuery = sortDescending
                ? groupedQuery.OrderByDescending(x => x.ExitDate).ThenBy(x => x.VehicleId)
                : groupedQuery.OrderBy(x => x.ExitDate).ThenBy(x => x.VehicleId);
        }

        // Считаем общее количество машин за период (без пагинации).
        var totalCount = await groupedQuery.CountAsync(cancellationToken);

        // Забираем одну страницу из SQL.
        var pageItems = await orderedQuery
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // Маппим «сырые» анонимные объекты в DTO.
        var items = pageItems
            .Select(x => new PeriodReportItemDto
            {
                VehicleId = x.VehicleId,
                Number = x.Number,
                VehiclePlate = x.VehiclePlate,
                Counterparty = x.Counterparty,
                EntryDate = x.EntryDate,
                ExitDate = x.ExitDate,
                MeasurementsCount = x.MeasurementsCount,
                AverageHumidity = x.AverageHumidity,
                MinHumidity = x.MinHumidity,
                MaxHumidity = x.MaxHumidity,
                AutoCount = x.AutoCount,
                ManualCount = x.ManualCount,
                LastMeasurementTimestamp = x.LastMeasurementTimestamp
            })
            .ToList();

        // Общая статистика по всем машинам за период.
        // Считаем отдельным запросом по полному набору агрегатов (без пагинации).
        // Средняя влажность — взвешенная по количеству замеров (учитывает, что у разных машин разное число замеров).
        var summary = await groupedQuery
            .GroupBy(_ => 1)
            .Select(g => new PeriodReportSummaryDto
            {
                VehicleCount = g.Count(),
                TotalMeasurements = g.Sum(x => x.MeasurementsCount),
                // Взвешенная средняя: сумма (avg * count) / сумма count.
                OverallAverageHumidity = g.Sum(x => x.AverageHumidity * x.MeasurementsCount) / g.Sum(x => x.MeasurementsCount),
                // Глобальный минимум — минимум по всем машинам, глобальный максимум — максимум по всем машинам.
                OverallMinHumidity = g.Min(x => x.MinHumidity),
                OverallMaxHumidity = g.Max(x => x.MaxHumidity),
                TotalAutoCount = g.Sum(x => x.AutoCount),
                TotalManualCount = g.Sum(x => x.ManualCount)
            })
            .FirstOrDefaultAsync(cancellationToken);

        return new PeriodReportResponseDto
        {
            Vehicles = new PagedResult<PeriodReportItemDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            },
            Summary = summary ?? new PeriodReportSummaryDto()
        };
    }

    /// <summary>
    /// Получить сводку по поставщикам (группировка по ИНН) за период с пагинацией и поиском.
    ///
    /// ПОИСК: параметр search фильтрует «сырые» машины до группировки по ИНН:
    ///   - ИНН машины содержит подстроку search (регистронезависимо);
    ///   - ИЛИ наименование поставщика (Counterparty) машины содержит подстроку search.
    /// Поставщик попадает в выборку, если хотя бы одна его машина за период совпала.
    /// Пустая строка / null — поиск не применяется.
    /// </summary>
    public async Task<PagedResult<SupplierDto>> GetSuppliersSummaryAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        int pageNumber,
        int pageSize,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var fromUtc = from.ToUniversalTime();
        var toUtc = to.ToUniversalTime();

        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        // Нормализуем поисковую строку: обрезаем пробелы и превращаем пустую в null,
        // чтобы в SQL не подставлять бессмысленный ILIKE '%%' (это лишняя нагрузка).
        var searchPattern = string.IsNullOrWhiteSpace(search)
            ? null
            : $"%{search.Trim()}%";

        // Основной запрос: все машины, въехавшие в период, с левым присоединением замеров за тот же период.
        // Если search задан — фильтруем машины по частичному совпадению ИНН или наименования.
        var query = from vehicle in Context.Vehicles
                    join measurement in Context.Measurements
                    on new { VehicleId = vehicle.Id, TimestampRange = true }
                    equals new { VehicleId = measurement.VehicleId, TimestampRange = measurement.Timestamp >= fromUtc && measurement.Timestamp <= toUtc }
                    into measurementsGroup
                    from measurement in measurementsGroup.DefaultIfEmpty()
                    where vehicle.EntryDate >= fromUtc && vehicle.EntryDate <= toUtc
                          && vehicle.Inn != null && vehicle.Inn != string.Empty
                          // Поиск по ИНН или наименованию поставщика (регистронезависимо).
                          // EF.Functions.ILike использует PostgreSQL-оператор ILIKE и не чувствителен к регистру.
                          && (searchPattern == null
                              || EF.Functions.ILike(vehicle.Inn, searchPattern)
                              || EF.Functions.ILike(vehicle.Counterparty, searchPattern))
                    group new { vehicle, measurement } by vehicle.Inn into g
                    select new
                    {
                        Inn = g.Key,
                        LastCounterparty = g.OrderByDescending(x => x.vehicle.Date)
                                            .Select(x => x.vehicle.Counterparty)
                                            .FirstOrDefault(),
                        // Общее количество машин
                        VehiclesCount = g.Select(x => x.vehicle.Id).Distinct().Count(),
                        // Количество машин с замерами
                        MeasuredVehiclesCount = g.Where(x => x.measurement != null)
                                                 .Select(x => x.vehicle.Id)
                                                 .Distinct()
                                                 .Count(),
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
                MeasuredVehiclesCount = x.MeasuredVehiclesCount,
                TotalMeasurements = x.TotalMeasurements,
                AverageHumidity = x.AverageHumidity,
                // Для обычного списка поставщиков байесовская коррекция не применяется —
                // поля AdjustedAverageHumidity / PriorWeight / GlobalAverageHumidity остаются по умолчанию.
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
    /// Вспомогательный метод: загружает все машины поставщика за период с агрегированными данными
    /// и вычисляет общую статистику. Используется как в постраничном методе, так и в методе для графика.
    /// </summary>
    /// <param name="inn">ИНН поставщика.</param>
    /// <param name="fromUtc">Начало периода в UTC (включительно).</param>
    /// <param name="toUtc">Конец периода в UTC (включительно).</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>
    /// Кортеж: полный список сводок по машинам (без сортировки и пагинации),
    /// общая статистика и актуальное наименование поставщика.
    /// </returns>
    private async Task<(List<SupplierVehicleSummaryDto> AllVehicles, MeasurementStatisticsDto OverallStats, string Counterparty)>
        LoadSupplierDataAsync(string inn, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken)
    {
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

        var list = await dataQuery
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        if (!list.Any())
        {
            return (new List<SupplierVehicleSummaryDto>(), new MeasurementStatisticsDto(), inn);
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
        DateTimeOffset? lastTimestamp = null;

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
                if (lastTimestamp == null || last > lastTimestamp) lastTimestamp = last;
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
            LastMeasurementTimestamp = lastTimestamp
        };

        return (vehicleSummaries, overallStats, counterparty);
    }

    /// <summary>
    /// Получить детальную информацию по поставщику (ИНН) за период с постраничной выборкой машин.
    /// Сортировка и пагинация выполняются на стороне сервера.
    /// </summary>
    public async Task<SupplierDetailsDto> GetSupplierDetailsAsync(
        string inn,
        DateTimeOffset from,
        DateTimeOffset to,
        int pageNumber,
        int pageSize,
        bool sortDescending,
        CancellationToken cancellationToken = default)
    {
        var fromUtc = from.ToUniversalTime();
        var toUtc = to.ToUniversalTime();

        // Нормализация параметров пагинации
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var (allVehicles, overallStats, counterparty) = await LoadSupplierDataAsync(
            inn, fromUtc, toUtc, cancellationToken);

        if (allVehicles.Count == 0)
        {
            return new SupplierDetailsDto
            {
                Inn = inn,
                Counterparty = inn,
                Vehicles = new PagedResult<SupplierVehicleSummaryDto>
                {
                    Items = new List<SupplierVehicleSummaryDto>(),
                    TotalCount = 0,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = 0
                },
                OverallStatistics = new MeasurementStatisticsDto()
            };
        }

        // Сортировка по дате въезда на сервере
        var sortedVehicles = sortDescending
            ? allVehicles.OrderByDescending(v => v.EntryDate).ToList()
            : allVehicles.OrderBy(v => v.EntryDate).ToList();

        // Пагинация на сервере
        var totalCount = sortedVehicles.Count;
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var pagedItems = sortedVehicles
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new SupplierDetailsDto
        {
            Inn = inn,
            Counterparty = counterparty,
            Vehicles = new PagedResult<SupplierVehicleSummaryDto>
            {
                Items = pagedItems,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = totalPages
            },
            OverallStatistics = overallStats
        };
    }

    /// <summary>
    /// Получить полный (без пагинации) список машин поставщика за период с агрегированными данными,
    /// отсортированный по дате въезда. Используется для построения графика.
    /// Сортировка выполняется на стороне сервера.
    /// </summary>
    public async Task<IEnumerable<SupplierVehicleSummaryDto>> GetSupplierVehiclesForChartAsync(
        string inn,
        DateTimeOffset from,
        DateTimeOffset to,
        bool sortDescending,
        CancellationToken cancellationToken = default)
    {
        var fromUtc = from.ToUniversalTime();
        var toUtc = to.ToUniversalTime();

        var (allVehicles, _, _) = await LoadSupplierDataAsync(inn, fromUtc, toUtc, cancellationToken);

        if (allVehicles.Count == 0)
        {
            return Enumerable.Empty<SupplierVehicleSummaryDto>();
        }

        // Сортировка по дате въезда на сервере (без пагинации)
        return sortDescending
            ? allVehicles.OrderByDescending(v => v.EntryDate).ToList()
            : allVehicles.OrderBy(v => v.EntryDate).ToList();
    }

    /// <summary>
    /// Получить топ-N поставщиков по средней влажности за период с байесовской коррекцией.
    ///
    /// АЛГОРИТМ:
    /// 1. Считаем глобальную среднюю влажность m по всем замерам за период (prior mean).
    /// 2. Считаем агрегаты по каждому поставщику: sum_i, n_i, min, max, кол-во машин и т.д.
    /// 3. Применяем байесовское сглаживание:
    ///        adjusted_i = (C * m + sum_i) / (C + n_i)
    ///    где C — вес prior (priorWeight).
    /// 4. Сортируем по adjusted_i и берём top-N.
    /// </summary>
    public async Task<IEnumerable<SupplierDto>> GetTopSuppliersAsync(
        int top,
        bool ascending,
        DateTimeOffset from,
        DateTimeOffset to,
        double priorWeight = 30,
        CancellationToken cancellationToken = default)
    {
        var fromUtc = from.ToUniversalTime();
        var toUtc = to.ToUniversalTime();

        // Защита от некорректных значений.
        if (top < 1) top = 1;
        if (top > 100) top = 100;
        if (priorWeight < 0) priorWeight = 0;
        // Верхняя граница выбрана на уровне 1000 — этого достаточно для любого разумного сценария.
        // Дальнейшее увеличение C практически не меняет результат (все средние стремятся к глобальной).
        if (priorWeight > 1000) priorWeight = 1000;

        // ШАГ 1: глобальная средняя влажность по всем замерам за период (prior mean m).
        // Если замеров нет — возвращаем пустой список.
        var globalStats = await Context.Measurements
            .Where(m => m.Timestamp >= fromUtc && m.Timestamp <= toUtc)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                AverageHumidity = g.Average(m => m.HumidityValue),
                TotalCount = g.Count()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (globalStats == null || globalStats.TotalCount == 0)
        {
            // Нет замеров за период — топ пуст.
            return Enumerable.Empty<SupplierDto>();
        }

        var globalAverage = globalStats.AverageHumidity;

        // ШАГ 2: агрегаты по каждому поставщику.
        // Используем тот же паттерн, что и в GetSuppliersSummaryAsync,
        // но дополнительно считаем сумму влажностей (SumHumidity), чтобы применить формулу.
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
                        MeasuredVehiclesCount = g.Where(x => x.measurement != null)
                                                 .Select(x => x.vehicle.Id)
                                                 .Distinct()
                                                 .Count(),
                        TotalMeasurements = g.Count(x => x.measurement != null),
                        // Сумма влажностей по всем замерам поставщика за период.
                        // Используем Sum с приведением к nullable, чтобы EF Core корректно обработал пустой набор.
                        SumHumidity = g.Where(x => x.measurement != null)
                                       .Sum(x => (double?)x.measurement!.HumidityValue) ?? 0.0,
                        MinHumidity = g.Where(x => x.measurement != null)
                                       .Min(x => (double?)x.measurement!.HumidityValue),
                        MaxHumidity = g.Where(x => x.measurement != null)
                                       .Max(x => (double?)x.measurement!.HumidityValue)
                    };

        // Исключаем поставщиков, у которых нет замеров за период:
        // байесовская коррекция для них не имеет смысла (n_i = 0).
        query = query.Where(x => x.TotalMeasurements > 0);

        // Забираем «сырые» агрегаты.
        var rawList = await query
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        if (rawList.Count == 0)
        {
            return Enumerable.Empty<SupplierDto>();
        }

        // ШАГ 3: применяем байесовскую коррекцию в памяти.
        // (Делаем это на клиенте, а не в SQL, потому что EF Core хуже транслирует
        //  сложные формулы с делением и подстановкой глобальной средней.)
        var items = rawList.Select(x =>
        {
            var n = x.TotalMeasurements;
            var sum = x.SumHumidity;

            // Наивная средняя (sum / n) — «сырая» средняя без коррекции.
            var naiveAvg = n > 0 ? sum / n : 0.0;

            // Байесовская средняя: (C * m + sum) / (C + n).
            // При priorWeight = 0 формула превращается в наивную среднюю.
            var adjusted = (priorWeight * globalAverage + sum) / (priorWeight + n);

            return new SupplierDto
            {
                Inn = x.Inn,
                Counterparty = x.LastCounterparty ?? x.Inn,
                VehiclesCount = x.VehiclesCount,
                MeasuredVehiclesCount = x.MeasuredVehiclesCount,
                TotalMeasurements = n,
                AverageHumidity = naiveAvg,
                AdjustedAverageHumidity = adjusted,
                PriorWeight = priorWeight,
                GlobalAverageHumidity = globalAverage,
                MinHumidity = x.MinHumidity,
                MaxHumidity = x.MaxHumidity
            };
        });

        // ШАГ 4: сортировка по скорректированной средней + стабильный tie-breaker.
        // Вторичный критерий — количество замеров (по убыванию): при равной средней
        // впереди оказывается поставщик с более надёжными данными.
        var sorted = ascending
            ? items.OrderBy(x => x.AdjustedAverageHumidity).ThenByDescending(x => x.TotalMeasurements)
            : items.OrderByDescending(x => x.AdjustedAverageHumidity).ThenByDescending(x => x.TotalMeasurements);

        return sorted.Take(top).ToList();
    }
}