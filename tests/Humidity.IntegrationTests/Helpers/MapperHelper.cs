using AutoMapper;
using Humidity.Application;
using Microsoft.Extensions.Logging.Abstractions;

namespace Humidity.IntegrationTests.Helpers;

/// <summary>
/// Вспомогательный класс для создания экземпляра IMapper с настройками из приложения
/// </summary>
public static class MapperHelper
{
    public static IMapper CreateMapper()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfile>();
        },
         NullLoggerFactory.Instance // Передаем обязательный второй аргумент
        );
        return config.CreateMapper();
    }
}