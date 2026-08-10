using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Humidity.Domain.Entities;
using Humidity.Infrastructure.Data;
using Humidity.IntegrationTests.Fixtures;
using Humidity.IntegrationTests.Helpers;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Humidity.IntegrationTests.Repositories;

public class VehicleRepositoryTests : IClassFixture<TestContainersFixture>, IAsyncLifetime
{
    private readonly HumidityDbContext _dbContext;

    public VehicleRepositoryTests(TestContainersFixture fixture)
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
    public async Task AddAsync_WithValidVehicle_ShouldPersistToDatabase()
    {
        // Arrange
        var vehicle = new Vehicle
        {
            Id = Guid.NewGuid(),
            Number = "TEST-001",
            Date = DateTimeOffset.UtcNow,
            EntryDate = DateTimeOffset.UtcNow,
            Counterparty = "Test Supplier LLC",
            Inn = "1234567890",
            VehicleBrand = "KAMAZ",
            VehiclePlate = "A001AA77",
            Trailer = "T001",
            Driver = "Ivanov I.I.",
            ExitDate = null
        };

        // Act
        _dbContext.Vehicles.Add(vehicle);
        await _dbContext.SaveChangesAsync();

        // Assert
        var savedVehicle = await _dbContext.Vehicles.FindAsync(vehicle.Id);
        savedVehicle.Should().NotBeNull();
        savedVehicle!.VehiclePlate.Should().Be("A001AA77");
        savedVehicle.Counterparty.Should().Be("Test Supplier LLC");
        savedVehicle.ExitDate.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WhenVehicleExists_ShouldReturnVehicle()
    {
        // Arrange
        var vehicleId = Guid.NewGuid();
        var vehicle = new Vehicle
        {
            Id = vehicleId,
            Number = "TEST-002",
            Date = DateTimeOffset.UtcNow,
            EntryDate = DateTimeOffset.UtcNow,
            Counterparty = "Supplier ABC",
            VehiclePlate = "B002BB77",
            Driver = "Petrov P.P.",
            ExitDate = null
        };

        _dbContext.Vehicles.Add(vehicle);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _dbContext.Vehicles.FindAsync(vehicleId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(vehicleId);
        result.VehiclePlate.Should().Be("B002BB77");
    }

    [Fact]
    public async Task GetByIdAsync_WhenVehicleNotExists_ShouldReturnNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _dbContext.Vehicles.FindAsync(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_WithPagination_ShouldReturnPagedResult()
    {
        // Arrange
        var vehicles = Enumerable.Range(1, 25).Select(i => new Vehicle
        {
            Id = Guid.NewGuid(),
            Number = $"TEST-{i:D3}",
            Date = DateTimeOffset.UtcNow,
            EntryDate = DateTimeOffset.UtcNow,
            Counterparty = $"Supplier {i}",
            VehiclePlate = $"C{i:D3}CC77",
            Driver = $"Driver {i}",
            ExitDate = null
        }).ToList();

        _dbContext.Vehicles.AddRange(vehicles);
        await _dbContext.SaveChangesAsync();

        // Act
        var pageNumber = 2;
        var pageSize = 10;
        var result = await _dbContext.Vehicles
            .OrderBy(v => v.Number)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var totalCount = await _dbContext.Vehicles.CountAsync();

        // Assert
        result.Should().HaveCount(pageSize);
        totalCount.Should().Be(25);
    }

    [Fact]
    public async Task GetActiveVehiclesAsync_ShouldReturnOnlyActiveVehicles()
    {
        // Arrange
        var activeVehicle = new Vehicle
        {
            Id = Guid.NewGuid(),
            Number = "ACTIVE-001",
            Date = DateTimeOffset.UtcNow,
            EntryDate = DateTimeOffset.UtcNow,
            Counterparty = "Active Supplier",
            VehiclePlate = "D001DD77",
            Driver = "Active Driver",
            ExitDate = null
        };

        var inactiveVehicle = new Vehicle
        {
            Id = Guid.NewGuid(),
            Number = "INACTIVE-001",
            Date = DateTimeOffset.UtcNow,
            EntryDate = DateTimeOffset.UtcNow,
            ExitDate = DateTimeOffset.UtcNow,
            Counterparty = "Inactive Supplier",
            VehiclePlate = "E001EE77",
            Driver = "Inactive Driver"
        };

        _dbContext.Vehicles.AddRange(activeVehicle, inactiveVehicle);
        await _dbContext.SaveChangesAsync();

        // Act
        var activeVehicles = await _dbContext.Vehicles
            .Where(v => v.ExitDate == null)
            .ToListAsync();

        // Assert
        activeVehicles.Should().ContainSingle();
        activeVehicles.First().Id.Should().Be(activeVehicle.Id);
    }

    [Fact]
    public async Task GetFilteredAsync_WithCounterpartyFilter_ShouldReturnMatchingVehicles()
    {
        // Arrange
        var vehicle1 = new Vehicle
        {
            Id = Guid.NewGuid(),
            Number = "FILTER-001",
            Date = DateTimeOffset.UtcNow,
            EntryDate = DateTimeOffset.UtcNow,
            Counterparty = "Alpha Supplier",
            VehiclePlate = "F001FF77",
            Driver = "Driver 1",
            ExitDate = null
        };

        var vehicle2 = new Vehicle
        {
            Id = Guid.NewGuid(),
            Number = "FILTER-002",
            Date = DateTimeOffset.UtcNow,
            EntryDate = DateTimeOffset.UtcNow,
            Counterparty = "Beta Supplier",
            VehiclePlate = "F002FF77",
            Driver = "Driver 2",
            ExitDate = null
        };

        _dbContext.Vehicles.AddRange(vehicle1, vehicle2);
        await _dbContext.SaveChangesAsync();

        // Act
        var filteredVehicles = await _dbContext.Vehicles
            .Where(v => v.Counterparty.Contains("Alpha"))
            .ToListAsync();

        // Assert
        filteredVehicles.Should().ContainSingle();
        filteredVehicles.First().Counterparty.Should().Contain("Alpha");
    }

    [Fact]
    public async Task UpdateAsync_WithModifiedVehicle_ShouldPersistChanges()
    {
        // Arrange
        var vehicle = new Vehicle
        {
            Id = Guid.NewGuid(),
            Number = "UPDATE-001",
            Date = DateTimeOffset.UtcNow,
            EntryDate = DateTimeOffset.UtcNow,
            Counterparty = "Original Supplier",
            VehiclePlate = "G001GG77",
            Driver = "Original Driver",
            ExitDate = null
        };

        _dbContext.Vehicles.Add(vehicle);
        await _dbContext.SaveChangesAsync();

        // Act
        vehicle.Counterparty = "Updated Supplier";
        vehicle.Driver = "Updated Driver";
        await _dbContext.SaveChangesAsync();

        // Assert
        var updatedVehicle = await _dbContext.Vehicles.FindAsync(vehicle.Id);
        updatedVehicle.Should().NotBeNull();
        updatedVehicle!.Counterparty.Should().Be("Updated Supplier");
        updatedVehicle.Driver.Should().Be("Updated Driver");
    }

    [Fact]
    public async Task DeleteAsync_WithExistingVehicle_ShouldRemoveFromDatabase()
    {
        // Arrange
        var vehicle = new Vehicle
        {
            Id = Guid.NewGuid(),
            Number = "DELETE-001",
            Date = DateTimeOffset.UtcNow,
            EntryDate = DateTimeOffset.UtcNow,
            Counterparty = "Delete Supplier",
            VehiclePlate = "H001HH77",
            Driver = "Delete Driver",
            ExitDate = null
        };

        _dbContext.Vehicles.Add(vehicle);
        await _dbContext.SaveChangesAsync();

        // Act
        _dbContext.Vehicles.Remove(vehicle);
        await _dbContext.SaveChangesAsync();

        // Assert
        var deletedVehicle = await _dbContext.Vehicles.FindAsync(vehicle.Id);
        deletedVehicle.Should().BeNull();
    }

    [Fact]
    public async Task UnloadAsync_WithValidData_ShouldUpdateVehicleMetrics()
    {
        // Arrange
        var vehicle = new Vehicle
        {
            Id = Guid.NewGuid(),
            Number = "UNLOAD-001",
            Date = DateTimeOffset.UtcNow,
            EntryDate = DateTimeOffset.UtcNow,
            Counterparty = "Unload Supplier",
            VehiclePlate = "I001II77",
            Driver = "Unload Driver",
            ExitDate = null,
            BaleCount = 0,
            DamagedBaleCount = 0,
            WeightKg = 0,
            StackNumber = null
        };

        _dbContext.Vehicles.Add(vehicle);
        await _dbContext.SaveChangesAsync();

        // Act
        vehicle.BaleCount = 100;
        vehicle.DamagedBaleCount = 5;
        vehicle.WeightKg = 5000.5;
        vehicle.StackNumber = "STACK-001";
        vehicle.ExitDate = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync();

        // Assert
        var unloadedVehicle = await _dbContext.Vehicles.FindAsync(vehicle.Id);
        unloadedVehicle.Should().NotBeNull();
        unloadedVehicle!.BaleCount.Should().Be(100);
        unloadedVehicle.DamagedBaleCount.Should().Be(5);
        unloadedVehicle.WeightKg.Should().Be(5000.5);
        unloadedVehicle.StackNumber.Should().Be("STACK-001");
        unloadedVehicle.ExitDate.Should().NotBeNull();
    }
}