using Humidity.Domain.Enums;

namespace Humidity.Application.DTOs;

/// <summary>
/// DTO для передачи данных о замере влажности клиенту.
/// </summary>
public class MeasurementDto
{
    /// <summary>
    /// Идентификатор замера.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Идентификатор машины.
    /// </summary>
    public Guid VehicleId { get; set; }

    /// <summary>
    /// Значение влажности (%).
    /// </summary>
    public double HumidityValue { get; set; }

    /// <summary>
    /// Температура (°C).
    /// </summary>
    public double TemperatureC { get; set; }

    /// <summary>
    /// Тип измерения.
    /// </summary>
    public string MeasurementType { get; set; } = string.Empty;

    /// <summary>
    /// Материал.
    /// </summary>
    public string Material { get; set; } = string.Empty;

    /// <summary>
    /// Источник данных (Auto/Manual).
    /// </summary>
    public MeasurementSource Source { get; set; }

    /// <summary>
    /// Дата и время замера.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Знак (Less/Greater/None).
    /// </summary>
    public SignType Sign { get; set; }

    /// <summary>
    /// Отображаемое значение влажности.
    /// </summary>
    public string DisplayValue
    {
        get
        {
            string sign = Sign == SignType.Less ? "<" : Sign == SignType.Greater ? ">" : "";
            return $"{sign} {HumidityValue:F1}%";
        }
    }
}

/// <summary>
/// Запрос на создание замера.
/// </summary>
public class CreateMeasurementRequest
{
    public Guid VehicleId { get; set; }
    public double HumidityValue { get; set; }
    public double TemperatureC { get; set; }
    public string MeasurementType { get; set; } = string.Empty;
    public string Material { get; set; } = string.Empty;
    public MeasurementSource Source { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public SignType Sign { get; set; }
}

/// <summary>
/// Запрос на обновление замера.
/// </summary>
public class UpdateMeasurementRequest
{
    public double? HumidityValue { get; set; }
    public double? TemperatureC { get; set; }
    public string? MeasurementType { get; set; }
    public string? Material { get; set; }
    public MeasurementSource? Source { get; set; }
    public SignType? Sign { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
}

/// <summary>
/// Результат массовой загрузки замеров.
/// </summary>
public class BulkMeasurementResult
{
    /// <summary>
    /// Количество успешно созданных замеров.
    /// </summary>
    public int CreatedCount { get; set; }

    /// <summary>
    /// Количество пропущенных замеров (из-за ошибок валидации или отсутствия машины).
    /// </summary>
    public int SkippedCount { get; set; }

    /// <summary>
    /// Список ошибок для каждого пропущенного замера.
    /// </summary>
    public IEnumerable<MeasurementBulkError> Errors { get; set; } = new List<MeasurementBulkError>();
}

/// <summary>
/// Детали ошибки для одного замера при массовой загрузке.
/// </summary>
public class MeasurementBulkError
{
    /// <summary>
    /// Порядковый номер записи во входном списке (начиная с 0).
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// Идентификатор машины, указанный в запросе.
    /// </summary>
    public Guid VehicleId { get; set; }

    /// <summary>
    /// Текст ошибки.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}