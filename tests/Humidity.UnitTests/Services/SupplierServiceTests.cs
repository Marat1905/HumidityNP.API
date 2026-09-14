using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using Humidity.Application.Services;
using Humidity.Domain.Common;
using Humidity.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Humidity.UnitTests.Services;

public class SupplierServiceTests
{
    private readonly Mock<IMeasurementRepository> _measurementRepositoryMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<ILogger<SupplierService>> _loggerMock;
    private readonly SupplierService _service;

    public SupplierServiceTests()
    {
        _measurementRepositoryMock = new Mock<IMeasurementRepository>();
        _mapperMock = new Mock<IMapper>();
        _loggerMock = new Mock<ILogger<SupplierService>>();

        _service = new SupplierService(
            _measurementRepositoryMock.Object,
            _mapperMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GetSuppliersAsync_ReturnsPagedResult()
    {
        // Arrange
        var from = DateTimeOffset.UtcNow.AddDays(-7);
        var to = DateTimeOffset.UtcNow;
        var supplierDto = new SupplierDto { Inn = "7707083893", Counterparty = "Test LLC" };

        var expectedPagedResult = new PagedResult<SupplierDto>
        {
            Items = new List<SupplierDto> { supplierDto },
            TotalCount = 1,
            PageNumber = 1,
            PageSize = 20,
            TotalPages = 1
        };

        _measurementRepositoryMock.Setup(r => r.GetSuppliersSummaryAsync(from, to, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPagedResult);

        // Act
        var result = await _service.GetSuppliersAsync(from, to, 1, 20, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(1);
        result.Items.Should().HaveCount(1);
        result.Items.First().Inn.Should().Be("7707083893");
    }

    [Fact]
    public async Task GetSupplierDetailsAsync_ReturnsDetailsWithPagedVehicles()
    {
        // Arrange
        var inn = "7707083893";
        var from = DateTimeOffset.UtcNow.AddDays(-7);
        var to = DateTimeOffset.UtcNow;
        var pageNumber = 1;
        var pageSize = 10;
        var sortDescending = true; // По умолчанию сервер сортирует по EntryDate по убыванию

        var expectedDetails = new SupplierDetailsDto
        {
            Inn = inn,
            Counterparty = "Test LLC",
            Vehicles = new PagedResult<SupplierVehicleSummaryDto>
            {
                Items = new List<SupplierVehicleSummaryDto>
                {
                    new SupplierVehicleSummaryDto { VehicleId = Guid.NewGuid(), VehiclePlate = "A123BC" }
                },
                TotalCount = 1,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = 1
            },
            OverallStatistics = new MeasurementStatisticsDto()
        };

        _measurementRepositoryMock
            .Setup(r => r.GetSupplierDetailsAsync(
                inn, from, to, pageNumber, pageSize, sortDescending, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDetails);

        // Act
        var result = await _service.GetSupplierDetailsAsync(
            inn, from, to, pageNumber, pageSize, sortDescending, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Inn.Should().Be(inn);
        result.Vehicles.Items.Should().HaveCount(1);
        result.Vehicles.TotalCount.Should().Be(1);

        _measurementRepositoryMock.Verify(
            r => r.GetSupplierDetailsAsync(
                inn, from, to, pageNumber, pageSize, sortDescending, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetSupplierVehiclesForChartAsync_ReturnsUnpaginatedSortedList()
    {
        // Arrange
        var inn = "7707083893";
        var from = DateTimeOffset.UtcNow.AddDays(-7);
        var to = DateTimeOffset.UtcNow;
        var sortDescending = true;

        var chartData = new List<SupplierVehicleSummaryDto>
        {
            new SupplierVehicleSummaryDto { VehicleId = Guid.NewGuid(), VehiclePlate = "B222BB", EntryDate = DateTimeOffset.UtcNow },
            new SupplierVehicleSummaryDto { VehicleId = Guid.NewGuid(), VehiclePlate = "A111AA", EntryDate = DateTimeOffset.UtcNow.AddDays(-1) }
        };

        _measurementRepositoryMock
            .Setup(r => r.GetSupplierVehiclesForChartAsync(
                inn, from, to, sortDescending, It.IsAny<CancellationToken>()))
            .ReturnsAsync(chartData);

        // Act
        var result = await _service.GetSupplierVehiclesForChartAsync(
            inn, from, to, sortDescending, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);

        _measurementRepositoryMock.Verify(
            r => r.GetSupplierVehiclesForChartAsync(
                inn, from, to, sortDescending, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetTopSuppliersAsync_WithBayesianCorrection_ReturnsSortedListWithAdjustedHumidity()
    {
        // Arrange
        var from = DateTimeOffset.UtcNow.AddDays(-7);
        var to = DateTimeOffset.UtcNow;
        var priorWeight = 30.0;

        // Два поставщика: у первого мало замеров (n=3), у второго много (n=100).
        // Наивная средняя у первого «лучше» (8%), но байесовская коррекция «подтянет» его
        // ближе к глобальной средней. Второй сохранит свою среднюю практически без изменений.
        var topSuppliers = new List<SupplierDto>
        {
            new SupplierDto
            {
                Inn = "7707083893",
                AverageHumidity = 8.0,
                AdjustedAverageHumidity = 12.5, // подтянулась к глобальной
                PriorWeight = priorWeight,
                GlobalAverageHumidity = 13.0,
                TotalMeasurements = 3
            },
            new SupplierDto
            {
                Inn = "7707083894",
                AverageHumidity = 15.0,
                AdjustedAverageHumidity = 14.8, // почти не изменилась
                PriorWeight = priorWeight,
                GlobalAverageHumidity = 13.0,
                TotalMeasurements = 100
            }
        };

        _measurementRepositoryMock
            .Setup(r => r.GetTopSuppliersAsync(2, true, from, to, priorWeight, It.IsAny<CancellationToken>()))
            .ReturnsAsync(topSuppliers);

        // Act
        var result = await _service.GetTopSuppliersAsync(2, true, from, to, priorWeight, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);

        var first = result.First();
        first.Inn.Should().Be("7707083893");
        first.AverageHumidity.Should().Be(8.0);
        first.AdjustedAverageHumidity.Should().Be(12.5);
        first.PriorWeight.Should().Be(priorWeight);
        first.GlobalAverageHumidity.Should().Be(13.0);

        // Проверяем, что сервис пробросил priorWeight в репозиторий без изменений.
        _measurementRepositoryMock.Verify(
            r => r.GetTopSuppliersAsync(2, true, from, to, priorWeight, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetTopSuppliersAsync_WithoutCorrection_ReturnsNaiveAverages()
    {
        // Arrange
        var from = DateTimeOffset.UtcNow.AddDays(-7);
        var to = DateTimeOffset.UtcNow;
        var priorWeight = 0.0; // отключённая коррекция

        var topSuppliers = new List<SupplierDto>
        {
            new SupplierDto
            {
                Inn = "7707083893",
                AverageHumidity = 8.0,
                AdjustedAverageHumidity = 8.0, // совпадает с наивной при C=0
                PriorWeight = 0,
                TotalMeasurements = 3
            }
        };

        _measurementRepositoryMock
            .Setup(r => r.GetTopSuppliersAsync(1, true, from, to, 0.0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(topSuppliers);

        // Act
        var result = await _service.GetTopSuppliersAsync(1, true, from, to, 0.0, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        var first = result.First();
        first.AverageHumidity.Should().Be(first.AdjustedAverageHumidity);
        first.PriorWeight.Should().Be(0);
    }
}