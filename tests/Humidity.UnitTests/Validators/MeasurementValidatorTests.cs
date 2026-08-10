using System;
using FluentAssertions;
using FluentValidation.TestHelper;
using Humidity.Application.DTOs;
using Humidity.Application.Validators;
using Humidity.Domain.Enums;
using Xunit;

namespace Humidity.UnitTests.Validators;

public class MeasurementValidatorTests
{
    private readonly CreateMeasurementRequestValidator _createValidator;
    private readonly UpdateMeasurementRequestValidator _updateValidator;

    public MeasurementValidatorTests()
    {
        _createValidator = new CreateMeasurementRequestValidator();
        _updateValidator = new UpdateMeasurementRequestValidator();
    }

    [Fact]
    public void CreateMeasurementRequestValidator_WithValidData_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var request = new CreateMeasurementRequest
        {
            VehicleId = Guid.NewGuid(),
            HumidityValue = 15.5,
            TemperatureC = 22.0,
            MeasurementType = "Type A",
            Material = "Wood",
            Source = MeasurementSource.Manual,
            Timestamp = DateTimeOffset.UtcNow,
            Sign = SignType.None
        };

        // Act
        var result = _createValidator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CreateMeasurementRequestValidator_WithInvalidData_ShouldHaveValidationErrors()
    {
        // Arrange
        var request = new CreateMeasurementRequest
        {
            VehicleId = Guid.Empty, // Ошибка: пустой Guid
            HumidityValue = 150.0,  // Ошибка: > 100
            TemperatureC = -100.0,  // Ошибка: < -50
            MeasurementType = new string('a', 60), // Ошибка: > 50 символов
            Material = new string('b', 150),       // Ошибка: > 100 символов
            Source = (MeasurementSource)999,       // Ошибка: не в enum
            Timestamp = DateTimeOffset.UtcNow.AddDays(1), // Ошибка: в будущем
            Sign = (SignType)999                   // Ошибка: не в enum
        };

        // Act
        var result = _createValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.VehicleId);
        result.ShouldHaveValidationErrorFor(x => x.HumidityValue);
        result.ShouldHaveValidationErrorFor(x => x.TemperatureC);
        result.ShouldHaveValidationErrorFor(x => x.MeasurementType);
        result.ShouldHaveValidationErrorFor(x => x.Material);
        result.ShouldHaveValidationErrorFor(x => x.Source);
        result.ShouldHaveValidationErrorFor(x => x.Timestamp);
        result.ShouldHaveValidationErrorFor(x => x.Sign);
    }

    [Fact]
    public void UpdateMeasurementRequestValidator_WithValidData_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var request = new UpdateMeasurementRequest
        {
            HumidityValue = 50.0,
            TemperatureC = 0.0,
            MeasurementType = "Type B",
            Material = "Metal",
            Source = MeasurementSource.Auto,
            Sign = SignType.Less,
            Timestamp = DateTimeOffset.UtcNow
        };

        // Act
        var result = _updateValidator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void UpdateMeasurementRequestValidator_WithFutureTimestamp_ShouldHaveValidationError()
    {
        // Arrange
        var request = new UpdateMeasurementRequest
        {
            Timestamp = DateTimeOffset.UtcNow.AddDays(1)
        };

        // Act
        var result = _updateValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Timestamp);
    }

    [Fact]
    public void UpdateMeasurementRequestValidator_WithNullValues_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var request = new UpdateMeasurementRequest(); // Все поля null

        // Act
        var result = _updateValidator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}