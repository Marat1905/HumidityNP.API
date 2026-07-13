using FluentValidation;
using Humidity.Application.DTOs;

namespace Humidity.Application.Validators;

/// <summary>
/// Валидатор для создания записи о машине.
/// </summary>
public class CreateVehicleRequestValidator : AbstractValidator<CreateVehicleRequest>
{
    public CreateVehicleRequestValidator()
    {
        RuleFor(x => x.Number)
            .NotEmpty().WithMessage("Номер заявки обязателен.")
            .MaximumLength(50).WithMessage("Номер заявки не может быть длиннее 50 символов.");

        RuleFor(x => x.Date)
            .NotEmpty().WithMessage("Дата создания обязательна.")
            .Must(BeValidDate).WithMessage("Некорректный формат даты создания.");

        RuleFor(x => x.ArrivalDate)
            .NotEmpty().WithMessage("Дата приезда обязательна.")
            .Must(BeValidDate).WithMessage("Некорректный формат даты приезда.");

        RuleFor(x => x.EntryDate)
            .NotEmpty().WithMessage("Дата въезда обязательна.")
            .Must(BeValidDate).WithMessage("Некорректный формат даты въезда.");

        RuleFor(x => x.ExitDate)
            .Must(BeValidDateOrNull).WithMessage("Некорректный формат даты выезда.");

        RuleFor(x => x.VehiclePlate)
            .NotEmpty().WithMessage("Гос. номер автомобиля обязателен.")
            .MaximumLength(20).WithMessage("Гос. номер не может быть длиннее 20 символов.");

        RuleFor(x => x.Counterparty).MaximumLength(200);
        RuleFor(x => x.WorkType).MaximumLength(100);
        RuleFor(x => x.VehicleBrand).MaximumLength(100);
        RuleFor(x => x.Trailer).MaximumLength(20);
        RuleFor(x => x.Driver).MaximumLength(200);
        RuleFor(x => x.Loader).MaximumLength(200);
        RuleFor(x => x.Expeditor).MaximumLength(200);
        RuleFor(x => x.Department).MaximumLength(100);
    }

    private bool BeValidDate(string date) => !string.IsNullOrWhiteSpace(date) && DateTime.TryParse(date, out _);
    private bool BeValidDateOrNull(string? date) => string.IsNullOrWhiteSpace(date) || DateTime.TryParse(date, out _);
}

/// <summary>
/// Валидатор для обновления записи о машине.
/// Проверяет форматы только тех полей, которые были переданы (не null).
/// </summary>
public class UpdateVehicleRequestValidator : AbstractValidator<UpdateVehicleRequest>
{
    public UpdateVehicleRequestValidator()
    {
        RuleFor(x => x.Number).MaximumLength(50).When(x => x.Number != null);
        RuleFor(x => x.ExitDate).Must(BeValidDateOrNull).When(x => x.ExitDate != null)
            .WithMessage("Некорректный формат даты выезда.");
        RuleFor(x => x.VehiclePlate).MaximumLength(20).When(x => x.VehiclePlate != null);
        RuleFor(x => x.Counterparty).MaximumLength(200).When(x => x.Counterparty != null);
        RuleFor(x => x.WorkType).MaximumLength(100).When(x => x.WorkType != null);
        RuleFor(x => x.VehicleBrand).MaximumLength(100).When(x => x.VehicleBrand != null);
        RuleFor(x => x.Trailer).MaximumLength(20).When(x => x.Trailer != null);
        RuleFor(x => x.Driver).MaximumLength(200).When(x => x.Driver != null);
        RuleFor(x => x.Loader).MaximumLength(200).When(x => x.Loader != null);
        RuleFor(x => x.Expeditor).MaximumLength(200).When(x => x.Expeditor != null);
        RuleFor(x => x.Department).MaximumLength(100).When(x => x.Department != null);
    }

    private bool BeValidDateOrNull(string? date) => string.IsNullOrWhiteSpace(date) || DateTime.TryParse(date, out _);
}