using Humidity.API.Controllers;
using Humidity.Application.Interfaces;
using Moq;

namespace Humidity.UnitTests.Controllers;
public class VehiclesControllerTests
{
    private readonly Mock<IVehicleService> _serviceMock;
    private readonly VehiclesController _controller;

    public VehiclesControllerTests()
    {
        _serviceMock = new Mock<IVehicleService>();
        _controller = new VehiclesController(_serviceMock.Object);
    }
}

