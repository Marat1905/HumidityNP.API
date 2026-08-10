using FluentAssertions;
using Humidity.API.Controllers;
using Humidity.Application.Interfaces;
using Humidity.Domain.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Humidity.UnitTests.Controllers;

public class SuppliersControllerTests
{
    private readonly Mock<ISupplierService> _supplierServiceMock;
    private readonly SuppliersController _controller;

    public SuppliersControllerTests()
    {
        _supplierServiceMock = new Mock<ISupplierService>();
        _controller = new SuppliersController(_supplierServiceMock.Object);

        // Инициализация HttpContext для предотвращения NullReferenceException 
        // при обращении к HttpContext.RequestAborted в методах контроллера
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    [Fact]
    public async Task GetSuppliers_WithValidParameters_ReturnsOkWithPagedResult()
    {
        // Arrange
        var from = DateTimeOffset.UtcNow.AddDays(-7);
        var to = DateTimeOffset.UtcNow;
        var suppliers = new List<SupplierDto>
        {
            new SupplierDto
            {
                Inn = "7707083893",
                Counterparty = "Test Supplier LLC",
                VehiclesCount = 5,
                TotalMeasurements = 10,
                AverageHumidity = 15.5,
                MinHumidity = 10.0,
                MaxHumidity = 20.0
            }
        };
        var pagedResult = new PagedResult<SupplierDto>
        {
            Items = suppliers,
            TotalCount = 1,
            PageNumber = 1,
            PageSize = 20,
            TotalPages = 1
        };

        _supplierServiceMock
            .Setup(s => s.GetSuppliersAsync(from, to, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetSuppliers(from, to, 1, 20);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(pagedResult);
    }

    [Fact]
    public async Task GetSupplierDetails_WhenVehiclesExist_ReturnsOk()
    {
        // Arrange
        var inn = "7707083893";
        var from = DateTimeOffset.UtcNow.AddDays(-7);
        var to = DateTimeOffset.UtcNow;
        var details = new SupplierDetailsDto
        {
            Inn = inn,
            Counterparty = "Test Supplier LLC",
            Vehicles = new List<SupplierVehicleSummaryDto>
            {
                new SupplierVehicleSummaryDto
                {
                    VehicleId = Guid.NewGuid(),
                    Number = "V001",
                    VehiclePlate = "A123BC",
                    EntryDate = DateTimeOffset.UtcNow.AddDays(-5),
                    ExitDate = null,
                    MeasurementsCount = 3,
                    AverageHumidity = 15.5,
                    MinHumidity = 12.0,
                    MaxHumidity = 18.0,
                    AutoCount = 2,
                    ManualCount = 1,
                    LastMeasurementTimestamp = DateTimeOffset.UtcNow.AddHours(-2)
                }
            },
            OverallStatistics = new MeasurementStatisticsDto()
        };

        _supplierServiceMock
            .Setup(s => s.GetSupplierDetailsAsync(inn, from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(details);

        // Act
        var result = await _controller.GetSupplierDetails(inn, from, to);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(details);
    }

    [Fact]
    public async Task GetSupplierDetails_WhenVehiclesEmpty_ReturnsNotFound()
    {
        // Arrange
        var inn = "7707083893";
        var from = DateTimeOffset.UtcNow.AddDays(-7);
        var to = DateTimeOffset.UtcNow;
        var details = new SupplierDetailsDto
        {
            Inn = inn,
            Counterparty = "Test Supplier LLC",
            Vehicles = new List<SupplierVehicleSummaryDto>(), // Пустой список машин
            OverallStatistics = new MeasurementStatisticsDto()
        };

        _supplierServiceMock
            .Setup(s => s.GetSupplierDetailsAsync(inn, from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(details);

        // Act
        var result = await _controller.GetSupplierDetails(inn, from, to);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetTopSuppliers_WithValidParameters_ReturnsOk()
    {
        // Arrange
        var from = DateTimeOffset.UtcNow.AddDays(-7);
        var to = DateTimeOffset.UtcNow;
        var topSuppliers = new List<SupplierDto>
        {
            new SupplierDto
            {
                Inn = "7707083893",
                Counterparty = "Top Supplier LLC",
                VehiclesCount = 10,
                TotalMeasurements = 50,
                AverageHumidity = 12.5,
                MinHumidity = 8.0,
                MaxHumidity = 17.0
            }
        };

        _supplierServiceMock
            .Setup(s => s.GetTopSuppliersAsync(10, true, from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(topSuppliers);

        // Act
        var result = await _controller.GetTopSuppliers(from, to, 10, "asc");

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(topSuppliers);
    }
}