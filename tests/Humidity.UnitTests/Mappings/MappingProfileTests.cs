using System;
using System.Collections.Generic;
using System.Globalization;
using AutoMapper;
using FluentAssertions;
using Humidity.Application;
using Humidity.Application.DTOs;
using Humidity.Domain.Entities;
using Humidity.Domain.Enums;
using Humidity.UnitTests.Helpers;
using Xunit;

namespace Humidity.UnitTests.Mappings;

public class MappingProfileTests
{
    private readonly IMapper _mapper;

    public MappingProfileTests()
    {
        _mapper = MapperHelper.CreateMapper();
    }

    #region Vehicle Mapping Tests

    [Fact]
    public void Vehicle_To_VehicleDto_ShouldMapCorrectly()
    {
        // Arrange
        var vehicle = new Vehicle
        {
            Id = Guid.NewGuid(),
            Number = "TEST-001",
            Date = DateTimeOffset.UtcNow,
            EntryDate = DateTimeOffset.UtcNow,
            ExitDate = null,
            Counterparty = "Test Supplier",
            Inn = "7707083893",
            VehicleBrand = "KAMAZ",
            VehiclePlate = "A123BC77",
            Trailer = "T001",
            Driver = "Ivanov I.I.",
            BaleCount = 10,
            DamagedBaleCount = 1,
            WeightKg = 500.5,
            StackNumber = "S-01",
            Measurements = new List<HumidityMeasurement> { new HumidityMeasurement(), new HumidityMeasurement() }
        };

        // Act
        var dto = _mapper.Map<VehicleDto>(vehicle);

        // Assert
        dto.Should().NotBeNull();
        dto.Id.Should().Be(vehicle.Id);
        dto.Number.Should().Be(vehicle.Number);
        dto.Counterparty.Should().Be(vehicle.Counterparty);
        dto.VehiclePlate.Should().Be(vehicle.VehiclePlate);
        dto.Driver.Should().Be(vehicle.Driver);
        dto.BaleCount.Should().Be(vehicle.BaleCount);
        dto.MeasurementsCount.Should().Be(2);
    }

    [Fact]
    public void CreateVehicleRequest_To_Vehicle_ShouldMapCorrectly()
    {
        // Arrange
        var request = new CreateVehicleRequest
        {
            Number = "TEST-002",
            Date = DateTimeOffset.UtcNow,
            EntryDate = DateTimeOffset.UtcNow,
            ExitDate = DateTimeOffset.UtcNow.AddDays(1),
            Counterparty = "New Supplier",
            Inn = "7707083894",
            VehicleBrand = "FAW",
            VehiclePlate = "B456CD77",
            Trailer = "T002",
            Driver = "Petrov P.P."
        };

        // Act
        var vehicle = _mapper.Map<Vehicle>(request);

        // Assert
        vehicle.Should().NotBeNull();
        vehicle.Number.Should().Be(request.Number);
        vehicle.Counterparty.Should().Be(request.Counterparty);
        vehicle.VehiclePlate.Should().Be(request.VehiclePlate);
        vehicle.Driver.Should().Be(request.Driver);
    }

    [Fact]
    public void UpdateVehicleRequest_To_Vehicle_ShouldIgnoreNullValues()
    {
        // Arrange
        var existingVehicle = new Vehicle
        {
            Id = Guid.NewGuid(),
            Number = "OLD-001",
            Counterparty = "Old Supplier",
            VehiclePlate = "A123BC",
            Driver = "Old Driver"
        };

        var request = new UpdateVehicleRequest
        {
            VehiclePlate = "NEW-PLATE",
            Driver = null // Явный null, не должен перезаписать существующее значение
        };

        // Act
        _mapper.Map(request, existingVehicle);

        // Assert
        existingVehicle.VehiclePlate.Should().Be("NEW-PLATE"); // Обновилось
        existingVehicle.Driver.Should().Be("Old Driver");      // Осталось старым, так как в запросе null
        existingVehicle.Number.Should().Be("OLD-001");         // Осталось старым, так как в запросе null
    }

