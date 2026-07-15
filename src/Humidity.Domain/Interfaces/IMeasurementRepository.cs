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
    /// <returns>Коллекция замеров.</returns>
    Task<IEnumerable<HumidityMeasurement>> GetByVehicleIdAsync(Guid vehicleId);

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
    /// <returns>Последний замер или null, если замеров нет.</returns>
    Task<HumidityMeasurement?> GetLatestByVehicleIdAsync(Guid vehicleId);

    /// <summary>
    /// Получить все замеры за указанную дату.
    /// </summary>
    /// <param name="date">Дата (время игнорируется, берётся весь день).</param>
    /// <returns>Коллекция замеров за день.</returns>
    Task<IEnumerable<HumidityMeasurement>> GetByDateAsync(DateTime date);

    /// <summary>
    /// Получить страницу замеров за указанную дату.
    /// </summary>
    /// <param name="date">Дата.</param>
    /// <param name="pageNumber">Номер страницы.</param>
    /// <param name="pageSize">Размер страницы.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Страница замеров за день.</returns>
    Task<PagedResult<HumidityMeasurement>> GetByDatePagedAsync(DateTime date, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить замеры в диапазоне дат.
    /// </summary>
    /// <param name="from">Начало диапазона (включительно).</param>
    /// <param name="to">Конец диапазона (включительно).</param>
    /// <returns>Коллекция замеров в диапазоне.</returns>
    Task<IEnumerable<HumidityMeasurement>> GetByDateRangeAsync(DateTime from, DateTime to);
}