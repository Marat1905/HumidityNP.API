// Application/Validators/UnloadVehicleRequestValidator.cs
using FluentValidation;
using Humidity.Application.DTOs;

namespace Humidity.Application.Validators;

/// <summary>
/// Валидатор для запроса фиксации разгрузки машины.
/// </summary>
public class UnloadVehicleRequestValidator : AbstractValidator<UnloadVehicleRequest>
{
    public UnloadVehicleRequestValidator()
    {
        RuleFor(x => x.BaleCount)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Количество тюков не может быть отрицательным.");

        RuleFor(x => x.DamagedBaleCount)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Количество порванных тюков не может быть отрицательным.");

        RuleFor(x => x.WeightKg)
            .GreaterThan(0)
            .WithMessage("Вес должен быть больше 0.");

        RuleFor(x => x.StackNumber)
            .NotEmpty()
            .WithMessage("Номер штабеля обязателен.")
            .MaximumLength(50)
            .WithMessage("Номер штабеля не должен превышать 50 символов.");

        // Дополнительно: количество порванных не может превышать общее количество
        RuleFor(x => x)
            .Must(x => x.DamagedBaleCount <= x.BaleCount)
            .WithMessage("Количество порванных тюков не может превышать общее количество тюков.");
    }
}