    [Fact]
    public void UnloadVehicleRequest_To_Vehicle_ShouldMapCorrectly()
    {
        // Arrange
        var request = new UnloadVehicleRequest
        {
            BaleCount = 15,
            DamagedBaleCount = 2,
            WeightKg = 750.0,
            StackNumber = "S-99"
        };

        var vehicle = new Vehicle { Id = Guid.NewGuid() };

        // Act
        _mapper.Map(request, vehicle);

        // Assert
        vehicle.BaleCount.Should().Be(15);
        vehicle.DamagedBaleCount.Should().Be(2);
        vehicle.WeightKg.Should().Be(750.0);
        vehicle.StackNumber.Should().Be("S-99");
    }

    #endregion

    #region HumidityMeasurement Mapping Tests

    [Fact]
    public void HumidityMeasurement_To_MeasurementDto_ShouldMapCorrectly_WithNestedVehicleProperties()
    {
        // Arrange
        var measurement = new HumidityMeasurement
        {
            Id = Guid.NewGuid(),
            VehicleId = Guid.NewGuid(),
            HumidityValue = 14.5,
            TemperatureC = 22.0,
            MeasurementType = "TypeA",
            Material = "Wood",
            Source = MeasurementSource.Auto,
            Timestamp = DateTimeOffset.UtcNow,
            Sign = SignType.Less,
            Vehicle = new Vehicle
            {
                Number = "V-NUM-123",
                VehiclePlate = "X999XX77"
            }
        };

        // Act
        var dto = _mapper.Map<MeasurementDto>(measurement);

        // Assert
        dto.Should().NotBeNull();
        dto.Id.Should().Be(measurement.Id);
        dto.VehicleId.Should().Be(measurement.VehicleId);
        dto.HumidityValue.Should().Be(measurement.HumidityValue);
        dto.TemperatureC.Should().Be(measurement.TemperatureC);
        dto.MeasurementType.Should().Be(measurement.MeasurementType);
        dto.Material.Should().Be(measurement.Material);
        dto.Source.Should().Be(measurement.Source);
        dto.Timestamp.Should().Be(measurement.Timestamp);
        dto.Sign.Should().Be(measurement.Sign);

        // Проверка вложенных свойств (Custom Mapping)
        dto.VehicleNumber.Should().Be("V-NUM-123");
        dto.VehiclePlate.Should().Be("X999XX77");

        // Проверка вычисляемого свойства с учетом текущей культуры системы (чтобы избежать ошибок 14.5 vs 14,5)
        string expectedSign = "<";
        string expectedValue = measurement.HumidityValue.ToString("F1", CultureInfo.CurrentCulture);
        string expectedDisplayValue = $"{expectedSign} {expectedValue}%";

        dto.DisplayValue.Should().Be(expectedDisplayValue);
    }

    [Fact]
    public void CreateMeasurementRequest_To_HumidityMeasurement_ShouldMapCorrectly()
    {
        // Arrange
        var request = new CreateMeasurementRequest
        {
            VehicleId = Guid.NewGuid(),
            HumidityValue = 16.0,
            TemperatureC = 20.5,
            MeasurementType = "TypeB",
            Material = "Paper",
            Source = MeasurementSource.Manual,
            Timestamp = DateTimeOffset.UtcNow,
            Sign = SignType.Greater
        };

        // Act
        var measurement = _mapper.Map<HumidityMeasurement>(request);

        // Assert
        measurement.Should().NotBeNull();
        measurement.VehicleId.Should().Be(request.VehicleId);
        measurement.HumidityValue.Should().Be(request.HumidityValue);
        measurement.TemperatureC.Should().Be(request.TemperatureC);
        measurement.MeasurementType.Should().Be(request.MeasurementType);
        measurement.Material.Should().Be(request.Material);
        measurement.Source.Should().Be(request.Source);
        measurement.Timestamp.Should().Be(request.Timestamp);
        measurement.Sign.Should().Be(request.Sign);
    }

    #endregion
}