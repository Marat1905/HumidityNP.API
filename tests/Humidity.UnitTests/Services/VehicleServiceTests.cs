using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using Humidity.Application.Common.Models;
using Humidity.Application.DTOs;
using Humidity.Application.Services;
using Humidity.Domain.Common;
using Humidity.Domain.Entities;
using Humidity.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Humidity.UnitTests.Services;

public class VehicleServiceTests
{
    private readonly Mock<IVehicleRepository> _vehicleRepositoryMock;
    private readonly Mock<IMeasurementRepository> _measurementRepositoryMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<ILogger<VehicleService>> _loggerMock;
    private readonly IOptions<OneCIntegrationSettings> _oneCSettings;
    private readonly VehicleService _service;

    public VehicleServiceTests()
    {
        _vehicleRepositoryMock = new Mock<IVehicleRepository>();
        _measurementRepositoryMock = new Mock<IMeasurementRepository>();
        _mapperMock = new Mock<IMapper>();
        _loggerMock = new Mock<ILogger<VehicleService>>();

        // Настройки интеграции с 1С нужны конструктору VehicleService
        // для инициализации часового пояса. Для юнит-тестов достаточно
        // корректного TimeZoneId: берём UTC, чтобы не зависеть от ОС,
        // на которой запускаются тесты (Windows/Linux).
        _oneCSettings = Options.Create(new OneCIntegrationSettings
        {
            TimeZoneId = "UTC"
        });

        _service = new VehicleService(
            _vehicleRepositoryMock.Object,
            _measurementRepositoryMock.Object,
            _mapperMock.Object,
            _loggerMock.Object,
            _oneCSettings);
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

        // Метод репозитория теперь принимает ещё entryDateFrom и entryDateTo (оба null в этом тесте).
        _vehicleRepositoryMock
            .Setup(r => r.GetFilteredPagedAsync(
                1, 20, null, true, null, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedEntities);

        _mapperMock.Setup(m => m.Map<IEnumerable<VehicleDto>>(It.IsAny<IEnumerable<Vehicle>>()))
            .Returns(new List<VehicleDto> { vehicleDto });

        _measurementRepositoryMock.Setup(r => r.GetCountsByVehicleIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, int> { { vehicleId, 5 } });

        // Act
        var result = await _service.GetFilteredPagedAsync(
            1, 20, null, true, null, null, cancellationToken: CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(1);
        result.Items.Should().HaveCount(1);
        result.Items.First().MeasurementsCount.Should().Be(5);
    }

    [Fact]
    public async Task GetFilteredPagedAsync_WithEntryDateRange_PassesRangeToRepository()
    {
        // Arrange
        var vehicleId = Guid.NewGuid();
        var vehicleEntity = new Vehicle { Id = vehicleId, VehiclePlate = "A123BC" };
        var vehicleDto = new VehicleDto { Id = vehicleId, VehiclePlate = "A123BC", MeasurementsCount = 0 };

        var from = DateTimeOffset.UtcNow.AddYears(-1);
        var to = DateTimeOffset.UtcNow;

        var pagedEntities = new PagedResult<Vehicle>
        {
            Items = new List<Vehicle> { vehicleEntity },
            TotalCount = 1,
            PageNumber = 1,
            PageSize = 20,
            TotalPages = 1
        };

        // Проверяем, что сервис пробрасывает диапазон дат в репозиторий без изменений.
        _vehicleRepositoryMock
            .Setup(r => r.GetFilteredPagedAsync(
                1, 20, null, null, null, null, from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedEntities);

        _mapperMock.Setup(m => m.Map<IEnumerable<VehicleDto>>(It.IsAny<IEnumerable<Vehicle>>()))
            .Returns(new List<VehicleDto> { vehicleDto });

        _measurementRepositoryMock
            .Setup(r => r.GetCountsByVehicleIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, int>());

        // Act
        var result = await _service.GetFilteredPagedAsync(
            1, 20, null, null, null, null, from, to, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();

        _vehicleRepositoryMock.Verify(
            r => r.GetFilteredPagedAsync(
                1, 20, null, null, null, null, from, to, It.IsAny<CancellationToken>()),
            Times.Once);
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