using FluentAssertions;
using FluentValidation;
using Humidity.Application.DTOs;
using Humidity.Application.Interfaces;
using Humidity.Application.Services;
using Humidity.Domain.Common;
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
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using HumidityMeasurement = Humidity.Domain.Entities.HumidityMeasurement;

namespace Humidity.IntegrationTests.Services;

public class SupplierServiceIntegrationTests : IClassFixture<TestContainersFixture>, IAsyncLifetime
{
    private readonly IServiceProvider _serviceProvider;
    private readonly HumidityDbContext _dbContext;
    private readonly ISupplierService _supplierService;

    public SupplierServiceIntegrationTests(TestContainersFixture fixture)
    {
        var services = new ServiceCollection();

        // 1. Регистрируем логирование
        services.AddLogging();

        // 2. Регистрируем реальную БД
        services.AddDbContext<HumidityDbContext>(options =>
            options.UseNpgsql(fixture.ConnectionString));

        // 3. Регистрируем репозитории, которые SupplierService реально использует 
        // для агрегации данных (ISupplierRepository НЕ существует в проекте)
        services.AddScoped<IMeasurementRepository, MeasurementRepository>();
        services.AddScoped<IVehicleRepository, VehicleRepository>();

        // 4. Регистрируем реальный маппер
        services.AddScoped(sp => MapperHelper.CreateMapper());

        // 5. Регистрируем все валидаторы FluentValidation
        services.AddValidatorsFromAssembly(typeof(SupplierDto).Assembly);

        // 6. Регистрируем тестируемый сервис
        services.AddScoped<ISupplierService, SupplierService>();

        _serviceProvider = services.BuildServiceProvider();
        _dbContext = _serviceProvider.GetRequiredService<HumidityDbContext>();
        _supplierService = _serviceProvider.GetRequiredService<ISupplierService>();
    }

