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

        // Параметры пагинации и сортировки, которые тест передаёт в контроллер.
        // order = "desc" означает сортировку по EntryDate по убыванию (новые сверху).
        var pageNumber = 1;
        var pageSize = 10;
        var order = "desc";
        var sortDescending = true; // "desc" → sortDescending = true

        // SupplierDetailsDto.Vehicles теперь PagedResult<SupplierVehicleSummaryDto>,
        // так как пагинация и сортировка выполняются на стороне сервера.
        var details = new SupplierDetailsDto
        {
            Inn = inn,
            Counterparty = "Test Supplier LLC",
            Vehicles = new PagedResult<SupplierVehicleSummaryDto>
            {
                Items = new List<SupplierVehicleSummaryDto>
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
                TotalCount = 1,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = 1
            },
            OverallStatistics = new MeasurementStatisticsDto()
        };

        _supplierServiceMock
            .Setup(s => s.GetSupplierDetailsAsync(
                inn,
                from,
                to,
                pageNumber,
                pageSize,
                sortDescending,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(details);

        // Act
        var result = await _controller.GetSupplierDetails(inn, from, to, pageNumber, pageSize, order);

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
        var pageNumber = 1;
        var pageSize = 10;
        var order = "desc";
        var sortDescending = true;

        // Пограничный случай: у поставщика нет машин за период.
        // Контроллер ориентируется на TotalCount == 0, чтобы вернуть 404.
        var details = new SupplierDetailsDto
        {
            Inn = inn,
            Counterparty = "Test Supplier LLC",
            Vehicles = new PagedResult<SupplierVehicleSummaryDto>
            {
                Items = new List<SupplierVehicleSummaryDto>(), // Пустой список машин
                TotalCount = 0,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = 0
            },
            OverallStatistics = new MeasurementStatisticsDto()
        };

        _supplierServiceMock
            .Setup(s => s.GetSupplierDetailsAsync(
                inn,
                from,
                to,
                pageNumber,
                pageSize,
                sortDescending,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(details);

        // Act
        var result = await _controller.GetSupplierDetails(inn, from, to, pageNumber, pageSize, order);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetSupplierVehiclesForChart_WithValidParameters_ReturnsOk()
    {
        // Arrange
        var inn = "7707083893";
        var from = DateTimeOffset.UtcNow.AddDays(-7);
        var to = DateTimeOffset.UtcNow;
        var order = "desc";
        var sortDescending = true; // "desc" → sortDescending = true

        var chartData = new List<SupplierVehicleSummaryDto>
        {
            new SupplierVehicleSummaryDto
            {
                VehicleId = Guid.NewGuid(),
                Number = "V001",
                VehiclePlate = "A123BC",
                EntryDate = DateTimeOffset.UtcNow.AddDays(-2),
                MeasurementsCount = 3,
                AverageHumidity = 15.5,
                MinHumidity = 12.0,
                MaxHumidity = 18.0,
                AutoCount = 2,
                ManualCount = 1,
                LastMeasurementTimestamp = DateTimeOffset.UtcNow.AddHours(-1)
            },
            new SupplierVehicleSummaryDto
            {
                VehicleId = Guid.NewGuid(),
                Number = "V002",
                VehiclePlate = "B456CD",
                EntryDate = DateTimeOffset.UtcNow.AddDays(-4),
                MeasurementsCount = 2,
                AverageHumidity = 13.0,
                MinHumidity = 11.0,
                MaxHumidity = 15.0,
                AutoCount = 1,
                ManualCount = 1,
                LastMeasurementTimestamp = DateTimeOffset.UtcNow.AddHours(-3)
            }
        };

        _supplierServiceMock
            .Setup(s => s.GetSupplierVehiclesForChartAsync(
                inn,
                from,
                to,
                sortDescending,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(chartData);

        // Act
        var result = await _controller.GetSupplierVehiclesForChart(inn, from, to, order);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(chartData);
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