using Humidity.Domain.Common;
using Humidity.Domain.Entities;

namespace Humidity.Domain.Interfaces;

/// <summary>
/// Интерфейс репозитория для работы с замерами влажности.
/// Расширяет базовый IRepository дополнительными методами, специфичными для HumidityMeasurement.
/// </summary>
public interface IMeasurementRepository : IRepository<HumidityMeasurement>
{
    /// <summary>
    /// Получить все замеры для указанной машины, отсортированные по времени (новые первыми).
    /// </summary>
    /// <param name="vehicleId">Идентификатор машины.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Коллекция замеров.</returns>
    Task<IEnumerable<HumidityMeasurement>> GetByVehicleIdAsync(Guid vehicleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить страницу замеров для указанной машины.
    /// </summary>
    /// <param name="vehicleId">Идентификатор машины.</param>
    /// <param name="pageNumber">Номер страницы.</param>
    /// <param name="pageSize">Размер страницы.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Страница замеров.</returns>
    Task<PagedResult<HumidityMeasurement>> GetByVehicleIdPagedAsync(Guid vehicleId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить последний (самый свежий) замер для указанной машины.
    /// </summary>
    /// <param name="vehicleId">Идентификатор машины.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Последний замер или null, если замеров нет.</returns>
    Task<HumidityMeasurement?> GetLatestByVehicleIdAsync(Guid vehicleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить все замеры за указанную дату.
    /// </summary>
    /// <param name="date">Дата (время игнорируется, берётся весь день).</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Коллекция замеров за день.</returns>
    Task<IEnumerable<HumidityMeasurement>> GetByDateAsync(DateTimeOffset date, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить страницу замеров за указанную дату.
    /// </summary>
    /// <param name="date">Дата.</param>
    /// <param name="pageNumber">Номер страницы.</param>
    /// <param name="pageSize">Размер страницы.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Страница замеров за день.</returns>
    Task<PagedResult<HumidityMeasurement>> GetByDatePagedAsync(DateTimeOffset date, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить замеры в диапазоне дат.
    /// </summary>
    /// <param name="from">Начало диапазона (включительно).</param>
    /// <param name="to">Конец диапазона (включительно).</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Коллекция замеров в диапазоне.</returns>
    Task<IEnumerable<HumidityMeasurement>> GetByDateRangeAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить словарь (VehicleId → количество замеров) для переданного списка идентификаторов машин.
    /// </summary>
    /// <param name="vehicleIds">Список идентификаторов машин.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Словарь, где ключ – VehicleId, значение – количество замеров.</returns>
    Task<Dictionary<Guid, int>> GetCountsByVehicleIdsAsync(IEnumerable<Guid> vehicleIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить статистику по замерам для указанной машины.
    /// </summary>
    /// <param name="vehicleId">Идентификатор машины.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Объект статистики.</returns>
    Task<MeasurementStatisticsDto> GetStatisticsByVehicleIdAsync(Guid vehicleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить страницу замеров в диапазоне дат.
    /// </summary>
    /// <param name="from">Начало диапазона (включительно).</param>
    /// <param name="to">Конец диапазона (включительно).</param>
    /// <param name="pageNumber">Номер страницы.</param>
    /// <param name="pageSize">Размер страницы.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Страница замеров.</returns>
    Task<PagedResult<HumidityMeasurement>> GetByDateRangePagedAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить сводку по поставщикам (группировка по ИНН) за период с пагинацией.
    /// </summary>
    Task<PagedResult<SupplierDto>> GetSuppliersSummaryAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить детальную информацию по поставщику (ИНН) за период.
    /// </summary>
    Task<SupplierDetailsDto> GetSupplierDetailsAsync(
        string inn,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default);
}