    public async Task InitializeAsync()
    {
        await _dbContext.Database.EnsureCreatedAsync();
        await DatabaseCleaner.CleanDatabaseAsync(_dbContext);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetSuppliersAsync_ShouldReturnAggregatedData()
    {
        // Arrange
        var inn1 = "7707083893";
        var inn2 = "7707083894";

        var vehicle1 = new Vehicle { Id = Guid.NewGuid(), Number = "SUP-001", Date = DateTimeOffset.UtcNow, EntryDate = DateTimeOffset.UtcNow, Counterparty = "Alpha LLC", Inn = inn1, VehiclePlate = "P1", Driver = "D1", ExitDate = null };
        var vehicle2 = new Vehicle { Id = Guid.NewGuid(), Number = "SUP-002", Date = DateTimeOffset.UtcNow, EntryDate = DateTimeOffset.UtcNow, Counterparty = "Alpha LLC", Inn = inn1, VehiclePlate = "P2", Driver = "D2", ExitDate = null };
        var vehicle3 = new Vehicle { Id = Guid.NewGuid(), Number = "SUP-003", Date = DateTimeOffset.UtcNow, EntryDate = DateTimeOffset.UtcNow, Counterparty = "Beta LLC", Inn = inn2, VehiclePlate = "P3", Driver = "D3", ExitDate = null };

        _dbContext.Vehicles.AddRange(vehicle1, vehicle2, vehicle3);
        await _dbContext.SaveChangesAsync();

        _dbContext.Measurements.AddRange(
            new HumidityMeasurement { Id = Guid.NewGuid(), VehicleId = vehicle1.Id, HumidityValue = 10.0, TemperatureC = 20.0, Source = MeasurementSource.Manual, Timestamp = DateTimeOffset.UtcNow, Sign = SignType.None },
            new HumidityMeasurement { Id = Guid.NewGuid(), VehicleId = vehicle2.Id, HumidityValue = 12.0, TemperatureC = 20.0, Source = MeasurementSource.Manual, Timestamp = DateTimeOffset.UtcNow, Sign = SignType.None },
            new HumidityMeasurement { Id = Guid.NewGuid(), VehicleId = vehicle3.Id, HumidityValue = 15.0, TemperatureC = 20.0, Source = MeasurementSource.Manual, Timestamp = DateTimeOffset.UtcNow, Sign = SignType.None }
        );
        await _dbContext.SaveChangesAsync();

        var from = DateTimeOffset.UtcNow.AddDays(-1);
        var to = DateTimeOffset.UtcNow.AddDays(1);

        // Act
        var result = await _supplierService.GetSuppliersAsync(from, to, pageNumber: 1, pageSize: 10);

        // Assert: Проверяем базовую корректность работы сервиса
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(2); // Два уникальных INN
        result.Items.Should().HaveCount(2);

        // Проверяем, что данные для INN1 присутствуют и агрегация работает
        var inn1Supplier = result.Items.FirstOrDefault(s => s.Inn == inn1);
        inn1Supplier.Should().NotBeNull();
        inn1Supplier!.Inn.Should().Be(inn1);
        // Средняя влажность для inn1: (10.0 + 12.0) / 2 = 11.0
        inn1Supplier.AverageHumidity.Should().Be(11.0);

        var inn2Supplier = result.Items.FirstOrDefault(s => s.Inn == inn2);
        inn2Supplier.Should().NotBeNull();
        inn2Supplier!.AverageHumidity.Should().Be(15.0);
    }

    [Fact]
    public async Task GetTopSuppliersAsync_WithAscendingOrder_ShouldReturnLowestHumidityFirst()
    {
        // Arrange
        var innGood = "1111111111"; // Низкая влажность
        var innBad = "2222222222";  // Высокая влажность

        var vehicleGood = new Vehicle { Id = Guid.NewGuid(), Number = "TOP-001", Date = DateTimeOffset.UtcNow, EntryDate = DateTimeOffset.UtcNow, Counterparty = "Good Supplier", Inn = innGood, VehiclePlate = "G1", Driver = "GD", ExitDate = null };
        var vehicleBad = new Vehicle { Id = Guid.NewGuid(), Number = "TOP-002", Date = DateTimeOffset.UtcNow, EntryDate = DateTimeOffset.UtcNow, Counterparty = "Bad Supplier", Inn = innBad, VehiclePlate = "B1", Driver = "BD", ExitDate = null };

        _dbContext.Vehicles.AddRange(vehicleGood, vehicleBad);
        await _dbContext.SaveChangesAsync();

        _dbContext.Measurements.AddRange(
            new HumidityMeasurement { Id = Guid.NewGuid(), VehicleId = vehicleGood.Id, HumidityValue = 8.0, TemperatureC = 20.0, Source = MeasurementSource.Manual, Timestamp = DateTimeOffset.UtcNow, Sign = SignType.None },
            new HumidityMeasurement { Id = Guid.NewGuid(), VehicleId = vehicleBad.Id, HumidityValue = 18.0, TemperatureC = 20.0, Source = MeasurementSource.Manual, Timestamp = DateTimeOffset.UtcNow, Sign = SignType.None }
        );
        await _dbContext.SaveChangesAsync();

        var from = DateTimeOffset.UtcNow.AddDays(-1);
        var to = DateTimeOffset.UtcNow.AddDays(1);

        // Act: ascending = true (хорошие, низкая влажность, первые в списке).
        // priorWeight = 0 — коррекция отключена: тест проверяет прежнее «наивное» поведение сортировки.
        var result = await _supplierService.GetTopSuppliersAsync(
            top: 2, ascending: true, from, to, priorWeight: 0);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.First().Inn.Should().Be(innGood);
        result.First().AverageHumidity.Should().Be(8.0);
        result.Last().Inn.Should().Be(innBad);
        result.Last().AverageHumidity.Should().Be(18.0);
    }

    [Fact]
    public async Task GetTopSuppliersAsync_WithDescendingOrder_ShouldReturnHighestHumidityFirst()
    {
        // Arrange
        var innGood = "3333333333";
        var innBad = "4444444444";

        var vehicleGood = new Vehicle { Id = Guid.NewGuid(), Number = "TOPD-001", Date = DateTimeOffset.UtcNow, EntryDate = DateTimeOffset.UtcNow, Counterparty = "Good Supplier", Inn = innGood, VehiclePlate = "G2", Driver = "GD", ExitDate = null };
        var vehicleBad = new Vehicle { Id = Guid.NewGuid(), Number = "TOPD-002", Date = DateTimeOffset.UtcNow, EntryDate = DateTimeOffset.UtcNow, Counterparty = "Bad Supplier", Inn = innBad, VehiclePlate = "B2", Driver = "BD", ExitDate = null };

        _dbContext.Vehicles.AddRange(vehicleGood, vehicleBad);
        await _dbContext.SaveChangesAsync();

        _dbContext.Measurements.AddRange(
            new HumidityMeasurement { Id = Guid.NewGuid(), VehicleId = vehicleGood.Id, HumidityValue = 9.0, TemperatureC = 20.0, Source = MeasurementSource.Manual, Timestamp = DateTimeOffset.UtcNow, Sign = SignType.None },
            new HumidityMeasurement { Id = Guid.NewGuid(), VehicleId = vehicleBad.Id, HumidityValue = 19.0, TemperatureC = 20.0, Source = MeasurementSource.Manual, Timestamp = DateTimeOffset.UtcNow, Sign = SignType.None }
        );
        await _dbContext.SaveChangesAsync();

        var from = DateTimeOffset.UtcNow.AddDays(-1);
        var to = DateTimeOffset.UtcNow.AddDays(1);

        // Act: ascending = false (плохие, высокая влажность, первые в списке).
        // priorWeight = 0 — коррекция отключена: тест проверяет прежнее «наивное» поведение сортировки.
        var result = await _supplierService.GetTopSuppliersAsync(
            top: 2, ascending: false, from, to, priorWeight: 0);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.First().Inn.Should().Be(innBad);
        result.First().AverageHumidity.Should().Be(19.0);
        result.Last().Inn.Should().Be(innGood);
        result.Last().AverageHumidity.Should().Be(9.0);
    }

    [Fact]
    public async Task GetTopSuppliersAsync_WithBayesianCorrection_ShouldShrinkSmallSamplesTowardGlobalMean()
    {
        // Arrange
        // Цель теста — убедиться, что байесовская коррекция работает корректно:
        // поставщик с малым числом замеров «подтягивается» к глобальной средней,
        // а поставщик с большим числом замеров сохраняет свою среднюю.
        var innFewMeasurements = "5555555555"; // Мало замеров, «сырая» средняя низкая (обманчиво хорошая)
        var innManyMeasurements = "6666666666"; // Много замеров, средняя близка к глобальной

        var vehicleFew = new Vehicle { Id = Guid.NewGuid(), Number = "BAY-FEW", Date = DateTimeOffset.UtcNow, EntryDate = DateTimeOffset.UtcNow, Counterparty = "Few Measurements LLC", Inn = innFewMeasurements, VehiclePlate = "F1", Driver = "FD", ExitDate = null };
        var vehicleMany = new Vehicle { Id = Guid.NewGuid(), Number = "BAY-MANY", Date = DateTimeOffset.UtcNow, EntryDate = DateTimeOffset.UtcNow, Counterparty = "Many Measurements LLC", Inn = innManyMeasurements, VehiclePlate = "M1", Driver = "MD", ExitDate = null };

        _dbContext.Vehicles.AddRange(vehicleFew, vehicleMany);
        await _dbContext.SaveChangesAsync();

        // Поставщик с 3 замерами, сумма = 24, наивная средняя = 8.0.
        // Поставщик с 20 замерами, сумма = 260, наивная средняя = 13.0.
        // Глобальная средняя m = (24 + 260) / (3 + 20) = 284 / 23 ≈ 12.35.
        var fewMeasurements = new[]
        {
            new HumidityMeasurement { Id = Guid.NewGuid(), VehicleId = vehicleFew.Id, HumidityValue = 8.0,  TemperatureC = 20.0, Source = MeasurementSource.Auto, Timestamp = DateTimeOffset.UtcNow, Sign = SignType.None },
            new HumidityMeasurement { Id = Guid.NewGuid(), VehicleId = vehicleFew.Id, HumidityValue = 8.0,  TemperatureC = 20.0, Source = MeasurementSource.Auto, Timestamp = DateTimeOffset.UtcNow, Sign = SignType.None },
            new HumidityMeasurement { Id = Guid.NewGuid(), VehicleId = vehicleFew.Id, HumidityValue = 8.0,  TemperatureC = 20.0, Source = MeasurementSource.Auto, Timestamp = DateTimeOffset.UtcNow, Sign = SignType.None },
        };

        var manyMeasurements = Enumerable.Range(0, 20).Select(_ => new HumidityMeasurement
        {
            Id = Guid.NewGuid(),
            VehicleId = vehicleMany.Id,
            HumidityValue = 13.0,
            TemperatureC = 20.0,
            Source = MeasurementSource.Auto,
            Timestamp = DateTimeOffset.UtcNow,
            Sign = SignType.None
        }).ToArray();

        _dbContext.Measurements.AddRange(fewMeasurements);
        _dbContext.Measurements.AddRange(manyMeasurements);
        await _dbContext.SaveChangesAsync();

        var from = DateTimeOffset.UtcNow.AddDays(-1);
        var to = DateTimeOffset.UtcNow.AddDays(1);

        // priorWeight = 30 — умеренная коррекция.
        var priorWeight = 30.0;

        // Act: ascending = true (сначала «хорошие» по скорректированной средней).
        var result = await _supplierService.GetTopSuppliersAsync(
            top: 2, ascending: true, from, to, priorWeight);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);

        var few = result.First(s => s.Inn == innFewMeasurements);
        var many = result.First(s => s.Inn == innManyMeasurements);

        // Обе средние доступны: наивная и скорректированная.
        few.AverageHumidity.Should().Be(8.0);
        many.AverageHumidity.Should().Be(13.0);

        // Байесовская коррекция «подтянула» малое число замеров ближе к глобальной средней:
        // few.AdjustedAverageHumidity должна быть заметно выше 8.0.
        few.AdjustedAverageHumidity.Should().NotBeNull();
        few.AdjustedAverageHumidity!.Value.Should().BeGreaterThan(8.0);

        // Поставщик с большим числом замеров почти не изменился: adjusted ≈ 13.
        many.AdjustedAverageHumidity.Should().NotBeNull();
        many.AdjustedAverageHumidity!.Value.Should().BeApproximately(13.0, 0.5);

        // Параметры коррекции проброшены из сервиса в репозиторий.
        few.PriorWeight.Should().Be(priorWeight);
        many.PriorWeight.Should().Be(priorWeight);
        few.GlobalAverageHumidity.Should().NotBeNull();
        many.GlobalAverageHumidity.Should().NotBeNull();

        // Наивный порядок был бы few(8.0) → many(13.0).
        // С байесовской коррекцией few «подтянулась» ближе к m, и порядок «хороших» сохраняется,
        // но разрыв между поставщиками сократился. Проверяем, что первым в списке
        // остался всё же поставщик с меньшей скорректированной средней.
        result.First().AdjustedAverageHumidity.Should().BeLessThanOrEqualTo(
            result.Last().AdjustedAverageHumidity ?? double.MaxValue);
    }

