using Humidity.Domain.Enums;

namespace Humidity.Domain.Entities;

/// <summary>
/// Машина, въезжающая на площадку.
/// Содержит информацию о номере пропуска, датах, поставщике, транспортном средстве и водителе.
/// </summary>
public class Vehicle : BaseEntity
{
    /// <summary>
    /// Номер пропуска (например, Я-9310099848).
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
    /// Дата выезда с площадки (может быть null, если машина ещё не выехала).
    /// </summary>
    public DateTimeOffset? ExitDate { get; set; }

    /// <summary>
    /// Поставщик (например, "Тандер(Сургут)").
    /// </summary>
    public string Counterparty { get; set; } = string.Empty;

    /// <summary>
    /// ИНН поставщика.
    /// </summary>
    public string? Inn { get; set; }

    /// <summary>
    /// Марка автомобиля (например, "FAW", "KAMAZ").
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
    /// Коллекция замеров влажности, привязанных к данной машине.
    /// Отношение "один ко многим": одна машина может иметь несколько замеров.
    /// При удалении машины все связанные замеры удаляются каскадно.
    /// </summary>
    public virtual ICollection<HumidityMeasurement> Measurements { get; set; } = new List<HumidityMeasurement>();
}