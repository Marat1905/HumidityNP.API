using Humidity.Domain.Common;

namespace Humidity.Application.Interfaces;

/// <summary>
/// Сервис для работы с поставщиками (аналитика по ИНН).
/// </summary>
public interface ISupplierService
{
    /// <summary>
    /// Получить список поставщиков с агрегированными данными за период (пагинированный).
    /// </summary>
    Task<PagedResult<SupplierDto>> GetSuppliersAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        int pageNumber,
        int pageSize,
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
    /// Получить топ-N поставщиков по средней влажности за период.
    /// </summary>
    /// <param name="top">Количество записей.</param>
    /// <param name="ascending">true — хорошие (низкая влажность), false — плохие (высокая).</param>
    /// <param name="from">Начало периода.</param>
    /// <param name="to">Конец периода.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Список DTO поставщиков.</returns>
    Task<IEnumerable<SupplierDto>> GetTopSuppliersAsync(
        int top,
        bool ascending,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default);
}