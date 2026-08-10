using FluentAssertions;
using FluentValidation;
using Humidity.Application.DTOs;
using Humidity.Application.Interfaces;
using Humidity.Application.Services;
using Humidity.Domain.Entities;
using Humidity.Domain.Enums;
using Humidity.Domain.Interfaces;
using Humidity.Infrastructure.Data;
using Humidity.Infrastructure.Repositories;
using Humidity.IntegrationTests.Fixtures;
using Humidity.IntegrationTests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using HumidityMeasurement = Humidity.Domain.Entities.HumidityMeasurement;

namespace Humidity.IntegrationTests.Services;

public class MeasurementServiceIntegrationTests : IClassFixture<TestContainersFixture>, IAsyncLifetime
{
    private readonly IServiceProvider _serviceProvider;
    private readonly HumidityDbContext _dbContext;
    private readonly IMeasurementService _measurementService;

    public MeasurementServiceIntegrationTests(TestContainersFixture fixture)
    {
        var services = new ServiceCollection();

        services.AddLogging();

        services.AddDbContext<HumidityDbContext>(options =>
            options.UseNpgsql(fixture.ConnectionString));

        services.AddScoped<IMeasurementRepository, MeasurementRepository>();
        services.AddScoped<IVehicleRepository, VehicleRepository>();

        services.AddScoped(sp => MapperHelper.CreateMapper());

        // НОВОЕ: Регистрируем ВСЕ валидаторы FluentValidation из сборки Application
        // Это автоматически найдёт CreateMeasurementRequestValidator, 
        // UpdateMeasurementRequestValidator и любые другие.
        services.AddValidatorsFromAssembly(typeof(CreateMeasurementRequest).Assembly);

        services.AddScoped<IMeasurementService, MeasurementService>();

        _serviceProvider = services.BuildServiceProvider();
        _dbContext = _serviceProvider.GetRequiredService<HumidityDbContext>();
        _measurementService = _serviceProvider.GetRequiredService<IMeasurementService>();
    }

