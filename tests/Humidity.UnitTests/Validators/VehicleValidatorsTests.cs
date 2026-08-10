using System;
using FluentAssertions;
using FluentValidation.TestHelper;
using Humidity.Application.DTOs;
using Humidity.Application.Validators;
using Xunit;

namespace Humidity.UnitTests.Validators;

public class VehicleValidatorsTests
{
    private readonly CreateVehicleRequestValidator _createValidator;
    private readonly UpdateVehicleRequestValidator _updateValidator;

    public VehicleValidatorsTests()
    {
        _createValidator = new CreateVehicleRequestValidator();
        _updateValidator = new UpdateVehicleRequestValidator();
    }

    [Fact]
    public void CreateVehicleRequestValidator_WithValidData_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var request = new CreateVehicleRequest
        {
            Number = "P-12345",
            Date = DateTimeOffset.UtcNow,
            EntryDate = DateTimeOffset.UtcNow,
            ExitDate = null,
            Counterparty = "ООО Ромашка",
            Inn = "7707083893", // 10 цифр
            VehicleBrand = "KAMAZ",
            VehiclePlate = "А123БВ777",
            Trailer = "Т123АВ777",
            Driver = "Иванов Иван Иванович"
        };

        // Act
        var result = _createValidator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CreateVehicleRequestValidator_WithInvalidInn_ShouldHaveValidationErrors()
    {
        // Arrange
        var requestShort = new CreateVehicleRequest
        {
            Number = "P-1",
            Date = DateTimeOffset.UtcNow,
            EntryDate = DateTimeOffset.UtcNow,
            Counterparty = "Test",
            VehicleBrand = "KAMAZ",
            VehiclePlate = "A1",
            Driver = "Ivan",
            Inn = "12345" // Ошибка: не 10 и не 12 цифр
        };

        var requestLetters = new CreateVehicleRequest
        {
            Number = "P-1",
            Date = DateTimeOffset.UtcNow,
            EntryDate = DateTimeOffset.UtcNow,
            Counterparty = "Test",
            VehicleBrand = "KAMAZ",
            VehiclePlate = "A1",
            Driver = "Ivan",
            Inn = "770708389A" // Ошибка: содержит буквы
        };

        // Act
        var result1 = _createValidator.TestValidate(requestShort);
        var result2 = _createValidator.TestValidate(requestLetters);

        // Assert
        result1.ShouldHaveValidationErrorFor(x => x.Inn).WithErrorMessage("ИНН должен содержать 10 или 12 цифр.");
        result2.ShouldHaveValidationErrorFor(x => x.Inn).WithErrorMessage("ИНН должен состоять только из цифр.");
    }

    [Fact]
    public void CreateVehicleRequestValidator_WithExitDateBeforeEntryDate_ShouldHaveValidationError()
    {
        // Arrange
        var request = new CreateVehicleRequest
        {
            Number = "P-1",
            Date = DateTimeOffset.UtcNow,
            EntryDate = DateTimeOffset.UtcNow.AddDays(1),
            ExitDate = DateTimeOffset.UtcNow, // Раньше EntryDate
            Counterparty = "Test",
            VehicleBrand = "KAMAZ",
            VehiclePlate = "A1",
            Driver = "Ivan"
        };

        // Act
        var result = _createValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ExitDate)
              .WithErrorMessage("Дата выезда не может быть раньше даты въезда.");
    }

    [Fact]
    public void CreateVehicleRequestValidator_WithMissingRequiredFields_ShouldHaveValidationErrors()
    {
        // Arrange
        var request = new CreateVehicleRequest
        {
            Number = "",
            Counterparty = "",
            VehicleBrand = "",
            VehiclePlate = "",
            Driver = ""
            // Date и EntryDate по умолчанию имеют значение DateTimeOffset.MinValue, что тоже вызовет ошибку "в будущем" или "обязательно"
        };

        // Act
        var result = _createValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Number);
        result.ShouldHaveValidationErrorFor(x => x.Counterparty);
        result.ShouldHaveValidationErrorFor(x => x.VehicleBrand);
        result.ShouldHaveValidationErrorFor(x => x.VehiclePlate);
        result.ShouldHaveValidationErrorFor(x => x.Driver);
    }

    [Fact]
    public void UpdateVehicleRequestValidator_WithValidOptionalData_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var request = new UpdateVehicleRequest
        {
            Number = "P-999",
            Counterparty = "Новое ООО",
            Inn = "770708389301", // 12 цифр
            VehicleBrand = "MAZ",
            VehiclePlate = "В456ГД77",
            Trailer = "Т999АВ77",
            Driver = "Петров Петр Петрович",
            ExitDate = DateTimeOffset.UtcNow
        };

        // Act
        var result = _updateValidator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void UpdateVehicleRequestValidator_WithFutureExitDate_ShouldHaveValidationError()
    {
        // Arrange
        var request = new UpdateVehicleRequest
        {
            ExitDate = DateTimeOffset.UtcNow.AddDays(1) // В будущем
        };

        // Act
        var result = _updateValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ExitDate)
              .WithErrorMessage("Дата выезда не может быть в будущем.");
    }

    [Fact]
    public void UpdateVehicleRequestValidator_WithExceededLengths_ShouldHaveValidationErrors()
    {
        // Arrange
        var request = new UpdateVehicleRequest
        {
            Number = new string('A', 60),
            Counterparty = new string('B', 150),
            VehicleBrand = new string('C', 60),
            VehiclePlate = new string('D', 30),
            Trailer = new string('E', 30),
            Driver = new string('F', 150)
        };

        // Act
        var result = _updateValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Number);
        result.ShouldHaveValidationErrorFor(x => x.Counterparty);
        result.ShouldHaveValidationErrorFor(x => x.VehicleBrand);
        result.ShouldHaveValidationErrorFor(x => x.VehiclePlate);
        result.ShouldHaveValidationErrorFor(x => x.Trailer);
        result.ShouldHaveValidationErrorFor(x => x.Driver);
    }
}