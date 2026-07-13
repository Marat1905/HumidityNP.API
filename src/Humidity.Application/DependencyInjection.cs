using Microsoft.Extensions.DependencyInjection;
using Humidity.Application.Interfaces;
using Humidity.Application.Services;

namespace Humidity.Application;

/// <summary>
/// Статический класс для регистрации зависимостей прикладного слоя в DI-контейнере.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Добавляет в контейнер сервисы приложения: AutoMapper, бизнес-сервисы.
    /// </summary>
    /// <param name="services">Коллекция сервисов.</param>
    /// <returns>Тот же экземпляр <see cref="IServiceCollection"/> для цепочки вызовов.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Регистрация AutoMapper
        services.AddAutoMapper(cfg =>
        {
        }, AppDomain.CurrentDomain.GetAssemblies());

        // Регистрация бизнес-сервисов
        services.AddScoped<IVehicleService, VehicleService>();
        services.AddScoped<IMeasurementService, MeasurementService>();

        return services;
    }
}