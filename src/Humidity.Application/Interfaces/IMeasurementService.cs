using Humidity.Application.DTOs;
using Humidity.Domain.Common;

namespace Humidity.Application.Interfaces;

/// <summary>
/// Сервис для управления записями о замерах влажности (CRUD).
/// </summary>
public interface IMeasurementService
{
    /// <summary>
    /// Получить все замеры для указанной машины.
    /// </summary>
    /// <param name="vehicleId">Идентификатор машины.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Коллекция DTO замеров, отсортированных по времени.</returns>
    Task<IEnumerable<MeasurementDto>> GetByVehicleIdAsync(Guid vehicleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить страницу замеров для указанной машины.
    /// </summary>
    /// <param name="vehicleId">Идентификатор машины.</param>
    /// <param name="pageNumber">Номер страницы.</param>
    /// <param name="pageSize">Размер страницы.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Страница замеров.</returns>
    Task<PagedResult<MeasurementDto>> GetByVehicleIdPagedAsync(Guid vehicleId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить последний замер для указанной машины.
    /// </summary>
    /// <param name="vehicleId">Идентификатор машины.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>DTO последнего замера или null.</returns>
    Task<MeasurementDto?> GetLatestByVehicleIdAsync(Guid vehicleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить все замеры за указанную дату.
    /// </summary>
    /// <param name="date">Дата (только дата, время игнорируется).</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Коллекция DTO замеров.</returns>
    Task<IEnumerable<MeasurementDto>> GetByDateAsync(DateTimeOffset date, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить страницу замеров за указанную дату.
    /// </summary>
    /// <param name="date">Дата.</param>
    /// <param name="pageNumber">Номер страницы.</param>
    /// <param name="pageSize">Размер страницы.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Страница замеров за день.</returns>
    Task<PagedResult<MeasurementDto>> GetByDatePagedAsync(DateTimeOffset date, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Создать новую запись о замере.
    /// </summary>
    /// <param name="request">Данные для создания.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>DTO созданного замера.</returns>
    Task<MeasurementDto> CreateAsync(CreateMeasurementRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Обновить существующую запись о замере.
    /// </summary>
    /// <param name="id">Идентификатор замера.</param>
    /// <param name="request">Данные для обновления.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>DTO обновлённого замера.</returns>
    Task<MeasurementDto> UpdateAsync(Guid id, UpdateMeasurementRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Удалить запись о замере.
    /// </summary>
    /// <param name="id">Идентификатор замера.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Массовая загрузка замеров (для выгрузки с мобильного приложения).
    /// Возвращает результат с количеством созданных и пропущенных записей и списком ошибок.
    /// </summary>
    /// <param name="requests">Список запросов на создание.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Результат массовой загрузки.</returns>
    Task<BulkMeasurementResult> BulkCreateAsync(IEnumerable<CreateMeasurementRequest> requests, CancellationToken cancellationToken = default);
}