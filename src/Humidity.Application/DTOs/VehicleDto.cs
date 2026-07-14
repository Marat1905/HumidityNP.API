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
    public DateTime Date { get; set; }

    /// <summary>
    /// Дата приезда.
    /// </summary>
    public DateTime ArrivalDate { get; set; }

    /// <summary>
    /// Дата въезда.
    /// </summary>
    public DateTime EntryDate { get; set; }

    /// <summary>
    /// Дата выезда (может быть null).
    /// </summary>
    public DateTime? ExitDate { get; set; }

    /// <summary>
    /// Контрагент.
    /// </summary>
    public string Counterparty { get; set; } = string.Empty;

    /// <summary>
    /// Вид работ.
    /// </summary>
    public string WorkType { get; set; } = string.Empty;

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
    /// ФИО грузчика.
    /// </summary>
    public string Loader { get; set; } = string.Empty;

    /// <summary>
    /// ФИО экспедитора.
    /// </summary>
    public string Expeditor { get; set; } = string.Empty;

    /// <summary>
    /// Подразделение.
    /// </summary>
    public string Department { get; set; } = string.Empty;
}

/// <summary>
/// Запрос на создание машины.
/// </summary>
public class CreateVehicleRequest
{
    public string Number { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public DateTime ArrivalDate { get; set; }
    public DateTime EntryDate { get; set; }
    public DateTime? ExitDate { get; set; }
    public string Counterparty { get; set; } = string.Empty;
    public string WorkType { get; set; } = string.Empty;
    public string VehicleBrand { get; set; } = string.Empty;
    public string VehiclePlate { get; set; } = string.Empty;
    public string Trailer { get; set; } = string.Empty;
    public string Driver { get; set; } = string.Empty;
    public string Loader { get; set; } = string.Empty;
    public string Expeditor { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
}

/// <summary>
/// Запрос на обновление машины.
/// </summary>
public class UpdateVehicleRequest
{
    public string? Number { get; set; }
    public string? Counterparty { get; set; }
    public string? WorkType { get; set; }
    public string? VehicleBrand { get; set; }
    public string? VehiclePlate { get; set; }
    public string? Trailer { get; set; }
    public string? Driver { get; set; }
    public string? Loader { get; set; }
    public string? Expeditor { get; set; }
    public string? Department { get; set; }
    public DateTime? ExitDate { get; set; }
}