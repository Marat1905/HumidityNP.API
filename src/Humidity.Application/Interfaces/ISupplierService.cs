using Humidity.Domain.Common;

namespace Humidity.Application.Interfaces;

/// <summary>
/// Сервис для работы с поставщиками (аналитика по ИНН).
/// </summary>
public interface ISupplierService
{
    /// <summary>
    /// Получить список поставщиков с агрегированными данными за период (пагинированный).
    ///
    /// Параметр <paramref name="search"/> позволяет фильтровать поставщиков по частичному
    /// совпадению ИНН или наименования (регистронезависимо). Пустая строка / null — без фильтра.
    /// </summary>
    /// <param name="from">Начало периода.</param>
    /// <param name="to">Конец периода.</param>
    /// <param name="pageNumber">Номер страницы (начиная с 1).</param>
    /// <param name="pageSize">Размер страницы.</param>
    /// <param name="search">Строка поиска по ИНН или наименованию поставщика.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    Task<PagedResult<SupplierDto>> GetSuppliersAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        int pageNumber,
        int pageSize,
        string? search = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить детальную информацию по одному поставщику (ИНН) за период.
    /// Пагинация и сортировка выполняются на стороне сервера.
    /// </summary>
    /// <param name="inn">ИНН поставщика.</param>
    /// <param name="from">Начало периода.</param>
    /// <param name="to">Конец периода.</param>
    /// <param name="pageNumber">Номер страницы (начиная с 1).</param>
    /// <param name="pageSize">Размер страницы.</param>
    /// <param name="sortDescending">true – сортировка по дате въезда по убыванию, false – по возрастанию.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    Task<SupplierDetailsDto> GetSupplierDetailsAsync(
        string inn,
        DateTimeOffset from,
        DateTimeOffset to,
        int pageNumber,
        int pageSize,
        bool sortDescending,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить полный (без пагинации) список машин поставщика за период,
    /// отсортированный по дате въезда. Используется для построения графика.
    /// </summary>
    /// <param name="inn">ИНН поставщика.</param>
    /// <param name="from">Начало периода.</param>
    /// <param name="to">Конец периода.</param>
    /// <param name="sortDescending">true – сортировка по дате въезда по убыванию, false – по возрастанию.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    Task<IEnumerable<SupplierVehicleSummaryDto>> GetSupplierVehiclesForChartAsync(
        string inn,
        DateTimeOffset from,
        DateTimeOffset to,
        bool sortDescending,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить топ-N поставщиков по средней влажности за период с байесовской коррекцией.
    ///
    /// Байесовская коррекция (credibility adjustment) сглаживает средние у поставщиков
    /// с малым количеством замеров: их средняя «тянется» к глобальной средней m по формуле
    ///     adjusted = (C * m + sum) / (C + n)
    /// где C — priorWeight, n — количество замеров у поставщика, sum — сумма влажностей.
    ///
    /// Параметр priorWeight задаёт силу сглаживания:
    ///   - 0   — коррекция отключена (наивная средняя);
    ///   - 10  — слабая;
    ///   - 30  — умеренная (по умолчанию);
    ///   - 100 — сильная.
    /// </summary>
    /// <param name="top">Количество записей в топе (максимум 100).</param>
    /// <param name="ascending">true — хорошие (низкая влажность), false — плохие (высокая).</param>
    /// <param name="from">Начало периода.</param>
    /// <param name="to">Конец периода.</param>
    /// <param name="priorWeight">Вес prior (C) для байесовской коррекции.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Список DTO поставщиков с наивной и скорректированной средней влажностью.</returns>
    Task<IEnumerable<SupplierDto>> GetTopSuppliersAsync(
        int top,
        bool ascending,
        DateTimeOffset from,
        DateTimeOffset to,
        double priorWeight,
        CancellationToken cancellationToken = default);
}