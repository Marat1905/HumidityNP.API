using Humidity.Domain.Common;
using Humidity.Domain.Entities;
using Humidity.Domain.Enums;
using Humidity.Domain.Interfaces;
using Humidity.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Data;

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
    /// <summary>
    /// SQL-запрос для получения сессий по штабелям.
    ///
    /// Логика запроса:
    ///  1. normalized — нормализуем данные: получаем ключ поставщика (ИНН или имя),
    ///     очищенное имя, время замера, влажность, нормализованный номер штабеля.
    ///     Тут же фильтруем по периоду [@From, @To].
    ///  2. with_gap — считаем разрыв между соседними замерами внутри одного штабеля.
    ///  3. base_marked — помечаем начало новой базовой сессии, если разрыв >= 24 часов.
    ///  4. base_session — присваиваем каждой строке id базовой сессии (сумма флагов).
    ///  5. base_summary — агрегируем базовые сессии: начало, конец, замеры, машины.
    ///  6. with_prev — считаем разрыв между текущей и предыдущей базовой сессией.
    ///  7. to_merge — решаем, нужно ли склеить сессию с предыдущей:
    ///     разрыв < 72 часов И (мало замеров ИЛИ мало машин).
    ///  8. renumber_map — пересчитываем id сессии с учётом склеек.
    ///  9. suppliers_raw/with_len/canon/ranked/agg — строим список поставщиков сессии:
    ///     группируем по ключу, выбираем каноническое (самое короткое) имя,
    ///     считаем машины, выбираем основного поставщика (rn=1).
    /// 10. Финальный SELECT — агрегируем сессии и джойним поставщиков.
    ///
    /// Параметры: @From, @To — границы периода.
    ///
    /// Сортировка результата:
    ///  - сначала штабели: по числовому префиксу ВОЗРАСТАНИЕ (1, 2, 3, ..., 10, 11, ...),
    ///    NULLS LAST — штабели без цифр уходят в конец;
    ///  - при равенстве числового префикса — по полному имени штабеля ПО ВОЗРАСТАНИЮ
    ///    (это даёт «1» перед «1A», «7» перед «7A»);
    ///  - внутри штабеля — сессии по УБЫВАНИЮ номера (session_id DESC):
    ///    САМАЯ СВЕЖАЯ СЕССИЯ СВЕРХУ. Номер сессии сквозной по времени:
    ///    чем больше номер, тем позже сессия началась. Поэтому DESC = новые сверху.
    ///
    /// ВАЖНО про типы:
    ///  - COUNT(*) и SUM(...) OVER (...) в PostgreSQL возвращают bigint (int8).
    ///    Npgsql 6+ не приводит типы молча: reader.GetInt32() на bigint бросает
    ///    InvalidCastException. Поэтому в финальном SELECT все «счётные» колонки
    ///    явно приводятся к int через ::int. Это самый чистый способ —
    ///    SQL сам объявляет ожидаемый тип, ридер читает без сюрпризов.
    ///  - AVG(...)::numeric в Postgres — это numeric (в Npgsql мапится в decimal).
    ///    GetDouble() на numeric работает не везде, поэтому итоговое значение
    ///    явно приводится к float8: ROUND(AVG(...)::numeric, 2)::float8.
    /// </summary>
    private const string StackSessionsSql = @"
