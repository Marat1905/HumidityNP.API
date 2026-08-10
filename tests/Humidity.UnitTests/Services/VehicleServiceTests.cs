using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using Humidity.Application.DTOs;
using Humidity.Application.Services;
using Humidity.Domain.Common;
using Humidity.Domain.Entities;
using Humidity.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Humidity.UnitTests.Services;

public class VehicleServiceTests
{
    private readonly Mock<IVehicleRepository> _vehicleRepositoryMock;
    private readonly Mock<IMeasurementRepository> _measurementRepositoryMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<ILogger<VehicleService>> _loggerMock;
    private readonly VehicleService _service;

    public VehicleServiceTests()
    {
        _vehicleRepositoryMock = new Mock<IVehicleRepository>();
        _measurementRepositoryMock = new Mock<IMeasurementRepository>();
        _mapperMock = new Mock<IMapper>();
        _loggerMock = new Mock<ILogger<VehicleService>>();

        _service = new VehicleService(
            _vehicleRepositoryMock.Object,
            _measurementRepositoryMock.Object,
            _mapperMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GetFilteredPagedAsync_ReturnsPagedResultWithMeasurementsCount()
    {
        // Arrange
        var vehicleId = Guid.NewGuid();
        var vehicleEntity = new Vehicle { Id = vehicleId, VehiclePlate = "A123BC" };
        var vehicleDto = new VehicleDto { Id = vehicleId, VehiclePlate = "A123BC", MeasurementsCount = 0 };

        var pagedEntities = new PagedResult<Vehicle>
        {
            Items = new List<Vehicle> { vehicleEntity },
            TotalCount = 1,
            PageNumber = 1,
            PageSize = 20,
            TotalPages = 1
        };

        _vehicleRepositoryMock.Setup(r => r.GetFilteredPagedAsync(1, 20, null, true, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedEntities);

        _mapperMock.Setup(m => m.Map<IEnumerable<VehicleDto>>(It.IsAny<IEnumerable<Vehicle>>()))
            .Returns(new List<VehicleDto> { vehicleDto });

        _measurementRepositoryMock.Setup(r => r.GetCountsByVehicleIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, int> { { vehicleId, 5 } });

        // Act
        var result = await _service.GetFilteredPagedAsync(1, 20, null, true, null, null, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(1);
        result.Items.Should().HaveCount(1);
        result.Items.First().MeasurementsCount.Should().Be(5);
    }

    [Fact]
    public async Task UpdateAsync_WhenExitDateIsBeforeEntryDate_ThrowsInvalidOperationException()
    {
        // Arrange
        var vehicleId = Guid.NewGuid();
        var entryDate = DateTimeOffset.UtcNow.AddDays(-1);
        var exitDate = DateTimeOffset.UtcNow.AddDays(-2); // Раньше въезда

        var existingVehicle = new Vehicle { Id = vehicleId, EntryDate = entryDate };
        var request = new UpdateVehicleRequest { ExitDate = exitDate };

        _vehicleRepositoryMock.Setup(r => r.GetByIdAsync(vehicleId, It.IsAny<CancellationToken>())).ReturnsAsync(existingVehicle);

        // Act
        Func<Task> act = async () => await _service.UpdateAsync(vehicleId, request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Дата выезда*не может быть раньше даты въезда*");
    }

    [Fact]
    public async Task UnloadAsync_WhenVehicleExists_UpdatesFieldsAndReturnsDto()
    {
        // Arrange
        var vehicleId = Guid.NewGuid();
        var vehicle = new Vehicle { Id = vehicleId };
        var request = new UnloadVehicleRequest { BaleCount = 10, DamagedBaleCount = 1, WeightKg = 500.0, StackNumber = "S1" };
        var updatedVehicle = new Vehicle { Id = vehicleId, BaleCount = 10, DamagedBaleCount = 1, WeightKg = 500.0, StackNumber = "S1" };
        var dto = new VehicleDto { Id = vehicleId, BaleCount = 10 };

        _vehicleRepositoryMock.Setup(r => r.GetByIdAsync(vehicleId, It.IsAny<CancellationToken>())).ReturnsAsync(vehicle);
        _vehicleRepositoryMock.Setup(r => r.UpdateAsync(vehicle, It.IsAny<CancellationToken>())).ReturnsAsync(updatedVehicle);
        _mapperMock.Setup(m => m.Map<VehicleDto>(updatedVehicle)).Returns(dto);

        // Act
        var result = await _service.UnloadAsync(vehicleId, request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.BaleCount.Should().Be(10);
        _vehicleRepositoryMock.Verify(r => r.UpdateAsync(vehicle, It.IsAny<CancellationToken>()), Times.Once);
    }
}