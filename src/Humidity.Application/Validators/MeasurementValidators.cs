using FluentValidation;
using Humidity.Application.DTOs;

namespace Humidity.Application.Validators;

/// <summary>
/// Валидатор для запроса на создание замера влажности (CreateMeasurementRequest).
/// Проверяет обязательность полей, диапазоны числовых значений и корректность даты.
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

        // Источник данных: обязателен, максимум 20 символов
        RuleFor(x => x.Source)
            .NotEmpty().WithMessage("Источник данных обязателен.")
            .MaximumLength(20).WithMessage("Источник данных не может быть длиннее 20 символов.");

        // Дата и время замера: обязательна, не может быть в будущем
        RuleFor(x => x.Timestamp)
            .NotEmpty().WithMessage("Дата и время замера обязательны.")
            .LessThanOrEqualTo(DateTime.UtcNow.AddMinutes(1))
            .WithMessage("Дата и время замера не могут быть в будущем.");

        // Знак (Less/Greater/None): обязателен, максимум 10 символов
        RuleFor(x => x.Sign)
            .NotEmpty().WithMessage("Знак обязателен.")
            .MaximumLength(10).WithMessage("Знак не может быть длиннее 10 символов.");
    }
}

/// <summary>
/// Валидатор для запроса на обновление замера влажности (UpdateMeasurementRequest).
/// Все поля опциональны, проверяются только диапазоны и длина строк.
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

        // Источник данных: максимум 20 символов
        RuleFor(x => x.Source)
            .MaximumLength(20).WithMessage("Источник данных не может быть длиннее 20 символов.");

        // Знак: максимум 10 символов
        RuleFor(x => x.Sign)
            .MaximumLength(10).WithMessage("Знак не может быть длиннее 10 символов.");

        // Дата и время замера: если указана, не может быть в будущем
        RuleFor(x => x.Timestamp)
            .Must(timestamp => timestamp == null || timestamp <= DateTime.UtcNow.AddMinutes(1))
            .WithMessage("Дата и время замера не могут быть в будущем.");
    }
}