WITH params AS (
    SELECT
        INTERVAL '24 hours' AS base_gap,
        INTERVAL '72 hours' AS merge_gap,
        20                  AS min_meas,
        3                   AS min_veh
),
normalized AS (
    SELECT
        v.""Id""                                          AS vehicle_id,
        -- Ключ группировки поставщика: ИНН, если есть, иначе нормализованное имя
        COALESCE(
            NULLIF(TRIM(v.""Inn""), ''),
            'NAME:' || UPPER(REGEXP_REPLACE(TRIM(v.""Counterparty""), '\s+', ' ', 'g'))
        )                                               AS cp_key,
        -- Очищенное имя (TRIM + схлопывание пробелов)
        REGEXP_REPLACE(TRIM(v.""Counterparty""), '\s+', ' ', 'g') AS counterparty_clean,
        m.""Timestamp""                                   AS ts,
        m.""HumidityValue""                               AS humidity,
        UPPER(TRANSLATE(v.""StackNumber"", 'аА', 'aA'))   AS stack_number
    FROM ""Vehicles""     v
    JOIN ""Measurements"" m ON m.""VehicleId"" = v.""Id""
    WHERE v.""StackNumber"" IS NOT NULL
      AND TRIM(v.""StackNumber"") <> ''
      AND m.""Timestamp"" >= @From
      AND m.""Timestamp"" <= @To
),
with_gap AS (
    SELECT n.*,
           n.ts - LAG(n.ts) OVER (
                      PARTITION BY n.stack_number ORDER BY n.ts
                  ) AS gap
    FROM normalized n
),
base_marked AS (
    SELECT w.*,
           CASE
               WHEN w.gap IS NULL OR w.gap >= (SELECT base_gap FROM params)
               THEN 1 ELSE 0
           END AS is_break
    FROM with_gap w
),
base_session AS (
    SELECT b.*,
           SUM(b.is_break) OVER (
               PARTITION BY b.stack_number ORDER BY b.ts
           ) AS base_session_id
    FROM base_marked b
),
base_summary AS (
    SELECT stack_number,
           base_session_id,
           MIN(ts)                    AS start_ts,
           MAX(ts)                    AS end_ts,
           COUNT(*)                   AS meas_cnt,
           COUNT(DISTINCT vehicle_id) AS veh_cnt
    FROM base_session
    GROUP BY stack_number, base_session_id
),
with_prev AS (
    SELECT bs.*,
           bs.start_ts - LAG(bs.end_ts) OVER (
                             PARTITION BY bs.stack_number
                             ORDER BY bs.start_ts
                         ) AS gap_from_prev
    FROM base_summary bs
),
to_merge AS (
    SELECT wp.*,
           CASE
               WHEN wp.gap_from_prev IS NOT NULL
                AND wp.gap_from_prev < (SELECT merge_gap FROM params)
                AND (wp.meas_cnt < (SELECT min_meas FROM params)
                     OR wp.veh_cnt < (SELECT min_veh  FROM params))
               THEN 1 ELSE 0
           END AS merge_flag
    FROM with_prev wp
),
renumber_map AS (
    SELECT tm.stack_number,
           tm.base_session_id,
           tm.base_session_id - COALESCE(
               SUM(tm.merge_flag) OVER (
                   PARTITION BY tm.stack_number
                   ORDER BY tm.base_session_id
               ), 0
           ) AS final_session_id
    FROM to_merge tm
),
-- (A) Уникальные поставщики по ключу cp_key
suppliers_raw AS (
    SELECT
        bs.stack_number,
        rm.final_session_id,
        bs.cp_key,
        bs.counterparty_clean,
        bs.vehicle_id
    FROM base_session bs
    JOIN renumber_map rm
      ON rm.stack_number    = bs.stack_number
     AND rm.base_session_id = bs.base_session_id
    WHERE bs.cp_key IS NOT NULL
      AND bs.cp_key <> ''
      AND bs.counterparty_clean IS NOT NULL
      AND bs.counterparty_clean <> ''
),
suppliers_with_len AS (
    SELECT sr.*,
           LENGTH(sr.counterparty_clean) AS name_len,
           MIN(LENGTH(sr.counterparty_clean)) OVER (
               PARTITION BY sr.stack_number, sr.final_session_id, sr.cp_key
           ) AS min_len
    FROM suppliers_raw sr
),
-- Каноническое имя = самое короткое из группы (обычно «Тандер», а не «Тандер(Новосибирск)»)
suppliers_canon AS (
    SELECT
        stack_number,
        final_session_id,
        cp_key,
        MIN(counterparty_clean)     AS counterparty_canonical,
        COUNT(DISTINCT vehicle_id)  AS vehicles_cnt
    FROM suppliers_with_len
    WHERE name_len = min_len
    GROUP BY stack_number, final_session_id, cp_key
),
suppliers_ranked AS (
    SELECT s.*,
           ROW_NUMBER() OVER (
               PARTITION BY stack_number, final_session_id
               ORDER BY vehicles_cnt DESC, counterparty_canonical
           ) AS rn
    FROM suppliers_canon s
),
-- (C) Основной поставщик + список, отсортированный по числу машин
suppliers_agg AS (
    SELECT
        stack_number,
        final_session_id,
        COUNT(*)                                                       AS counterparty_count,
        STRING_AGG(counterparty_canonical, '; '
                   ORDER BY vehicles_cnt DESC, counterparty_canonical) AS counterparties,
        MAX(counterparty_canonical) FILTER (WHERE rn = 1)              AS primary_counterparty
    FROM suppliers_ranked
    GROUP BY stack_number, final_session_id
)
SELECT
    t.stack_number                                                          AS ""StackNumber"",
    -- SessionId: final_session_id имеет тип bigint (base_session_id - SUM(...) OVER ...).
    -- Явно приводим к int, чтобы reader.GetInt32() работал без InvalidCastException.
    t.session_id::int                                                       AS ""SessionId"",
    t.session_start                                                         AS ""SessionStart"",
    t.session_end                                                           AS ""SessionEnd"",
    -- (E) Единый формат длительности: чч:мм:сс, где чч может быть > 24
    LPAD((EXTRACT(DAY    FROM t.dur) * 24
        + EXTRACT(HOUR   FROM t.dur))::int::text, 2, '0')
      || ':' || LPAD(EXTRACT(MINUTE FROM t.dur)::int::text, 2, '0')
      || ':' || LPAD(EXTRACT(SECOND FROM t.dur)::int::text, 2, '0')         AS ""Duration"",
    -- COUNT(*) → bigint; приводим к int для reader.GetInt32().
    t.measurements_count::int                                               AS ""MeasurementsCount"",
    -- COUNT(DISTINCT ...) → bigint; приводим к int.
    t.vehicles_count::int                                                   AS ""VehiclesCount"",
    -- COUNT(*) + COALESCE(..., 0) → bigint; приводим к int.
    t.counterparty_count::int                                               AS ""CounterpartyCount"",
    t.primary_counterparty                                                  AS ""PrimaryCounterparty"",
    t.counterparties                                                        AS ""Counterparties"",
    t.avg_humidity                                                          AS ""AverageHumidity"",
    t.min_humidity                                                          AS ""MinHumidity"",
    t.max_humidity                                                          AS ""MaxHumidity""
