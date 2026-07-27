using Humidity.Application.DTOs;
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
    /// </summary>
    Task<SupplierDetailsDto> GetSupplierDetailsAsync(
        string inn,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default);
}