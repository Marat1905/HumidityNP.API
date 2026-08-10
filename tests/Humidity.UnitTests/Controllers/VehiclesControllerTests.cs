using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Humidity.API.Controllers;
using Humidity.Application.DTOs;
using Humidity.Application.Interfaces;
using Humidity.Domain.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Humidity.UnitTests.Controllers;

public class VehiclesControllerTests
{
    private readonly Mock<IVehicleService> _vehicleServiceMock;
    private readonly VehiclesController _controller;

    public VehiclesControllerTests()
    {
        _vehicleServiceMock = new Mock<IVehicleService>();
        _controller = new VehiclesController(_vehicleServiceMock.Object);

        // Инициализация HttpContext для предотвращения NullReferenceException
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    [Fact]
    public async Task GetAll_WithValidParameters_ReturnsOkWithPagedResult()
    {
        // Arrange
        var vehicles = new List<VehicleDto> { new VehicleDto { Id = Guid.NewGuid(), VehiclePlate = "A123BC" } };
        var pagedResult = new PagedResult<VehicleDto>
        {
            Items = vehicles,
            TotalCount = 1,
            PageNumber = 1,
            PageSize = 20,
            TotalPages = 1
        };

        _vehicleServiceMock
            .Setup(s => s.GetFilteredPagedAsync(1, 20, null, true, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetAll(1, 20, null, "active", null, null);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(pagedResult);
    }

    [Fact]
    public async Task GetById_WhenVehicleExists_ReturnsOk()
    {
        // Arrange
        var vehicleId = Guid.NewGuid();
        var vehicle = new VehicleDto { Id = vehicleId, VehiclePlate = "A123BC" };

        _vehicleServiceMock
            .Setup(s => s.GetByIdAsync(vehicleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vehicle);

        // Act
        var result = await _controller.GetById(vehicleId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(vehicle);
    }

    [Fact]
    public async Task GetById_WhenVehicleNotFound_ReturnsNotFound()
    {
        // Arrange
        var vehicleId = Guid.NewGuid();

        _vehicleServiceMock
            .Setup(s => s.GetByIdAsync(vehicleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((VehicleDto?)null);

        // Act
        var result = await _controller.GetById(vehicleId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Create_WithValidRequest_ReturnsCreatedAtAction()
    {
        // Arrange
        var request = new CreateVehicleRequest { VehiclePlate = "A123BC", Counterparty = "Test Supplier" };
        var createdVehicle = new VehicleDto { Id = Guid.NewGuid(), VehiclePlate = "A123BC" };

        _vehicleServiceMock
            .Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdVehicle);

        // Act
        var result = await _controller.Create(request);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(VehiclesController.GetById));
        createdResult.Value.Should().BeEquivalentTo(createdVehicle);
    }

    [Fact]
    public async Task Delete_WithValidId_ReturnsNoContent()
    {
        // Arrange
        var vehicleId = Guid.NewGuid();

        _vehicleServiceMock
            .Setup(s => s.DeleteAsync(vehicleId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Delete(vehicleId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }
}