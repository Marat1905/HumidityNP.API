namespace Humidity.Application.DTOs;

/// <summary>
/// DTO для данных о машине, полученных из 1С.
/// </summary>
public class OneCVehicleDto
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
    /// Дата выезда с площадки (может быть null).
    /// </summary>
    public DateTimeOffset? ExitDate { get; set; }

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
    /// Поставщик.
    /// </summary>
    public string Counterparty { get; set; } = string.Empty;

    /// <summary>
    /// ИНН поставщика.
    /// </summary>
    public string? Inn { get; set; }

    /// <summary>
    /// ФИО водителя.
    /// </summary>
    public string Driver { get; set; } = string.Empty;
}