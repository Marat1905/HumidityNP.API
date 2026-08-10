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
    public async Task GetSupplierDetailsAsync_ReturnsDetailsWithVehicles()
    {
        // Arrange
        var inn = "7707083893";
        var from = DateTimeOffset.UtcNow.AddDays(-7);
        var to = DateTimeOffset.UtcNow;

        var expectedDetails = new SupplierDetailsDto
        {
            Inn = inn,
            Counterparty = "Test LLC",
            Vehicles = new List<SupplierVehicleSummaryDto>
            {
                new SupplierVehicleSummaryDto { VehicleId = Guid.NewGuid(), VehiclePlate = "A123BC" }
            },
            OverallStatistics = new MeasurementStatisticsDto()
        };

        _measurementRepositoryMock.Setup(r => r.GetSupplierDetailsAsync(inn, from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDetails);

        // Act
        var result = await _service.GetSupplierDetailsAsync(inn, from, to, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Inn.Should().Be(inn);
        result.Vehicles.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetTopSuppliersAsync_ReturnsSortedList()
    {
        // Arrange
        var from = DateTimeOffset.UtcNow.AddDays(-7);
        var to = DateTimeOffset.UtcNow;
        var topSuppliers = new List<SupplierDto>
        {
            new SupplierDto { Inn = "7707083893", AverageHumidity = 12.0 },
            new SupplierDto { Inn = "7707083894", AverageHumidity = 15.0 }
        };

        _measurementRepositoryMock.Setup(r => r.GetTopSuppliersAsync(2, true, from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(topSuppliers);

        // Act
        var result = await _service.GetTopSuppliersAsync(2, true, from, to, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.First().AverageHumidity.Should().Be(12.0);
    }
}