FROM (
    SELECT
        bs.stack_number,
        rm.final_session_id                                          AS session_id,
        MIN(date_trunc('second', bs.ts AT TIME ZONE 'Asia/Yekaterinburg')) AS session_start,
        MAX(date_trunc('second', bs.ts AT TIME ZONE 'Asia/Yekaterinburg')) AS session_end,
        date_trunc('second', MAX(bs.ts) - MIN(bs.ts))                AS dur,
        COUNT(*)                                                     AS measurements_count,
        COUNT(DISTINCT bs.vehicle_id)                                AS vehicles_count,
        COALESCE(sa.counterparty_count, 0)                           AS counterparty_count,
        COALESCE(sa.primary_counterparty, '')                        AS primary_counterparty,
        COALESCE(sa.counterparties, '')                              AS counterparties,
        -- ROUND(...::numeric, 2) даёт numeric; приводим итог к float8,
        -- чтобы Npgsql отдал его именно как double precision,
        -- а не как decimal (иначе reader.GetDouble() может упасть).
        ROUND(AVG(bs.humidity)::numeric, 2)::float8                  AS avg_humidity,
        MIN(bs.humidity)                                             AS min_humidity,
        MAX(bs.humidity)                                             AS max_humidity
    FROM base_session bs
    JOIN renumber_map rm
      ON rm.stack_number    = bs.stack_number
     AND rm.base_session_id = bs.base_session_id
    LEFT JOIN suppliers_agg sa
           ON sa.stack_number     = bs.stack_number
          AND sa.final_session_id = rm.final_session_id
    GROUP BY bs.stack_number,
             rm.final_session_id,
             sa.counterparty_count,
             sa.primary_counterparty,
             sa.counterparties
) t
ORDER BY
    -- Числовой префикс штабеля по возрастанию (1, 2, ..., 10, 11, ...).
    -- NULLS LAST — штабели без цифр уходят в конец.
    NULLIF(regexp_replace(t.stack_number, '\D', '', 'g'), '')::int NULLS LAST,
    -- Тай-брейк по полному имени: даёт «1» перед «1A», «7» перед «7A».
    t.stack_number,
    -- Сессии внутри штабеля — по УБЫВАНИЮ номера.
    -- Номер сессии сквозной по времени: чем больше номер, тем позже сессия
    -- началась. Значит DESC = новые сессии сверху, старые снизу.
    t.session_id DESC;
