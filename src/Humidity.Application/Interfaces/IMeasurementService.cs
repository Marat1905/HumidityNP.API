using Humidity.Application.DTOs;

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
    /// <returns>Коллекция DTO замеров, отсортированных по времени.</returns>
    Task<IEnumerable<MeasurementDto>> GetByVehicleIdAsync(Guid vehicleId);

    /// <summary>
    /// Получить последний замер для указанной машины.
    /// </summary>
    /// <param name="vehicleId">Идентификатор машины.</param>
    /// <returns>DTO последнего замера или null.</returns>
    Task<MeasurementDto?> GetLatestByVehicleIdAsync(Guid vehicleId);

    /// <summary>
    /// Получить все замеры за указанную дату.
    /// </summary>
    /// <param name="date">Дата (только дата, время игнорируется).</param>
    /// <returns>Коллекция DTO замеров.</returns>
    Task<IEnumerable<MeasurementDto>> GetByDateAsync(DateTime date);

    /// <summary>
    /// Создать новую запись о замере.
    /// </summary>
    /// <param name="request">Данные для создания.</param>
    /// <returns>DTO созданного замера.</returns>
    Task<MeasurementDto> CreateAsync(CreateMeasurementRequest request);

    /// <summary>
    /// Обновить существующую запись о замере.
    /// </summary>
    /// <param name="id">Идентификатор замера.</param>
    /// <param name="request">Данные для обновления.</param>
    /// <returns>DTO обновлённого замера.</returns>
    Task<MeasurementDto> UpdateAsync(Guid id, UpdateMeasurementRequest request);

    /// <summary>
    /// Удалить запись о замере.
    /// </summary>
    /// <param name="id">Идентификатор замера.</param>
    Task DeleteAsync(Guid id);

    /// <summary>
    /// Массовая загрузка замеров (для выгрузки с мобильного приложения).
    /// </summary>
    /// <param name="requests">Список запросов на создание.</param>
    /// <returns>Список созданных замеров.</returns>
    Task<IEnumerable<MeasurementDto>> BulkCreateAsync(IEnumerable<CreateMeasurementRequest> requests);
}