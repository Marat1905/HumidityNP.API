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
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Дата и время замера.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Знак (Less/Greater/None).
    /// </summary>
    public string Sign { get; set; } = string.Empty;

    /// <summary>
    /// Отображаемое значение влажности.
    /// </summary>
    public string DisplayValue { get; set; } = string.Empty;
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
    public string Source { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Sign { get; set; } = string.Empty;
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
    public string? Source { get; set; }
    public string? Sign { get; set; }
    public DateTime? Timestamp { get; set; }
}