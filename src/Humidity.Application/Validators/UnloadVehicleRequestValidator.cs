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
        // Общее количество тюков не может быть отрицательным.
        RuleFor(x => x.BaleCount)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Количество тюков не может быть отрицательным.");

        // Количество порванных тюков не может быть отрицательным.
        RuleFor(x => x.DamagedBaleCount)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Количество порванных тюков не может быть отрицательным.");

        // Вес: если есть порванные тюки, вес должен быть строго больше 0.
        // Если порванных нет, вес может быть равен 0 (но не отрицательным).
        RuleFor(x => x.WeightKg)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Вес не может быть отрицательным.")
            .Must((request, weight) =>
            {
                // Если порванных тюков больше 0, то вес должен быть > 0.
                if (request.DamagedBaleCount > 0)
                    return weight > 0;
                // Иначе вес может быть любым >= 0.
                return true;
            })
            .WithMessage("При наличии порванных тюков вес должен быть больше 0.");

        // Номер штабеля обязателен и не должен превышать 50 символов.
        RuleFor(x => x.StackNumber)
            .NotEmpty()
            .WithMessage("Номер штабеля обязателен.")
            .MaximumLength(50)
            .WithMessage("Номер штабеля не должен превышать 50 символов.");

        // Количество порванных тюков не может превышать общее количество тюков.
        RuleFor(x => x)
            .Must(x => x.DamagedBaleCount <= x.BaleCount)
            .WithMessage("Количество порванных тюков не может превышать общее количество тюков.");
    }
}