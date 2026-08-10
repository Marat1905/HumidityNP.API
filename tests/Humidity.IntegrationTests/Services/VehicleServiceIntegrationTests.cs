using FluentAssertions;
using Humidity.Application.DTOs;
using Humidity.Application.Interfaces;
using Humidity.Application.Services;
using Humidity.Domain.Entities;
using Humidity.Domain.Interfaces;
using Humidity.Infrastructure.Data;
using Humidity.Infrastructure.Repositories;
using Humidity.IntegrationTests.Fixtures;
using Humidity.IntegrationTests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Humidity.IntegrationTests.Services;

public class VehicleServiceIntegrationTests : IClassFixture<TestContainersFixture>, IAsyncLifetime
{
    private readonly IServiceProvider _serviceProvider;
    private readonly HumidityDbContext _dbContext;
    private readonly IVehicleService _vehicleService;

    public VehicleServiceIntegrationTests(TestContainersFixture fixture)
    {
        var services = new ServiceCollection();

        // 1. Регистрируем логирование (ОБЯЗАТЕЛЬНО для удовлетворения зависимости ILogger<T> в сервисах)
        services.AddLogging();

        // 2. Регистрируем реальную БД
        services.AddDbContext<HumidityDbContext>(options =>
            options.UseNpgsql(fixture.ConnectionString));

        // 3. Регистрируем ВСЕ репозитории для гарантии разрешения любых внутренних зависимостей
        services.AddScoped<IVehicleRepository, VehicleRepository>();
        services.AddScoped<IMeasurementRepository, MeasurementRepository>();

        // 4. Регистрируем реальный маппер через ваш хелпер
        services.AddScoped(sp => MapperHelper.CreateMapper());

        // 5. Регистрируем тестируемый сервис
        services.AddScoped<IVehicleService, VehicleService>();

        _serviceProvider = services.BuildServiceProvider();
        _dbContext = _serviceProvider.GetRequiredService<HumidityDbContext>();
        _vehicleService = _serviceProvider.GetRequiredService<IVehicleService>();
    }

    public async Task InitializeAsync()
    {
        await _dbContext.Database.EnsureCreatedAsync();
        await DatabaseCleaner.CleanDatabaseAsync(_dbContext);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CreateAsync_WithValidRequest_ShouldPersistAndReturnDto()
    {
        // Arrange
        var request = new CreateVehicleRequest
        {
            Number = "TEST-001",
            Date = DateTimeOffset.UtcNow,
            EntryDate = DateTimeOffset.UtcNow,
            Counterparty = "Test Supplier",
            Inn = "7707083893",
            VehicleBrand = "KAMAZ",
            VehiclePlate = "A001AA77",
            Trailer = "T001",
            Driver = "Ivanov I.I."
        };

        // Act
        var result = await _vehicleService.CreateAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.VehiclePlate.Should().Be("A001AA77");
        result.Counterparty.Should().Be("Test Supplier");

        var dbVehicle = await _dbContext.Vehicles.FindAsync(result.Id);
        dbVehicle.Should().NotBeNull();
        dbVehicle!.Driver.Should().Be("Ivanov I.I.");
    }

    [Fact]
    public async Task GetFilteredPagedAsync_WithActiveStatus_ShouldReturnOnlyActiveVehicles()
    {
        // Arrange
        var activeVehicle = new Vehicle
        {
            Id = Guid.NewGuid(),
            Number = "ACT-001",
            Date = DateTimeOffset.UtcNow,
            EntryDate = DateTimeOffset.UtcNow,
            Counterparty = "Supplier A",
            VehiclePlate = "B001BB77",
            Driver = "Driver A",
            ExitDate = null
        };

        var exitedVehicle = new Vehicle
        {
            Id = Guid.NewGuid(),
            Number = "EXT-001",
            Date = DateTimeOffset.UtcNow,
            EntryDate = DateTimeOffset.UtcNow,
            ExitDate = DateTimeOffset.UtcNow,
            Counterparty = "Supplier B",
            VehiclePlate = "C001CC77",
            Driver = "Driver B"
        };

        _dbContext.Vehicles.AddRange(activeVehicle, exitedVehicle);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _vehicleService.GetFilteredPagedAsync(
            pageNumber: 1,
            pageSize: 10,
            counterparty: null,
            isActive: true,
            plate: null,
            driver: null);

        // Assert
        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle();
        result.Items.First().VehiclePlate.Should().Be("B001BB77");
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
            VehiclePlate = "D001DD77",
            Driver = "Driver D",
            ExitDate = null,
            BaleCount = 0,
            DamagedBaleCount = 0,
            WeightKg = 0,
            StackNumber = null
        };

        _dbContext.Vehicles.Add(vehicle);
        await _dbContext.SaveChangesAsync();

        var unloadRequest = new UnloadVehicleRequest
        {
            BaleCount = 150,
            DamagedBaleCount = 3,
            WeightKg = 7500.5,
            StackNumber = "STACK-99"
        };

        // Act
        var result = await _vehicleService.UnloadAsync(vehicle.Id, unloadRequest);

        // Assert: Проверяем, что метрики разгрузки корректно обновлены
        result.BaleCount.Should().Be(150);
        result.DamagedBaleCount.Should().Be(3);
        result.WeightKg.Should().Be(7500.5);
        result.StackNumber.Should().Be("STACK-99");

        // Разгрузка НЕ должна автоматически устанавливать дату выезда — 
        // это отдельная бизнес-операция. Машина остаётся активной.
        result.ExitDate.Should().BeNull();

        // Проверяем, что изменения сохранены в БД
        var dbVehicle = await _dbContext.Vehicles.FindAsync(vehicle.Id);
        dbVehicle.Should().NotBeNull();
        dbVehicle!.BaleCount.Should().Be(150);
        dbVehicle.DamagedBaleCount.Should().Be(3);
        dbVehicle.WeightKg.Should().Be(7500.5);
        dbVehicle.StackNumber.Should().Be("STACK-99");
        dbVehicle.ExitDate.Should().BeNull(); // Машина всё ещё на площадке
    }
}