";

    /// <summary>
    /// Получить список сессий работы со штабелями за указанный период.
    ///
    /// Используем ADO.NET напрямую (а не EF Core SqlQuery), потому что запрос:
    ///   - содержит CTE (WITH) и оконные функции;
    ///   - использует PostgreSQL-специфичные функции (TRANSLATE, REGEXP_REPLACE,
    ///     STRING_AGG, date_trunc, AT TIME ZONE);
    ///   - возвращает колонки с русскими именами в исходнике — здесь мы уже
    ///     переименовали их в английские алиасы для маппинга.
    ///
    /// Ручное чтение через NpgsqlDataReader даёт полный контроль над SQL
    /// и не зависит от того, как EF Core компонует запросы.
    ///
    /// Производительность:
    ///  - GetOrdinal() делается ОДИН раз на колонку (а не на каждую строку):
    ///    это линейный поиск по списку колонок, и на 10k строк он бы дал
    ///    десятки тысяч лишних операций.
    ///  - CommandTimeout увеличен до 120 секунд: запрос с несколькими
    ///    оконными функциями и STRING_AGG может не уложиться в дефолтные 30.
    /// </summary>
    /// <param name="from">Начало периода (включительно) по Timestamp замера.</param>
    /// <param name="to">Конец периода (включительно) по Timestamp замера.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    public async Task<IEnumerable<StackSessionDto>> GetStackSessionsAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default)
    {
        var connection = Context.Database.GetDbConnection();
        var wasClosed = connection.State == ConnectionState.Closed;
        if (wasClosed)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = StackSessionsSql;

            // Тяжёлый отчёт с оконными функциями: даём запросу больше времени,
            // чем дефолтные 30 секунд. При необходимости вынести в настройки.
            command.CommandTimeout = 120;

            // Приводим границы периода к UTC — Timestamp в БД хранится в UTC.
            // В SQL-запросе мы фильтруем по m."Timestamp" (UTC), а уже в финальном
            // SELECT конвертируем в Asia/Yekaterinburg для отображения.
            var fromParam = command.CreateParameter();
            fromParam.ParameterName = "@From";
            fromParam.Value = from.ToUniversalTime();
            command.Parameters.Add(fromParam);

            var toParam = command.CreateParameter();
            toParam.ParameterName = "@To";
            toParam.Value = to.ToUniversalTime();
            command.Parameters.Add(toParam);

            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            // Выносим индексы колонок за цикл: GetOrdinal — линейный поиск
            // по списку колонок, и на большом числе строк повторные вызовы
            // дают заметный оверхед. Считаем один раз до цикла.
            var ordStackNumber = reader.GetOrdinal("StackNumber");
            var ordSessionId = reader.GetOrdinal("SessionId");
            var ordSessionStart = reader.GetOrdinal("SessionStart");
            var ordSessionEnd = reader.GetOrdinal("SessionEnd");
            var ordDuration = reader.GetOrdinal("Duration");
            var ordMeasurementsCount = reader.GetOrdinal("MeasurementsCount");
            var ordVehiclesCount = reader.GetOrdinal("VehiclesCount");
            var ordCounterpartyCount = reader.GetOrdinal("CounterpartyCount");
            var ordPrimaryCounterparty = reader.GetOrdinal("PrimaryCounterparty");
            var ordCounterparties = reader.GetOrdinal("Counterparties");
            var ordAverageHumidity = reader.GetOrdinal("AverageHumidity");
            var ordMinHumidity = reader.GetOrdinal("MinHumidity");
            var ordMaxHumidity = reader.GetOrdinal("MaxHumidity");

            var result = new List<StackSessionDto>();

            while (await reader.ReadAsync(cancellationToken))
            {
                result.Add(new StackSessionDto
                {
                    StackNumber = reader.GetString(ordStackNumber),
                    // SessionId в SQL явно приведён к int (::int) — GetInt32 безопасен.
                    SessionId = reader.GetInt32(ordSessionId),
                    SessionStart = reader.GetDateTime(ordSessionStart),
                    SessionEnd = reader.GetDateTime(ordSessionEnd),
                    Duration = reader.GetString(ordDuration),
                    // Счётчики в SQL приведены к int (::int) — GetInt32 безопасен.
                    MeasurementsCount = reader.GetInt32(ordMeasurementsCount),
                    VehiclesCount = reader.GetInt32(ordVehiclesCount),
                    CounterpartyCount = reader.GetInt32(ordCounterpartyCount),
                    PrimaryCounterparty = reader.IsDBNull(ordPrimaryCounterparty)
                        ? string.Empty
                        : reader.GetString(ordPrimaryCounterparty),
                    Counterparties = reader.IsDBNull(ordCounterparties)
                        ? string.Empty
                        : reader.GetString(ordCounterparties),
                    // Влажности — nullable: NULL в БД означает «нет данных»,
                    // и это отличается от «средняя 0%». Раньше мы возвращали 0,
                    // что на клиенте уходило в красную зону графика.
                    AverageHumidity = reader.IsDBNull(ordAverageHumidity)
                        ? (double?)null
                        : reader.GetDouble(ordAverageHumidity),
                    MinHumidity = reader.IsDBNull(ordMinHumidity)
                        ? (double?)null
                        : reader.GetDouble(ordMinHumidity),
                    MaxHumidity = reader.IsDBNull(ordMaxHumidity)
                        ? (double?)null
                        : reader.GetDouble(ordMaxHumidity)
                });
            }

            return result;
        }
        finally
        {
            if (wasClosed)
            {
                await connection.CloseAsync();
            }
        }
    }

    /// <summary>
    /// SQL-запрос для сводной статистики по штабелям за период.
    ///
    /// Считает в одном проходе:
    ///  - total_measurements — все замеры за период (без фильтра по штабелю);
    ///  - measurements_with_stack — замеры с заполненным StackNumber;
    ///  - measurements_without_stack — замеры без StackNumber;
    ///  - total_vehicles — уникальные машины с замерами;
    ///  - vehicles_with_stack — уникальные машины с заполненным StackNumber;
    ///  - vehicles_without_stack — уникальные машины без StackNumber;
    ///  - unique_stacks — уникальные штабели (по нормализованному номеру).
    ///
    /// FILTER (WHERE ...) — это стандартный PostgreSQL-синтаксис для
    /// условной агрегации. Работает быстрее, чем отдельные подзапросы.
    ///
    /// ВАЖНО про типы:
    ///  COUNT(*) и COUNT(DISTINCT ...) в Postgres возвращают bigint.
    ///  Npgsql 6+ не приводит типы молча, поэтому явно приводим к ::int,
    ///  чтобы reader.GetInt32() работал без InvalidCastException.
    /// </summary>
    private const string StackSessionsStatsSql = @"
