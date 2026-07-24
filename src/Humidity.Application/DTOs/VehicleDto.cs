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
    /// Номер заявки.
    /// </summary>
    public string Number { get; set; } = string.Empty;

    /// <summary>
    /// Дата создания записи.
    /// </summary>
    public DateTimeOffset Date { get; set; }

    /// <summary>
    /// Дата въезда.
    /// </summary>
    public DateTimeOffset EntryDate { get; set; }

    /// <summary>
    /// Дата выезда (может быть null).
    /// </summary>
    public DateTimeOffset? ExitDate { get; set; }

    /// <summary>
    /// Контрагент.
    /// </summary>
    public string Counterparty { get; set; } = string.Empty;

    /// <summary>
    /// ИНН контрагента.
    /// </summary>
    public string? Inn { get; set; }

    /// <summary>
    /// Марка автомобиля.
    /// </summary>
    public string VehicleBrand { get; set; } = string.Empty;

    /// <summary>
    /// Государственный номер.
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
    public string Number { get; set; } = string.Empty;
    public DateTimeOffset Date { get; set; }
    public DateTimeOffset EntryDate { get; set; }
    public DateTimeOffset? ExitDate { get; set; }
    public string Counterparty { get; set; } = string.Empty;
    public string? Inn { get; set; }
    public string VehicleBrand { get; set; } = string.Empty;
    public string VehiclePlate { get; set; } = string.Empty;
    public string Trailer { get; set; } = string.Empty;
    public string Driver { get; set; } = string.Empty;
}

/// <summary>
/// Запрос на обновление машины.
/// </summary>
public class UpdateVehicleRequest
{
    public string? Number { get; set; }
    public string? Counterparty { get; set; }
    public string? Inn { get; set; }
    public string? VehicleBrand { get; set; }
    public string? VehiclePlate { get; set; }
    public string? Trailer { get; set; }
    public string? Driver { get; set; }
    public DateTimeOffset? ExitDate { get; set; }
}