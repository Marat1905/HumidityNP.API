using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using FluentValidation;
using Humidity.Application.DTOs;
using Humidity.Application.Services;
using Humidity.Domain.Common;
using Humidity.Domain.Entities;
using Humidity.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Humidity.UnitTests.Services;

public class MeasurementServiceTests
{
    private readonly Mock<IMeasurementRepository> _measurementRepositoryMock;
    private readonly Mock<IVehicleRepository> _vehicleRepositoryMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<ILogger<MeasurementService>> _loggerMock;
    private readonly Mock<IValidator<CreateMeasurementRequest>> _validatorMock;
    private readonly MeasurementService _service;

    public MeasurementServiceTests()
    {
        _measurementRepositoryMock = new Mock<IMeasurementRepository>();
        _vehicleRepositoryMock = new Mock<IVehicleRepository>();
        _mapperMock = new Mock<IMapper>();
        _loggerMock = new Mock<ILogger<MeasurementService>>();
        _validatorMock = new Mock<IValidator<CreateMeasurementRequest>>();

        _service = new MeasurementService(
            _measurementRepositoryMock.Object,
            _vehicleRepositoryMock.Object,
            _mapperMock.Object,
            _loggerMock.Object,
            _validatorMock.Object);
    }

    [Fact]
    public async Task CreateAsync_WhenVehicleExists_ReturnsCreatedMeasurement()
    {
        // Arrange
        var vehicleId = Guid.NewGuid();
        var request = new CreateMeasurementRequest { VehicleId = vehicleId, HumidityValue = 15.0 };
        var entity = new HumidityMeasurement { Id = Guid.NewGuid(), VehicleId = vehicleId, HumidityValue = 15.0 };
        var dto = new MeasurementDto { Id = entity.Id, VehicleId = vehicleId, HumidityValue = 15.0 };

        _vehicleRepositoryMock.Setup(r => r.ExistsAsync(vehicleId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _mapperMock.Setup(m => m.Map<HumidityMeasurement>(request)).Returns(entity);
        _measurementRepositoryMock.Setup(r => r.AddAsync(entity, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mapperMock.Setup(m => m.Map<MeasurementDto>(entity)).Returns(dto);
        _validatorMock.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(new FluentValidation.Results.ValidationResult());

        // Act
        var result = await _service.CreateAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(entity.Id);
        _vehicleRepositoryMock.Verify(r => r.ExistsAsync(vehicleId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenVehicleDoesNotExist_ThrowsKeyNotFoundException()
    {
        // Arrange
        var vehicleId = Guid.NewGuid();
        var request = new CreateMeasurementRequest { VehicleId = vehicleId, HumidityValue = 15.0 };

        _vehicleRepositoryMock.Setup(r => r.ExistsAsync(vehicleId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        // Act
        Func<Task> act = async () => await _service.CreateAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"Машина с id {vehicleId} не найдена");
    }

    [Fact]
    public async Task BulkCreateAsync_WithMixedValidAndInvalidRequests_ReturnsCorrectCounts()
    {
        // Arrange
        var validVehicleId = Guid.NewGuid();
        var invalidVehicleId = Guid.NewGuid();

        var requests = new List<CreateMeasurementRequest>
        {
            new CreateMeasurementRequest { VehicleId = validVehicleId, HumidityValue = 12.0 },
            new CreateMeasurementRequest { VehicleId = invalidVehicleId, HumidityValue = 15.0 }
        };

        _vehicleRepositoryMock.Setup(r => r.GetExistingIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<Guid> { validVehicleId });

        _validatorMock.Setup(v => v.ValidateAsync(It.IsAny<CreateMeasurementRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult()); // Все проходят валидацию

        var createdEntities = new List<HumidityMeasurement> { new HumidityMeasurement { Id = Guid.NewGuid() } };
        _measurementRepositoryMock.Setup(r => r.BulkAddAsync(It.IsAny<List<HumidityMeasurement>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdEntities);

        // Act
        var result = await _service.BulkCreateAsync(requests, CancellationToken.None);

        // Assert
        result.CreatedCount.Should().Be(1);
        result.SkippedCount.Should().Be(1);
        result.Errors.Should().HaveCount(1);
        result.Errors.First().VehicleId.Should().Be(invalidVehicleId);
    }
}