    [Fact]
    public async Task GetSuppliersAsync_WithDateRange_ShouldFilterByPeriod()
    {
        // Arrange
        var inn1 = "7777777777";
        var vehicle = new Vehicle { Id = Guid.NewGuid(), Number = "RANGE-001", Date = DateTimeOffset.UtcNow, EntryDate = DateTimeOffset.UtcNow, Counterparty = "Range Supplier", Inn = inn1, VehiclePlate = "R1", Driver = "RD", ExitDate = null };

        _dbContext.Vehicles.Add(vehicle);
        await _dbContext.SaveChangesAsync();

        var now = DateTimeOffset.UtcNow;

        _dbContext.Measurements.AddRange(
            // Замер ВНУТРИ диапазона (должен быть учтён)
            new HumidityMeasurement { Id = Guid.NewGuid(), VehicleId = vehicle.Id, HumidityValue = 10.0, TemperatureC = 20.0, Source = MeasurementSource.Manual, Timestamp = now.AddDays(-2), Sign = SignType.None },
            // Замер ВНЕ диапазона (должен быть проигнорирован)
            new HumidityMeasurement { Id = Guid.NewGuid(), VehicleId = vehicle.Id, HumidityValue = 50.0, TemperatureC = 20.0, Source = MeasurementSource.Manual, Timestamp = now.AddDays(-10), Sign = SignType.None }
        );
        await _dbContext.SaveChangesAsync();

        var from = now.AddDays(-5);
        var to = now.AddDays(1);

        // Act
        var result = await _supplierService.GetSuppliersAsync(from, to, pageNumber: 1, pageSize: 10);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().ContainSingle();

        var supplier = result.Items.First();
        supplier.Inn.Should().Be(inn1);
        // Средняя влажность должна быть 10.0 (только замер внутри диапазона), а не 30.0
        supplier.AverageHumidity.Should().Be(10.0);
    }
}