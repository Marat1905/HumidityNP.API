using System.Text.Json.Serialization;

namespace Humidity.Domain.Enums;

/// <summary>
/// Тип знака для значений вне диапазона.
/// Используется когда значение измерения выходит за пределы допустимого диапазона.
/// </summary>
/// <remarks>
/// Согласно документации X0 Series BLE Protocol:
/// - 16-битное значение показания включает два 1-битных флага
/// - Low out-of-range flag: старший бит (0x80)
/// - High out-of-range flag: второй старший бит (0x40)
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SignType
{
    /// <summary>
    /// Нет знака (значение в допустимом диапазоне).
    /// </summary>
    None,

    /// <summary>
    /// Значение ниже допустимого диапазона (меньше).
    /// Устанавливается когда Low out-of-range flag = 1.
    /// </summary>
    Less,

    /// <summary>
    /// Значение выше допустимого диапазона (больше).
    /// Устанавливается когда High out-of-range flag = 1.
    /// </summary>
    Greater
}