SELECT
    COUNT(*)::int AS ""TotalMeasurements"",
    COUNT(*) FILTER (
        WHERE v.""StackNumber"" IS NOT NULL
          AND TRIM(v.""StackNumber"") <> ''
    )::int AS ""MeasurementsWithStack"",
    COUNT(*) FILTER (
        WHERE v.""StackNumber"" IS NULL
           OR TRIM(v.""StackNumber"") = ''
    )::int AS ""MeasurementsWithoutStack"",
    COUNT(DISTINCT v.""Id"")::int AS ""TotalVehicles"",
    COUNT(DISTINCT v.""Id"") FILTER (
        WHERE v.""StackNumber"" IS NOT NULL
          AND TRIM(v.""StackNumber"") <> ''
    )::int AS ""VehiclesWithStack"",
    COUNT(DISTINCT v.""Id"") FILTER (
        WHERE v.""StackNumber"" IS NULL
           OR TRIM(v.""StackNumber"") = ''
    )::int AS ""VehiclesWithoutStack"",
    COUNT(DISTINCT UPPER(TRANSLATE(TRIM(v.""StackNumber""), 'аА', 'aA'))) FILTER (
        WHERE v.""StackNumber"" IS NOT NULL
          AND TRIM(v.""StackNumber"") <> ''
    )::int AS ""UniqueStacks""
