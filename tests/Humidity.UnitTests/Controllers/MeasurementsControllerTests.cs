using Humidity.API.Controllers;
using Humidity.Application.Interfaces;
using Moq;

namespace Humidity.UnitTests.Controllers;
public class MeasurementsControllerTests
{
    private readonly Mock<IMeasurementService> _serviceMock;
    private readonly MeasurementsController _controller;

    public MeasurementsControllerTests()
    {
        _serviceMock = new Mock<IMeasurementService>();
        _controller = new MeasurementsController(_serviceMock.Object);
    }
}

