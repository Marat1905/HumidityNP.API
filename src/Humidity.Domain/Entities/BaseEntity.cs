namespace Humidity.Domain.Entities;

/// <summary>
/// Базовый класс для всех сущностей с временными метками.
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// Уникальный идентификатор.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Дата и время создания записи (UTC).
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Дата и время последнего обновления записи (UTC).
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; set; }
}