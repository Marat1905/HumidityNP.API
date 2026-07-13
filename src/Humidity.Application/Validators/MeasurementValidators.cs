using FluentValidation;
using Humidity.Application.DTOs;
using Humidity.Domain.Enums;

namespace Humidity.Application.Validators;

/// <summary>
/// Валидатор для создания записи о замере влажности.
/// </summary>
public class CreateMeasurementRequestValidator : AbstractValidator<CreateMeasurementRequest>
{
    public CreateMeasurementRequestValidator()
    {
        RuleFor(x => x.VehicleId)
            .NotEmpty().WithMessage("Идентификатор машины обязателен.");

        RuleFor(x => x.HumidityValue)
            .InclusiveBetween(0, 100).WithMessage("Значение влажности должно быть в диапазоне от 0 до 100%.");

        RuleFor(x => x.TemperatureC)
            .InclusiveBetween(-60, 100).WithMessage("Температура вне разумных пределов (-60 ... 100 °C).");

        RuleFor(x => x.Timestamp)
            .NotEmpty().WithMessage("Время замера обязательно.")
            .Must(BeValidDate).WithMessage("Некорректный формат времени замера.");

        RuleFor(x => x.MeasurementType).MaximumLength(50);
        RuleFor(x => x.Material).MaximumLength(100);
        RuleFor(x => x.Sign).MaximumLength(10);

        RuleFor(x => x.Source)
            .Must(BeValidSource).WithMessage("Источник данных должен быть 'Auto' или 'Manual'.");
    }

    private bool BeValidDate(string date) => !string.IsNullOrWhiteSpace(date) && DateTime.TryParse(date, out _);

    private bool BeValidSource(string source)
    {
        return Enum.TryParse<MeasurementSource>(source, true, out _);
    }
}

/// <summary>
/// Валидатор для обновления записи о замере.
/// </summary>
public class UpdateMeasurementRequestValidator : AbstractValidator<UpdateMeasurementRequest>
{
    public UpdateMeasurementRequestValidator()
    {
        RuleFor(x => x.HumidityValue)
            .InclusiveBetween(0, 100).WithMessage("Значение влажности должно быть в диапазоне от 0 до 100%.")
            .When(x => x.HumidityValue.HasValue);

        RuleFor(x => x.TemperatureC)
            .InclusiveBetween(-60, 100).WithMessage("Температура вне разумных пределов (-60 ... 100 °C).")
            .When(x => x.TemperatureC.HasValue);

        RuleFor(x => x.Source)
            .Must(BeValidSource).WithMessage("Источник данных должен быть 'Auto' или 'Manual'.")
            .When(x => !string.IsNullOrWhiteSpace(x.Source));

        RuleFor(x => x.MeasurementType).MaximumLength(50).When(x => x.MeasurementType != null);
        RuleFor(x => x.Material).MaximumLength(100).When(x => x.Material != null);
        RuleFor(x => x.Sign).MaximumLength(10).When(x => x.Sign != null);
    }

    private bool BeValidSource(string source)
    {
        return Enum.TryParse<MeasurementSource>(source, true, out _);
    }
}