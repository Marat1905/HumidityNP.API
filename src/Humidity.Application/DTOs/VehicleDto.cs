namespace Humidity.Application.DTOs;

/// <summary>
/// DTO для передачи данных о машине клиенту.
/// </summary>
public class VehicleDto
{
    /// <summary>
    /// Идентификатор машины.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Номер пропуска.
    /// </summary>
    public string Number { get; set; } = string.Empty;

    /// <summary>
    /// Дата создания пропуска.
    /// </summary>
    public DateTimeOffset Date { get; set; }

    /// <summary>
    /// Дата въезда на площадку.
    /// </summary>
    public DateTimeOffset EntryDate { get; set; }

    /// <summary>
    /// Дата выезда с площадки (может быть null).
    /// </summary>
    public DateTimeOffset? ExitDate { get; set; }

    /// <summary>
    /// Поставщик.
    /// </summary>
    public string Counterparty { get; set; } = string.Empty;

    /// <summary>
    /// ИНН поставщика.
    /// </summary>
    public string? Inn { get; set; }

    /// <summary>
    /// Марка автомобиля.
    /// </summary>
    public string VehicleBrand { get; set; } = string.Empty;

    /// <summary>
    /// Государственный номер автомобиля.
    /// </summary>
    public string VehiclePlate { get; set; } = string.Empty;

    /// <summary>
    /// Номер прицепа.
    /// </summary>
    public string Trailer { get; set; } = string.Empty;

    /// <summary>
    /// ФИО водителя.
    /// </summary>
    public string Driver { get; set; } = string.Empty;

    /// <summary>
    /// Количество замеров влажности, выполненных для данной машины.
    /// </summary>
    public int MeasurementsCount { get; set; }
}

/// <summary>
/// Запрос на создание машины.
/// </summary>
public class CreateVehicleRequest
{
    /// <summary>
    /// Номер пропуска.
    /// </summary>
    public string Number { get; set; } = string.Empty;

    /// <summary>
    /// Дата создания пропуска.
    /// </summary>
    public DateTimeOffset Date { get; set; }

    /// <summary>
    /// Дата въезда на площадку.
    /// </summary>
    public DateTimeOffset EntryDate { get; set; }

    /// <summary>
    /// Дата выезда с площадки (опционально).
    /// </summary>
    public DateTimeOffset? ExitDate { get; set; }

    /// <summary>
    /// Поставщик.
    /// </summary>
    public string Counterparty { get; set; } = string.Empty;

    /// <summary>
    /// ИНН поставщика.
    /// </summary>
    public string? Inn { get; set; }

    /// <summary>
    /// Марка автомобиля.
    /// </summary>
    public string VehicleBrand { get; set; } = string.Empty;

    /// <summary>
    /// Государственный номер автомобиля.
    /// </summary>
    public string VehiclePlate { get; set; } = string.Empty;

    /// <summary>
    /// Номер прицепа.
    /// </summary>
    public string Trailer { get; set; } = string.Empty;

    /// <summary>
    /// ФИО водителя.
    /// </summary>
    public string Driver { get; set; } = string.Empty;
}

/// <summary>
/// Запрос на обновление машины.
/// </summary>
public class UpdateVehicleRequest
{
    /// <summary>
    /// Номер пропуска.
    /// </summary>
    public string? Number { get; set; }

    /// <summary>
    /// Поставщик.
    /// </summary>
    public string? Counterparty { get; set; }

    /// <summary>
    /// ИНН поставщика.
    /// </summary>
    public string? Inn { get; set; }

    /// <summary>
    /// Марка автомобиля.
    /// </summary>
    public string? VehicleBrand { get; set; }

    /// <summary>
    /// Государственный номер автомобиля.
    /// </summary>
    public string? VehiclePlate { get; set; }

    /// <summary>
    /// Номер прицепа.
    /// </summary>
    public string? Trailer { get; set; }

    /// <summary>
    /// ФИО водителя.
    /// </summary>
    public string? Driver { get; set; }

    /// <summary>
    /// Дата выезда с площадки.
    /// </summary>
    public DateTimeOffset? ExitDate { get; set; }
}