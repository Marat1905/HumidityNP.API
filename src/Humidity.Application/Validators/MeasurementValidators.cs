using FluentValidation;
using Humidity.Application.DTOs;
using Humidity.Domain.Enums;

namespace Humidity.Application.Validators;

/// <summary>
/// Валидатор для запроса на создание замера влажности (CreateMeasurementRequest).
/// Проверяет обязательность полей, диапазоны числовых значений, корректность даты и допустимость перечислений.
/// </summary>
public class CreateMeasurementRequestValidator : AbstractValidator<CreateMeasurementRequest>
{
    public CreateMeasurementRequestValidator()
    {
        // Идентификатор машины: обязателен (не может быть пустым Guid)
        RuleFor(x => x.VehicleId)
            .NotEmpty().WithMessage("Идентификатор машины обязателен.");

        // Значение влажности: обязательно, в диапазоне от 0 до 100 процентов
        RuleFor(x => x.HumidityValue)
            .InclusiveBetween(0, 100)
            .WithMessage("Значение влажности должно быть в диапазоне от 0 до 100%.");

        // Температура: обязательна, в разумном диапазоне (от -50 до +100 °C)
        RuleFor(x => x.TemperatureC)
            .InclusiveBetween(-50, 100)
            .WithMessage("Температура должна быть в диапазоне от -50 до +100 °C.");

        // Тип измерения: обязателен, максимум 50 символов
        RuleFor(x => x.MeasurementType)
            .NotEmpty().WithMessage("Тип измерения обязателен.")
            .MaximumLength(50).WithMessage("Тип измерения не может быть длиннее 50 символов.");

        // Материал: обязателен, максимум 100 символов
        RuleFor(x => x.Material)
            .NotEmpty().WithMessage("Материал обязателен.")
            .MaximumLength(100).WithMessage("Материал не может быть длиннее 100 символов.");

        // Источник данных: проверяем, что переданное значение определено в перечислении
        RuleFor(x => x.Source)
            .IsInEnum().WithMessage("Недопустимое значение для источника данных. Допустимые значения: Auto, Manual.");

        // Дата и время замера: обязательна, не может быть в будущем
        RuleFor(x => x.Timestamp)
            .NotEmpty().WithMessage("Дата и время замера обязательны.")
            .LessThanOrEqualTo(DateTimeOffset.UtcNow.AddMinutes(1))
            .WithMessage("Дата и время замера не могут быть в будущем.");

        // Знак: проверяем, что переданное значение определено в перечислении
        RuleFor(x => x.Sign)
            .IsInEnum().WithMessage("Недопустимое значение для знака. Допустимые значения: None, Less, Greater.");
    }
}

/// <summary>
/// Валидатор для запроса на обновление замера влажности (UpdateMeasurementRequest).
/// Все поля опциональны, проверяются только диапазоны, длина строк и допустимость перечислений (если указаны).
/// </summary>
public class UpdateMeasurementRequestValidator : AbstractValidator<UpdateMeasurementRequest>
{
    public UpdateMeasurementRequestValidator()
    {
        // Значение влажности: если указано, в диапазоне от 0 до 100 процентов
        RuleFor(x => x.HumidityValue)
            .InclusiveBetween(0, 100)
            .WithMessage("Значение влажности должно быть в диапазоне от 0 до 100%.");

        // Температура: если указана, в разумном диапазоне (от -50 до +100 °C)
        RuleFor(x => x.TemperatureC)
            .InclusiveBetween(-50, 100)
            .WithMessage("Температура должна быть в диапазоне от -50 до +100 °C.");

        // Тип измерения: максимум 50 символов
        RuleFor(x => x.MeasurementType)
            .MaximumLength(50).WithMessage("Тип измерения не может быть длиннее 50 символов.");

        // Материал: максимум 100 символов
        RuleFor(x => x.Material)
            .MaximumLength(100).WithMessage("Материал не может быть длиннее 100 символов.");

        // Источник данных: если указан, проверяем, что значение определено в перечислении
        RuleFor(x => x.Source)
            .IsInEnum().When(x => x.Source.HasValue)
            .WithMessage("Недопустимое значение для источника данных. Допустимые значения: Auto, Manual.");

        // Знак: если указан, проверяем, что значение определено в перечислении
        RuleFor(x => x.Sign)
            .IsInEnum().When(x => x.Sign.HasValue)
            .WithMessage("Недопустимое значение для знака. Допустимые значения: None, Less, Greater.");

        // Дата и время замера: если указана, не может быть в будущем
        RuleFor(x => x.Timestamp)
            .Must(timestamp => timestamp == null || timestamp <= DateTimeOffset.UtcNow.AddMinutes(1))
            .WithMessage("Дата и время замера не могут быть в будущем.");
    }
}