    public async Task InitializeAsync()
    {
        await _dbContext.Database.EnsureCreatedAsync();
        await DatabaseCleaner.CleanDatabaseAsync(_dbContext);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CreateAsync_WithValidRequest_ShouldPersistAndLinkToVehicle()
    {
        // Arrange
        var vehicle = new Vehicle
        {
            Id = Guid.NewGuid(),
            Number = "MEAS-VEH-001",
            Date = DateTimeOffset.UtcNow,
            EntryDate = DateTimeOffset.UtcNow,
            Counterparty = "Test Supplier",
            VehiclePlate = "E001EE77",
            Driver = "Driver E",
            ExitDate = null
        };
        _dbContext.Vehicles.Add(vehicle);
        await _dbContext.SaveChangesAsync();

        var request = new CreateMeasurementRequest
        {
            VehicleId = vehicle.Id,
            HumidityValue = 14.5,
            TemperatureC = 22.0,
            MeasurementType = "Manual",
            Material = "Grain",
            Source = MeasurementSource.Manual,
            Timestamp = DateTimeOffset.UtcNow,
            Sign = SignType.None
        };

        // Act
        var result = await _measurementService.CreateAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.HumidityValue.Should().Be(14.5);
        result.VehicleId.Should().Be(vehicle.Id);

        var dbMeasurement = await _dbContext.Measurements.FindAsync(result.Id);
        dbMeasurement.Should().NotBeNull();
        dbMeasurement!.TemperatureC.Should().Be(22.0);
    }

    [Fact]
    public async Task BulkCreateAsync_ShouldCreateValidAndSkipInvalidMeasurements()
    {
        // Arrange
        var validVehicleId = Guid.NewGuid();
        var invalidVehicleId = Guid.NewGuid();

        _dbContext.Vehicles.Add(new Vehicle
        {
            Id = validVehicleId,
            Number = "BULK-001",
            Date = DateTimeOffset.UtcNow,
            EntryDate = DateTimeOffset.UtcNow,
            Counterparty = "S",
            VehiclePlate = "P",
            Driver = "D",
            ExitDate = null
        });
        await _dbContext.SaveChangesAsync();

        var requests = new List<CreateMeasurementRequest>
        {
            new()
            {
                VehicleId = validVehicleId,
                HumidityValue = 12.0,
                TemperatureC = 20.0,
                Source = MeasurementSource.Auto,
                Timestamp = DateTimeOffset.UtcNow,
                Sign = SignType.None
            },
            new()
            {
                VehicleId = invalidVehicleId,
                HumidityValue = 13.0,
                TemperatureC = 20.0,
                Source = MeasurementSource.Auto,
                Timestamp = DateTimeOffset.UtcNow,
                Sign = SignType.None
            }
        };

        // Act
        var result = await _measurementService.BulkCreateAsync(requests);

        // Assert
        result.CreatedCount.Should().Be(1);
        result.SkippedCount.Should().Be(1);
        result.Errors.Should().ContainSingle();
        result.Errors.First().VehicleId.Should().Be(invalidVehicleId);

        var dbCount = await _dbContext.Measurements.CountAsync();
        dbCount.Should().Be(1);
    }

    [Fact]
    public async Task GetStatisticsByVehicleIdAsync_ShouldReturnStatisticsForVehicle()
    {
        // Arrange
        var vehicleId = Guid.NewGuid();
        _dbContext.Vehicles.Add(new Vehicle
        {
            Id = vehicleId,
            Number = "STAT-001",
            Date = DateTimeOffset.UtcNow,
            EntryDate = DateTimeOffset.UtcNow,
            Counterparty = "S",
            VehiclePlate = "P",
            Driver = "D",
            ExitDate = null
        });
        await _dbContext.SaveChangesAsync();

        var measurements = new[]
        {
            new HumidityMeasurement
            {
                Id = Guid.NewGuid(),
                VehicleId = vehicleId,
                HumidityValue = 10.0,
                TemperatureC = 20.0,
                Source = MeasurementSource.Manual,
                Timestamp = DateTimeOffset.UtcNow.AddHours(-2),
                Sign = SignType.None
            },
            new HumidityMeasurement
            {
                Id = Guid.NewGuid(),
                VehicleId = vehicleId,
                HumidityValue = 14.0,
                TemperatureC = 21.0,
                Source = MeasurementSource.Manual,
                Timestamp = DateTimeOffset.UtcNow.AddHours(-1),
                Sign = SignType.None
            },
            new HumidityMeasurement
            {
                Id = Guid.NewGuid(),
                VehicleId = vehicleId,
                HumidityValue = 12.0,
                TemperatureC = 22.0,
                Source = MeasurementSource.Manual,
                Timestamp = DateTimeOffset.UtcNow,
                Sign = SignType.None
            }
        };

        _dbContext.Measurements.AddRange(measurements);
        await _dbContext.SaveChangesAsync();

        // Act
        var stats = await _measurementService.GetStatisticsByVehicleIdAsync(vehicleId);

        // Assert
        stats.Should().NotBeNull();
    }

    [Fact]
    public async Task GetLatestByVehicleIdAsync_ShouldReturnMostRecentMeasurement()
    {
        // Arrange
        var vehicleId = Guid.NewGuid();
        _dbContext.Vehicles.Add(new Vehicle
        {
            Id = vehicleId,
            Number = "LATEST-001",
            Date = DateTimeOffset.UtcNow,
            EntryDate = DateTimeOffset.UtcNow,
            Counterparty = "S",
            VehiclePlate = "P",
            Driver = "D",
            ExitDate = null
        });
        await _dbContext.SaveChangesAsync();

        var oldMeasurement = new HumidityMeasurement
        {
            Id = Guid.NewGuid(),
            VehicleId = vehicleId,
            HumidityValue = 10.0,
            TemperatureC = 20.0,
            Source = MeasurementSource.Manual,
            Timestamp = DateTimeOffset.UtcNow.AddHours(-2),
            Sign = SignType.None
        };

        var newMeasurement = new HumidityMeasurement
        {
            Id = Guid.NewGuid(),
            VehicleId = vehicleId,
            HumidityValue = 15.0,
            TemperatureC = 21.0,
            Source = MeasurementSource.Manual,
            Timestamp = DateTimeOffset.UtcNow,
            Sign = SignType.None
        };

        _dbContext.Measurements.AddRange(oldMeasurement, newMeasurement);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _measurementService.GetLatestByVehicleIdAsync(vehicleId);

        // Assert
        result.Should().NotBeNull();
        result!.HumidityValue.Should().Be(15.0);
    }

    [Fact]
    public async Task GetByVehicleIdPagedAsync_ShouldReturnPagedResult()
    {
        // Arrange
        var vehicleId = Guid.NewGuid();
        _dbContext.Vehicles.Add(new Vehicle
        {
            Id = vehicleId,
            Number = "PAGED-001",
            Date = DateTimeOffset.UtcNow,
            EntryDate = DateTimeOffset.UtcNow,
            Counterparty = "S",
            VehiclePlate = "P",
            Driver = "D",
            ExitDate = null
        });
        await _dbContext.SaveChangesAsync();

        var measurements = Enumerable.Range(1, 25).Select(i => new HumidityMeasurement
        {
            Id = Guid.NewGuid(),
            VehicleId = vehicleId,
            HumidityValue = 10.0 + i * 0.1,
            TemperatureC = 20.0,
            Source = MeasurementSource.Manual,
            Timestamp = DateTimeOffset.UtcNow.AddMinutes(-i),
            Sign = SignType.None
        }).ToList();

        _dbContext.Measurements.AddRange(measurements);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _measurementService.GetByVehicleIdPagedAsync(vehicleId, pageNumber: 1, pageSize: 10);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(25);
        result.Items.Should().HaveCount(10);
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(10);
    }
}