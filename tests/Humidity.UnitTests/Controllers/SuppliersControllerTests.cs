using Humidity.API.Controllers;
using Humidity.Application.Interfaces;
using Moq;

namespace Humidity.UnitTests.Controllers;

public class SuppliersControllerTests
{
    private readonly Mock<ISupplierService> _serviceMock;
    private readonly SuppliersController _controller;

    public SuppliersControllerTests()
    {
        _serviceMock = new Mock<ISupplierService>();
        _controller = new SuppliersController(_serviceMock.Object);
    }

}