using FluentAssertions;
using FluentValidation.TestHelper;
using Humidity.Application.DTOs;
using Humidity.Application.Validators;
using Xunit;

namespace Humidity.UnitTests.Validators;

public class UnloadVehicleRequestValidatorTests
{
    private readonly UnloadVehicleRequestValidator _validator;

    public UnloadVehicleRequestValidatorTests()
    {
        _validator = new UnloadVehicleRequestValidator();
    }

    [Fact]
    public void UnloadVehicleRequestValidator_WithValidData_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var request = new UnloadVehicleRequest
        {
            BaleCount = 10,
            DamagedBaleCount = 2,
            WeightKg = 500.5,
            StackNumber = "A-100"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void UnloadVehicleRequestValidator_WithNegativeCounts_ShouldHaveValidationErrors()
    {
        // Arrange
        var request = new UnloadVehicleRequest
        {
            BaleCount = -1,
            DamagedBaleCount = -1,
            WeightKg = -10.0,
            StackNumber = "S1"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.BaleCount);
        result.ShouldHaveValidationErrorFor(x => x.DamagedBaleCount);
        result.ShouldHaveValidationErrorFor(x => x.WeightKg);
    }

    [Fact]
    public void UnloadVehicleRequestValidator_WithDamagedBalesButZeroWeight_ShouldHaveValidationError()
    {
        // Arrange
        var request = new UnloadVehicleRequest
        {
            BaleCount = 10,
            DamagedBaleCount = 1, // > 0
            WeightKg = 0.0,       // Должно быть > 0
            StackNumber = "S1"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.WeightKg)
              .WithErrorMessage("При наличии порванных тюков вес должен быть больше 0.");
    }

    [Fact]
    public void UnloadVehicleRequestValidator_WithDamagedBalesGreaterThanTotal_ShouldHaveValidationError()
    {
        // Arrange
        var request = new UnloadVehicleRequest
        {
            BaleCount = 5,
            DamagedBaleCount = 10, // > BaleCount
            WeightKg = 100.0,
            StackNumber = "S1"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
              .WithErrorMessage("Количество порванных тюков не может превышать общее количество тюков.");
    }

    [Fact]
    public void UnloadVehicleRequestValidator_WithEmptyOrLongStackNumber_ShouldHaveValidationErrors()
    {
        // Arrange
        var request = new UnloadVehicleRequest
        {
            BaleCount = 10,
            DamagedBaleCount = 0,
            WeightKg = 100.0,
            StackNumber = "" // Ошибка: пустая строка
        };

        var requestLong = new UnloadVehicleRequest
        {
            BaleCount = 10,
            DamagedBaleCount = 0,
            WeightKg = 100.0,
            StackNumber = new string('A', 60) // Ошибка: > 50 символов
        };

        // Act
        var result1 = _validator.TestValidate(request);
        var result2 = _validator.TestValidate(requestLong);

        // Assert
        result1.ShouldHaveValidationErrorFor(x => x.StackNumber).WithErrorMessage("Номер штабеля обязателен.");
        result2.ShouldHaveValidationErrorFor(x => x.StackNumber).WithErrorMessage("Номер штабеля не должен превышать 50 символов.");
    }
}