using FluentValidation;
using Humidity.Application.DTOs;

namespace Humidity.Application.Validators;

/// <summary>
/// Валидатор для запроса на создание машины (CreateVehicleRequest).
/// Проверяет обязательность полей, длину строк и корректность дат.
/// </summary>
public class CreateVehicleRequestValidator : AbstractValidator<CreateVehicleRequest>
{
    public CreateVehicleRequestValidator()
    {
        // Номер заявки: обязателен, максимум 50 символов
        RuleFor(x => x.Number)
            .NotEmpty().WithMessage("Номер заявки обязателен.")
            .MaximumLength(50).WithMessage("Номер заявки не может быть длиннее 50 символов.");

        // Дата создания записи: обязательна, не может быть в будущем
        RuleFor(x => x.Date)
            .NotEmpty().WithMessage("Дата создания записи обязательна.")
            .LessThanOrEqualTo(DateTime.UtcNow.AddMinutes(1))
            .WithMessage("Дата создания записи не может быть в будущем.");

        // Дата приезда: обязательна, не может быть в будущем
        RuleFor(x => x.ArrivalDate)
            .NotEmpty().WithMessage("Дата приезда обязательна.")
            .LessThanOrEqualTo(DateTime.UtcNow.AddMinutes(1))
            .WithMessage("Дата приезда не может быть в будущем.");

        // Дата въезда: обязательна, не может быть в будущем, не должна быть раньше даты приезда
        RuleFor(x => x.EntryDate)
            .NotEmpty().WithMessage("Дата въезда обязательна.")
            .LessThanOrEqualTo(DateTime.UtcNow.AddMinutes(1))
            .WithMessage("Дата въезда не может быть в будущем.")
            .GreaterThanOrEqualTo(x => x.ArrivalDate)
            .WithMessage("Дата въезда не может быть раньше даты приезда.");

        // Дата выезда: опциональна, но если указана, должна быть не раньше даты въезда и не в будущем
        RuleFor(x => x.ExitDate)
            .Must((vehicle, exitDate) => exitDate == null || exitDate >= vehicle.EntryDate)
            .WithMessage("Дата выезда не может быть раньше даты въезда.")
            .Must(exitDate => exitDate == null || exitDate <= DateTime.UtcNow.AddMinutes(1))
            .WithMessage("Дата выезда не может быть в будущем.");

        // Контрагент: обязателен, максимум 100 символов
        RuleFor(x => x.Counterparty)
            .NotEmpty().WithMessage("Контрагент обязателен.")
            .MaximumLength(100).WithMessage("Контрагент не может быть длиннее 100 символов.");

        // Вид работ: обязателен, максимум 50 символов
        RuleFor(x => x.WorkType)
            .NotEmpty().WithMessage("Вид работ обязателен.")
            .MaximumLength(50).WithMessage("Вид работ не может быть длиннее 50 символов.");

        // Марка автомобиля: обязательна, максимум 50 символов
        RuleFor(x => x.VehicleBrand)
            .NotEmpty().WithMessage("Марка автомобиля обязательна.")
            .MaximumLength(50).WithMessage("Марка автомобиля не может быть длиннее 50 символов.");

        // Государственный номер: обязателен, максимум 20 символов
        RuleFor(x => x.VehiclePlate)
            .NotEmpty().WithMessage("Государственный номер обязателен.")
            .MaximumLength(20).WithMessage("Государственный номер не может быть длиннее 20 символов.");

        // Номер прицепа: опционален, максимум 20 символов
        RuleFor(x => x.Trailer)
            .MaximumLength(20).WithMessage("Номер прицепа не может быть длиннее 20 символов.");

        // ФИО водителя: обязательно, максимум 100 символов
        RuleFor(x => x.Driver)
            .NotEmpty().WithMessage("ФИО водителя обязательно.")
            .MaximumLength(100).WithMessage("ФИО водителя не может быть длиннее 100 символов.");

        // ФИО грузчика: опционально, максимум 100 символов
        RuleFor(x => x.Loader)
            .MaximumLength(100).WithMessage("ФИО грузчика не может быть длиннее 100 символов.");

        // ФИО экспедитора: опционально, максимум 100 символов
        RuleFor(x => x.Expeditor)
            .MaximumLength(100).WithMessage("ФИО экспедитора не может быть длиннее 100 символов.");

        // Подразделение: обязательно, максимум 100 символов
        RuleFor(x => x.Department)
            .NotEmpty().WithMessage("Подразделение обязательно.")
            .MaximumLength(100).WithMessage("Подразделение не может быть длиннее 100 символов.");
    }
}

/// <summary>
/// Валидатор для запроса на обновление машины (UpdateVehicleRequest).
/// Проверяет длину строк и корректность дат (все поля опциональны).
/// </summary>
public class UpdateVehicleRequestValidator : AbstractValidator<UpdateVehicleRequest>
{
    public UpdateVehicleRequestValidator()
    {
        // Номер заявки: максимум 50 символов
        RuleFor(x => x.Number)
            .MaximumLength(50).WithMessage("Номер заявки не может быть длиннее 50 символов.");

        // Контрагент: максимум 100 символов
        RuleFor(x => x.Counterparty)
            .MaximumLength(100).WithMessage("Контрагент не может быть длиннее 100 символов.");

        // Вид работ: максимум 50 символов
        RuleFor(x => x.WorkType)
            .MaximumLength(50).WithMessage("Вид работ не может быть длиннее 50 символов.");

        // Марка автомобиля: максимум 50 символов
        RuleFor(x => x.VehicleBrand)
            .MaximumLength(50).WithMessage("Марка автомобиля не может быть длиннее 50 символов.");

        // Государственный номер: максимум 20 символов
        RuleFor(x => x.VehiclePlate)
            .MaximumLength(20).WithMessage("Государственный номер не может быть длиннее 20 символов.");

        // Номер прицепа: максимум 20 символов
        RuleFor(x => x.Trailer)
            .MaximumLength(20).WithMessage("Номер прицепа не может быть длиннее 20 символов.");

        // ФИО водителя: максимум 100 символов
        RuleFor(x => x.Driver)
            .MaximumLength(100).WithMessage("ФИО водителя не может быть длиннее 100 символов.");

        // ФИО грузчика: максимум 100 символов
        RuleFor(x => x.Loader)
            .MaximumLength(100).WithMessage("ФИО грузчика не может быть длиннее 100 символов.");

        // ФИО экспедитора: максимум 100 символов
        RuleFor(x => x.Expeditor)
            .MaximumLength(100).WithMessage("ФИО экспедитора не может быть длиннее 100 символов.");

        // Подразделение: максимум 100 символов
        RuleFor(x => x.Department)
            .MaximumLength(100).WithMessage("Подразделение не может быть длиннее 100 символов.");

        // Дата выезда: если указана, не должна быть в будущем
        RuleFor(x => x.ExitDate)
            .Must(exitDate => exitDate == null || exitDate <= DateTime.UtcNow.AddMinutes(1))
            .WithMessage("Дата выезда не может быть в будущем.");
    }
}