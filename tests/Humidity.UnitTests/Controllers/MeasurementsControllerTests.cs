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

public class MeasurementsControllerTests
{
    private readonly Mock<IMeasurementService> _measurementServiceMock;
    private readonly MeasurementsController _controller;

    public MeasurementsControllerTests()
    {
        _measurementServiceMock = new Mock<IMeasurementService>();
        _controller = new MeasurementsController(_measurementServiceMock.Object);

        // Инициализация HttpContext для предотвращения NullReferenceException 
        // при обращении к HttpContext.RequestAborted в методах контроллера
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    [Fact]
    public async Task GetAll_WithValidParameters_ReturnsOkWithPagedResult()
    {
        // Arrange
        var measurements = new List<MeasurementDto> { new MeasurementDto { Id = Guid.NewGuid(), HumidityValue = 12.5 } };
        var pagedResult = new PagedResult<MeasurementDto>
        {
            Items = measurements,
            TotalCount = 1,
            PageNumber = 1,
            PageSize = 20,
            TotalPages = 1
        };

        _measurementServiceMock
            .Setup(s => s.GetAllPagedAsync(1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetAll(1, 20);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(pagedResult);
    }

    [Fact]
    public async Task Create_WithValidRequest_ReturnsCreatedAtAction()
    {
        // Arrange
        var vehicleId = Guid.NewGuid();
        var request = new CreateMeasurementRequest { VehicleId = vehicleId, HumidityValue = 15.0 };
        var createdMeasurement = new MeasurementDto { Id = Guid.NewGuid(), VehicleId = vehicleId, HumidityValue = 15.0 };

        _measurementServiceMock
            .Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdMeasurement);

        // Act
        var result = await _controller.Create(request);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(MeasurementsController.GetByVehicle));
        createdResult.Value.Should().BeEquivalentTo(createdMeasurement);
    }

    [Fact]
    public async Task Delete_WithValidId_ReturnsNoContent()
    {
        // Arrange
        var measurementId = Guid.NewGuid();

        _measurementServiceMock
            .Setup(s => s.DeleteAsync(measurementId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Delete(measurementId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }
}