FROM ""Measurements"" m
JOIN ""Vehicles"" v ON v.""Id"" = m.""VehicleId""
WHERE m.""Timestamp"" >= @From
  AND m.""Timestamp"" <= @To;
";

    /// <summary>
    /// Получить сводную статистику по штабелям за период.
    ///
    /// Отдельный запрос от <see cref="GetStackSessionsAsync"/>, потому что
    /// статистика считается по «сырым» замерам (в т.ч. без штабеля),
    /// а не по сессиям. Запрос лёгкий — один проход по индексированному Timestamp.
    /// </summary>
    /// <param name="from">Начало периода (включительно) по Timestamp замера.</param>
    /// <param name="to">Конец периода (включительно) по Timestamp замера.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    public async Task<StackSessionsStatsDto> GetStackSessionsStatsAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default)
    {
        var connection = Context.Database.GetDbConnection();
        var wasClosed = connection.State == ConnectionState.Closed;
        if (wasClosed)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = StackSessionsStatsSql;
            command.CommandTimeout = 60;

            var fromParam = command.CreateParameter();
            fromParam.ParameterName = "@From";
            fromParam.Value = from.ToUniversalTime();
            command.Parameters.Add(fromParam);

            var toParam = command.CreateParameter();
            toParam.ParameterName = "@To";
            toParam.Value = to.ToUniversalTime();
            command.Parameters.Add(toParam);

            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            // Статистика — одна строка. Если по каким-то причинам строк нет
            // (например, не нашлось ни одного замера), возвращаем пустой DTO
            // со всеми нулями.
            if (!await reader.ReadAsync(cancellationToken))
            {
                return new StackSessionsStatsDto();
            }

            // Индексы колонок — один раз на строку (здесь их всего одна),
            // но паттерн оставляем единый с остальными методами.
            var ordTotalMeasurements = reader.GetOrdinal("TotalMeasurements");
            var ordMeasurementsWithStack = reader.GetOrdinal("MeasurementsWithStack");
            var ordMeasurementsWithoutStack = reader.GetOrdinal("MeasurementsWithoutStack");
            var ordTotalVehicles = reader.GetOrdinal("TotalVehicles");
            var ordVehiclesWithStack = reader.GetOrdinal("VehiclesWithStack");
            var ordVehiclesWithoutStack = reader.GetOrdinal("VehiclesWithoutStack");
            var ordUniqueStacks = reader.GetOrdinal("UniqueStacks");

            return new StackSessionsStatsDto
            {
                TotalMeasurements = reader.GetInt32(ordTotalMeasurements),
                MeasurementsWithStack = reader.GetInt32(ordMeasurementsWithStack),
                MeasurementsWithoutStack = reader.GetInt32(ordMeasurementsWithoutStack),
                TotalVehicles = reader.GetInt32(ordTotalVehicles),
                VehiclesWithStack = reader.GetInt32(ordVehiclesWithStack),
                VehiclesWithoutStack = reader.GetInt32(ordVehiclesWithoutStack),
                UniqueStacks = reader.GetInt32(ordUniqueStacks)
            };
        }
        finally
        {
            if (wasClosed)
            {
                await connection.CloseAsync();
            }
        }
    }
}