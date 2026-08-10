using FluentAssertions;
using Humidity.Domain.Entities;
using Humidity.Domain.Enums;
using Humidity.Infrastructure.Data;
using Humidity.IntegrationTests.Fixtures;
using Humidity.IntegrationTests.Helpers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
// Используем alias, чтобы избежать конфликта с System.Diagnostics.Metrics.Measurement
using HumidityMeasurement = Humidity.Domain.Entities.HumidityMeasurement;

namespace Humidity.IntegrationTests.Repositories;

public class MeasurementRepositoryTests : IClassFixture<TestContainersFixture>, IAsyncLifetime
{
    private readonly HumidityDbContext _dbContext;

    public MeasurementRepositoryTests(TestContainersFixture fixture)
    {
        var options = new DbContextOptionsBuilder<HumidityDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options;

        _dbContext = new HumidityDbContext(options);
    }

    // IAsyncLifetime: выполняется перед каждым тестом в классе
    public async Task InitializeAsync()
    {
        await _dbContext.Database.EnsureCreatedAsync();
        await DatabaseCleaner.CleanDatabaseAsync(_dbContext);
    }

    // IAsyncLifetime: выполняется после всех тестов в классе
    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task AddAsync_WithValidMeasurement_ShouldPersistToDatabase()
    {
        // Arrange
        var vehicleId = Guid.NewGuid();
        var vehicle = new Vehicle
        {
            Id = vehicleId,
            Number = "MEAS-VEH-001",
            Date = DateTimeOffset.UtcNow,
            EntryDate = DateTimeOffset.UtcNow,
            Counterparty = "Measurement Supplier",
            VehiclePlate = "J001JJ77",
            Driver = "Measurement Driver",
            ExitDate = null
        };

        _dbContext.Vehicles.Add(vehicle);
        await _dbContext.SaveChangesAsync();

        var measurement = new HumidityMeasurement
        {
            Id = Guid.NewGuid(),
            VehicleId = vehicleId,
            HumidityValue = 12.5,
            TemperatureC = 22.3,
            MeasurementType = "Manual",
            Material = "Grain",
            Source = MeasurementSource.Manual,
            Timestamp = DateTimeOffset.UtcNow,
            Sign = SignType.None
        };

        // Act
        _dbContext.Measurements.Add(measurement);
        await _dbContext.SaveChangesAsync();

        // Assert
        var savedMeasurement = await _dbContext.Measurements.FindAsync(measurement.Id);
        savedMeasurement.Should().NotBeNull();
        savedMeasurement!.HumidityValue.Should().Be(12.5);
        savedMeasurement.VehicleId.Should().Be(vehicleId);
        savedMeasurement.Source.Should().Be(MeasurementSource.Manual);
    }

    [Fact]
    public async Task GetByIdAsync_WhenMeasurementExists_ShouldReturnMeasurement()
    {
        // Arrange
        var vehicleId = Guid.NewGuid();
        var vehicle = new Vehicle
        {
            Id = vehicleId,
            Number = "MEAS-VEH-002",
            Date = DateTimeOffset.UtcNow,
            EntryDate = DateTimeOffset.UtcNow,
            Counterparty = "Supplier",
            VehiclePlate = "K002KK77",
            Driver = "Driver",
            ExitDate = null
        };

        _dbContext.Vehicles.Add(vehicle);
        await _dbContext.SaveChangesAsync();

        var measurementId = Guid.NewGuid();
        var measurement = new HumidityMeasurement
        {
            Id = measurementId,
            VehicleId = vehicleId,
            HumidityValue = 15.0,
            TemperatureC = 20.0,
            Source = MeasurementSource.Auto,
            Timestamp = DateTimeOffset.UtcNow,
            Sign = SignType.Less
        };

        _dbContext.Measurements.Add(measurement);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _dbContext.Measurements.FindAsync(measurementId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(measurementId);
        result.HumidityValue.Should().Be(15.0);
        result.Sign.Should().Be(SignType.Less);
    }

    [Fact]
    public async Task GetByVehicleIdAsync_ShouldReturnAllMeasurementsForVehicle()
    {
        // Arrange
        var vehicleId = Guid.NewGuid();
        var vehicle = new Vehicle
        {
            Id = vehicleId,
            Number = "MEAS-VEH-003",
            Date = DateTimeOffset.UtcNow,
            EntryDate = DateTimeOffset.UtcNow,
            Counterparty = "Supplier",
            VehiclePlate = "L003LL77",
            Driver = "Driver",
            ExitDate = null
        };

        _dbContext.Vehicles.Add(vehicle);
        await _dbContext.SaveChangesAsync();

        var measurements = Enumerable.Range(1, 5).Select(i => new HumidityMeasurement
        {
            Id = Guid.NewGuid(),
            VehicleId = vehicleId,
            HumidityValue = 10.0 + i,
            TemperatureC = 20.0,
            Source = MeasurementSource.Manual,
            Timestamp = DateTimeOffset.UtcNow.AddMinutes(-i),
            Sign = SignType.None
        }).ToList();

        _dbContext.Measurements.AddRange(measurements);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _dbContext.Measurements
            .Where(m => m.VehicleId == vehicleId)
            .OrderByDescending(m => m.Timestamp)
            .ToListAsync();

        // Assert
        result.Should().HaveCount(5);
        result.First().HumidityValue.Should().Be(11.0);
    }

    [Fact]
    public async Task GetLatestByVehicleIdAsync_ShouldReturnMostRecentMeasurement()
    {
        // Arrange
        var vehicleId = Guid.NewGuid();
        var vehicle = new Vehicle
        {
            Id = vehicleId,
            Number = "MEAS-VEH-004",
            Date = DateTimeOffset.UtcNow,
            EntryDate = DateTimeOffset.UtcNow,
            Counterparty = "Supplier",
            VehiclePlate = "M004MM77",
            Driver = "Driver",
            ExitDate = null
        };

        _dbContext.Vehicles.Add(vehicle);
        await _dbContext.SaveChangesAsync();

        var oldMeasurement = new HumidityMeasurement
        {
            Id = Guid.NewGuid(),
            VehicleId = vehicleId,
            HumidityValue = 12.0,
            TemperatureC = 20.0,
            Source = MeasurementSource.Manual,
            Timestamp = DateTimeOffset.UtcNow.AddHours(-2),
            Sign = SignType.None
        };

        var newMeasurement = new HumidityMeasurement
        {
            Id = Guid.NewGuid(),
            VehicleId = vehicleId,
            HumidityValue = 14.0,
            TemperatureC = 21.0,
            Source = MeasurementSource.Auto,
            Timestamp = DateTimeOffset.UtcNow,
            Sign = SignType.Greater
        };

        _dbContext.Measurements.AddRange(oldMeasurement, newMeasurement);
        await _dbContext.SaveChangesAsync();

        // Act
        var latest = await _dbContext.Measurements
            .Where(m => m.VehicleId == vehicleId)
            .OrderByDescending(m => m.Timestamp)
            .FirstOrDefaultAsync();

        // Assert
        latest.Should().NotBeNull();
        latest!.Id.Should().Be(newMeasurement.Id);
        latest.HumidityValue.Should().Be(14.0);
    }

    [Fact]
    public async Task GetByDateAsync_ShouldReturnMeasurementsForSpecificDate()
    {
        // Arrange
        var vehicleId = Guid.NewGuid();
        var vehicle = new Vehicle
        {
            Id = vehicleId,
            Number = "MEAS-VEH-005",
            Date = DateTimeOffset.UtcNow,
            EntryDate = DateTimeOffset.UtcNow,
            Counterparty = "Supplier",
            VehiclePlate = "N005NN77",
            Driver = "Driver",
            ExitDate = null
        };

        _dbContext.Vehicles.Add(vehicle);
        await _dbContext.SaveChangesAsync();

        // ИСПРАВЛЕНО: используем диапазон дат вместо .Date, чтобы избежать проблем с типами PostgreSQL
        var todayUtc = DateTime.UtcNow.Date;
        var startOfDay = new DateTimeOffset(todayUtc, TimeSpan.Zero);
        var endOfDay = startOfDay.AddDays(1);

        var todayMeasurement = new HumidityMeasurement
        {
            Id = Guid.NewGuid(),
            VehicleId = vehicleId,
            HumidityValue = 13.0,
            TemperatureC = 20.0,
            Source = MeasurementSource.Manual,
            Timestamp = startOfDay.AddHours(10),
            Sign = SignType.None
        };

        var yesterdayMeasurement = new HumidityMeasurement
        {
            Id = Guid.NewGuid(),
            VehicleId = vehicleId,
            HumidityValue = 12.0,
            TemperatureC = 19.0,
            Source = MeasurementSource.Manual,
            Timestamp = startOfDay.AddDays(-1).AddHours(10),
            Sign = SignType.None
        };

        var tomorrowMeasurement = new HumidityMeasurement
        {
            Id = Guid.NewGuid(),
            VehicleId = vehicleId,
            HumidityValue = 14.0,
            TemperatureC = 21.0,
            Source = MeasurementSource.Manual,
            Timestamp = endOfDay.AddHours(2),
            Sign = SignType.None
        };

        _dbContext.Measurements.AddRange(todayMeasurement, yesterdayMeasurement, tomorrowMeasurement);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _dbContext.Measurements
            .Where(m => m.Timestamp >= startOfDay && m.Timestamp < endOfDay)
            .ToListAsync();

        // Assert
        result.Should().ContainSingle();
        result.First().Id.Should().Be(todayMeasurement.Id);
    }

    [Fact]
    public async Task GetByDateRangeAsync_ShouldReturnMeasurementsInRange()
    {
        // Arrange
        var vehicleId = Guid.NewGuid();
        var vehicle = new Vehicle
        {
            Id = vehicleId,
            Number = "MEAS-VEH-006",
            Date = DateTimeOffset.UtcNow,
            EntryDate = DateTimeOffset.UtcNow,
            Counterparty = "Supplier",
            VehiclePlate = "O006OO77",
            Driver = "Driver",
            ExitDate = null
        };

        _dbContext.Vehicles.Add(vehicle);
        await _dbContext.SaveChangesAsync();

        var now = DateTimeOffset.UtcNow;

        var measurements = new[]
        {
            new HumidityMeasurement
            {
                Id = Guid.NewGuid(),
                VehicleId = vehicleId,
                HumidityValue = 11.0,
                TemperatureC = 20.0,
                Source = MeasurementSource.Manual,
                Timestamp = now.AddDays(-5),
                Sign = SignType.None
            },
            new HumidityMeasurement
            {
                Id = Guid.NewGuid(),
                VehicleId = vehicleId,
                HumidityValue = 12.0,
                TemperatureC = 20.0,
                Source = MeasurementSource.Manual,
                Timestamp = now.AddDays(-3),
                Sign = SignType.None
            },
            new HumidityMeasurement
            {
                Id = Guid.NewGuid(),
                VehicleId = vehicleId,
                HumidityValue = 13.0,
                TemperatureC = 20.0,
                Source = MeasurementSource.Manual,
                Timestamp = now.AddDays(-1),
                Sign = SignType.None
            }
        };

        _dbContext.Measurements.AddRange(measurements);
        await _dbContext.SaveChangesAsync();

        // Act
        var from = now.AddDays(-4);
        var to = now.AddDays(-2);

        var result = await _dbContext.Measurements
            .Where(m => m.Timestamp >= from && m.Timestamp <= to)
            .ToListAsync();

        // Assert
        result.Should().ContainSingle();
        result.First().HumidityValue.Should().Be(12.0);
    }

    [Fact]
    public async Task UpdateAsync_WithModifiedMeasurement_ShouldPersistChanges()
    {
        // Arrange
        var vehicleId = Guid.NewGuid();
        var vehicle = new Vehicle
        {
            Id = vehicleId,
            Number = "MEAS-VEH-007",
            Date = DateTimeOffset.UtcNow,
            EntryDate = DateTimeOffset.UtcNow,
            Counterparty = "Supplier",
            VehiclePlate = "P007PP77",
            Driver = "Driver",
            ExitDate = null
        };

        _dbContext.Vehicles.Add(vehicle);
        await _dbContext.SaveChangesAsync();

        var measurement = new HumidityMeasurement
        {
            Id = Guid.NewGuid(),
            VehicleId = vehicleId,
            HumidityValue = 10.0,
            TemperatureC = 20.0,
            MeasurementType = "Manual",
            Source = MeasurementSource.Manual,
            Timestamp = DateTimeOffset.UtcNow,
            Sign = SignType.None
        };

        _dbContext.Measurements.Add(measurement);
        await _dbContext.SaveChangesAsync();

        // Act
        measurement.HumidityValue = 15.5;
        measurement.TemperatureC = 22.0;
        measurement.MeasurementType = "Auto";
        measurement.Sign = SignType.Greater;
        await _dbContext.SaveChangesAsync();

        // Assert
        var updated = await _dbContext.Measurements.FindAsync(measurement.Id);
        updated.Should().NotBeNull();
        updated!.HumidityValue.Should().Be(15.5);
        updated.TemperatureC.Should().Be(22.0);
        updated.MeasurementType.Should().Be("Auto");
        updated.Sign.Should().Be(SignType.Greater);
    }

    [Fact]
    public async Task DeleteAsync_WithExistingMeasurement_ShouldRemoveFromDatabase()
    {
        // Arrange
        var vehicleId = Guid.NewGuid();
        var vehicle = new Vehicle
        {
            Id = vehicleId,
            Number = "MEAS-VEH-008",
            Date = DateTimeOffset.UtcNow,
            EntryDate = DateTimeOffset.UtcNow,
            Counterparty = "Supplier",
            VehiclePlate = "Q008QQ77",
            Driver = "Driver",
            ExitDate = null
        };

        _dbContext.Vehicles.Add(vehicle);
        await _dbContext.SaveChangesAsync();

        var measurement = new HumidityMeasurement
        {
            Id = Guid.NewGuid(),
            VehicleId = vehicleId,
            HumidityValue = 12.0,
            TemperatureC = 20.0,
            Source = MeasurementSource.Manual,
            Timestamp = DateTimeOffset.UtcNow,
            Sign = SignType.None
        };

        _dbContext.Measurements.Add(measurement);
        await _dbContext.SaveChangesAsync();

        // Act
        _dbContext.Measurements.Remove(measurement);
        await _dbContext.SaveChangesAsync();

        // Assert
        var deleted = await _dbContext.Measurements.FindAsync(measurement.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task BulkCreateAsync_WithMultipleMeasurements_ShouldPersistAll()
    {
        // Arrange
        var vehicleId = Guid.NewGuid();
        var vehicle = new Vehicle
        {
            Id = vehicleId,
            Number = "MEAS-VEH-009",
            Date = DateTimeOffset.UtcNow,
            EntryDate = DateTimeOffset.UtcNow,
            Counterparty = "Supplier",
            VehiclePlate = "R009RR77",
            Driver = "Driver",
            ExitDate = null
        };

        _dbContext.Vehicles.Add(vehicle);
        await _dbContext.SaveChangesAsync();

        var measurements = Enumerable.Range(1, 10).Select(i => new HumidityMeasurement
        {
            Id = Guid.NewGuid(),
            VehicleId = vehicleId,
            HumidityValue = 10.0 + i * 0.5,
            TemperatureC = 20.0,
            Source = MeasurementSource.Auto,
            Timestamp = DateTimeOffset.UtcNow.AddMinutes(-i),
            Sign = SignType.None
        }).ToList();

        // Act
        _dbContext.Measurements.AddRange(measurements);
        await _dbContext.SaveChangesAsync();

        // Assert
        var count = await _dbContext.Measurements
            .Where(m => m.VehicleId == vehicleId)
            .CountAsync();

        count.Should().Be(10);
    }

    [Fact]
    public async Task GetStatisticsByVehicleIdAsync_ShouldReturnCorrectAggregates()
    {
        // Arrange
        var vehicleId = Guid.NewGuid();
        var vehicle = new Vehicle
        {
            Id = vehicleId,
            Number = "MEAS-VEH-010",
            Date = DateTimeOffset.UtcNow,
            EntryDate = DateTimeOffset.UtcNow,
            Counterparty = "Supplier",
            VehiclePlate = "S010SS77",
            Driver = "Driver",
            ExitDate = null
        };

        _dbContext.Vehicles.Add(vehicle);
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
                Timestamp = DateTimeOffset.UtcNow.AddHours(-3),
                Sign = SignType.None
            },
            new HumidityMeasurement
            {
                Id = Guid.NewGuid(),
                VehicleId = vehicleId,
                HumidityValue = 12.0,
                TemperatureC = 21.0,
                Source = MeasurementSource.Manual,
                Timestamp = DateTimeOffset.UtcNow.AddHours(-2),
                Sign = SignType.None
            },
            new HumidityMeasurement
            {
                Id = Guid.NewGuid(),
                VehicleId = vehicleId,
                HumidityValue = 14.0,
                TemperatureC = 22.0,
                Source = MeasurementSource.Manual,
                Timestamp = DateTimeOffset.UtcNow.AddHours(-1),
                Sign = SignType.None
            }
        };

        _dbContext.Measurements.AddRange(measurements);
        await _dbContext.SaveChangesAsync();

        // Act
        var stats = await _dbContext.Measurements
            .Where(m => m.VehicleId == vehicleId)
            .Select(m => m.HumidityValue)
            .ToListAsync();

        var avgHumidity = stats.Average();
        var minHumidity = stats.Min();
        var maxHumidity = stats.Max();
        var count = stats.Count;

        // Assert
        count.Should().Be(3);
        avgHumidity.Should().Be(12.0);
        minHumidity.Should().Be(10.0);
        maxHumidity.Should().Be(14.0);
    }
}